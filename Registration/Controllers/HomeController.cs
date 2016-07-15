//------------------------------------------ START OF LICENSE -----------------------------------------
//Azure Usage Insights Portal
//
//Copyright(c) Microsoft Corporation
//
//All rights reserved.
//
//MIT License
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//associated documentation files (the ""Software""), to deal in the Software without restriction, 
//including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial 
//portions of the Software.
//
//THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
//BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------- END OF LICENSE ------------------------------------------
using Commons;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Registration.Models;
using System;
using System.Collections.Generic;
using System.Configuration; // access to configuration files
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace Registration.Controllers
{
    public class HomeController : Controller
    {
        private DataAccess db = new DataAccess();
        public ActionResult Index()
        {
            HomeIndexViewModel model = null;

            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
            {
                model = new HomeIndexViewModel();
                model.UserOrganizations = new Dictionary<string, Organization>();
                model.UserSubscriptions = new Dictionary<string, Subscription>();
                model.UserCanManageAccessForSubscriptions = new List<string>();
                model.DisconnectedUserOrganizations = new List<string>();

                var organizations = AzureResourceManagerUtil.GetUserOrganizations();
                foreach (Organization org in organizations)
                {
                    model.UserOrganizations.Add(org.Id, org);
                    var subscriptions = AzureResourceManagerUtil.GetUserSubscriptions(org.Id);

                    if (subscriptions != null)
                    {
                        foreach (var subscription in subscriptions)
                        {

                            Subscription s = db.Subscriptions.Find(subscription.Id);
                            if (s != null)
                            {
                                subscription.IsConnected = true;
                                subscription.ConnectedOn = s.ConnectedOn;
                                subscription.ConnectedBy = s.ConnectedBy;
                                subscription.DisplayTag = s.DisplayTag;
                                subscription.AzureAccessNeedsToBeRepaired = !AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscription.Id, org.Id);
                            }
                            else
                            {
                                subscription.IsConnected = false;
                            }
                            model.UserSubscriptions.Add(subscription.Id, subscription);

                            if (AzureResourceManagerUtil.UserCanManageAccessForSubscription(subscription.Id, org.Id))
                                model.UserCanManageAccessForSubscriptions.Add(subscription.Id);
                        }
                    }
                    else
                        model.DisconnectedUserOrganizations.Add(org.Id);
                }
            }

            return View(model);
        }
        public void Register(string directoryName = "common", bool isMSA = false)
        {
            if (!Request.IsAuthenticated)
            {
                // note configuration (keys, etc…) will not necessarily understand this authority.
                HttpContext.GetOwinContext().Environment.Add("Authority", string.Format(ConfigurationManager.AppSettings["ida:Authority"] + "OAuth2/Authorize", directoryName));
                if (isMSA) HttpContext.GetOwinContext().Environment.Add("DomainHint", "live.com");
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = this.Url.Action("Index", "Home") }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }
        public ActionResult SignOut()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType, 
                CookieAuthenticationDefaults.AuthenticationType);

            return RedirectToAction("Index", "Home");
        }
        public ActionResult Connect([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription, string servicePrincipalObjectId, string displayTag)
        {
            if (ModelState.IsValid)
            {
                AzureResourceManagerUtil.RevokeRoleFromServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);

                AzureResourceManagerUtil.GrantRoleToServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);

                if (AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscription.Id, subscription.OrganizationId))
                {
                    // Insert into SQL DB
                    subscription.ConnectedBy = (System.Security.Claims.ClaimsPrincipal.Current).FindFirst(ClaimTypes.Name).Value;
                    subscription.ConnectedOn = DateTime.Now;
                    subscription.AzureAccessNeedsToBeRepaired = false;
                    subscription.DisplayTag = displayTag;

                    subscription.DataGenDate = DateTime.UtcNow;
                    subscription.DataGenStatus = DataGenStatus.Pending;

                    db.Subscriptions.Add(subscription);
                    db.SaveChanges();


                    DateTime sdt = DateTime.Now.AddYears(-3);
                    DateTime edt = DateTime.Now.AddDays(-1);
                    BillingRequest br = new BillingRequest(subscription.Id, subscription.OrganizationId, sdt, edt);

                    // Insert into Azure Storage Queue
                    var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                    CloudQueue subscriptionsQueue = queueClient.GetQueueReference(ConfigurationManager.AppSettings["ida:QueueBillingDataRequests"].ToString());
                    subscriptionsQueue.CreateIfNotExists();
                    var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(br));
                    subscriptionsQueue.AddMessageAsync(queueMessage);
                }
            }

            return RedirectToAction("Index", "Home");
        }
        public ActionResult Disconnect([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            if (ModelState.IsValid)
            {
                AzureResourceManagerUtil.RevokeRoleFromServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);

                Subscription s = db.Subscriptions.Find(subscription.Id);
                if (s != null)
                {
                    db.Subscriptions.Remove(s);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Index", "Home");
        }
        public ActionResult RepairAccess([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            if (ModelState.IsValid)
            {
                AzureResourceManagerUtil.RevokeRoleFromServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);
                AzureResourceManagerUtil.GrantRoleToServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);

                Subscription s = db.Subscriptions.Find(subscription.Id);
                if (s != null)
                {
                    s.AzureAccessNeedsToBeRepaired = false;
                    db.Entry(s).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Index", "Home");
        }
        public ActionResult Error()
        {
            return View();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}