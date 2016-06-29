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
using Dashboard.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    //[Authorize]
    public class DashboardController : Controller
    {
        static private DashboardModel dm = new DashboardModel();
        private DataAccess db = new DataAccess();

        public ActionResult Index()
        {
            dm.subscriptionList.Clear();
            foreach (var s in db.Subscriptions.OrderBy(e => e.DisplayTag))
                dm.subscriptionList.Add(s.Id, s);

            return View(dm);
        }

        public async Task<ActionResult> ProcessSelectedIds(string[] selectedIDs)
        {
            try
            {
                foreach (string sid in selectedIDs)
                {
                    Subscription subs = dm.subscriptionList[sid];
                    if (subs == null)
                        continue;

                    // Following sendToQueue and SaveToDB must be atomic
                    DateTime sdt = DateTime.Now.AddYears(-3);
                    DateTime edt = DateTime.Now.AddDays(-1);
                    BillingRequest br = new BillingRequest(subs.Id, subs.OrganizationId, sdt, edt);

                    var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                    CloudQueue subscriptionsQueue = queueClient.GetQueueReference(ConfigurationManager.AppSettings["ida:QueueBillingDataRequests"].ToString());
                    subscriptionsQueue.CreateIfNotExists();
                    var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(br));
                    await subscriptionsQueue.AddMessageAsync(queueMessage);

                    Subscription s = db.Subscriptions.Find(sid);
                    if (s != null)
                    {
                        s.DataGenDate = DateTime.UtcNow;
                        s.DataGenStatus = DataGenStatus.Pending;
                        db.Entry(s).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                return RedirectToAction("Error", "Home", new {msg = e.Message });
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}