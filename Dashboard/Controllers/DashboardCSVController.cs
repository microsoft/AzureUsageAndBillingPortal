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
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    [Authorize]
    public class DashboardCSVController : Controller
    {
        static private DashboardCSVModel dm = new DashboardCSVModel();
        private DataAccess db = new DataAccess();
        private CloudQueue reportRequestsQueue;

        public DashboardCSVController()
        {
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            // Open storage account using credentials from .cscfg file.
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            // Get context object for working with queues, and Get a reference to the queue.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            reportRequestsQueue = queueClient.GetQueueReference(ConfigurationManager.AppSettings["ida:QueueReportRequest"].ToString());
            reportRequestsQueue.CreateIfNotExists();

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer csvContainer = blobClient.GetContainerReference(ConfigurationManager.AppSettings["ida:BlobReportPublish"].ToString());
            csvContainer.CreateIfNotExists();

            // create new permissions
            BlobContainerPermissions perms = new BlobContainerPermissions();
            perms.PublicAccess = BlobContainerPublicAccessType.Blob; // blob public access
            csvContainer.SetPermissions(perms);
        }

        // GET: Dashboard
        public ActionResult Index()
        {
            dm.userSubscriptionsList.Clear();
            foreach (var s in db.Subscriptions.OrderBy(e => e.DisplayTag))
                dm.userSubscriptionsList.Add(s.Id, s);

            if (dm.userSubscriptionsList.Count > 0 && dm.selectedUserSubscriptions.Count < 1)
                dm.selectedUserSubscriptions.Add(dm.userSubscriptionsList.ElementAt(0).Key);

            dm.repReqsList.Clear();
            foreach (var j in db.ReportRequests.OrderByDescending(e => e.reportDate))
                dm.repReqsList.Add(j.repReqID, j);

            return View(dm);
        }

        public async Task<ActionResult> CreateReport(string sdatemonth, string sdateday, string sdateyear,
                                         string edatemonth, string edateday, string edateyear,
                                         string period, string detailed, string [] subslist)
        {
            try
            {
                dm.startDateMonth = Convert.ToInt32(sdatemonth);
                dm.startDateDay = Convert.ToInt32(sdateday);
                dm.startDateYear = Convert.ToInt32(sdateyear);

                dm.endDateMonth = Convert.ToInt32(edatemonth);
                dm.endDateDay = Convert.ToInt32(edateday);
                dm.endDateYear = Convert.ToInt32(edateyear);

                dm.detailedReport = (detailed == "d" ? true : false);
                dm.dailyReport = (period == "d" ? true : false);

                if (subslist != null)
                    dm.selectedUserSubscriptions = subslist.ToList();

                if (dm.selectedUserSubscriptions.Count() < 1)
                    throw new Exception("No Azure subscription is selected. Select from list and try again (may use CTRL for multiple selection).");

                DateTime s, e;
                try
                {
                    s = new DateTime(dm.startDateYear, dm.startDateMonth, dm.startDateDay);
                    e = new DateTime(dm.endDateYear, dm.endDateMonth, dm.endDateDay);
                } catch
                {
                    throw new Exception("Invalid date input.");
                }

                TimeSpan ts = e.Subtract(s);
                if (ts.TotalDays > 150)
                    throw new Exception("Time interval can not be greater than 150 (5 month) days");

                if (ts.TotalDays < 1)
                    throw new Exception("Start date can not be later than or equal to the end date.");


                
                
                // now we have all parameters set. Ready to create jobs and send them to Storage queue to be processed by webjob
                ReportRequest rr = new ReportRequest();
                rr.reportDate = DateTime.UtcNow;
                rr.startDate = s;
                rr.endDate = e;
                rr.detailedReport = dm.detailedReport;
                rr.dailyReport = dm.dailyReport;

                foreach (string sid in dm.selectedUserSubscriptions)
                {
                    Subscription subs = dm.userSubscriptionsList[sid];
                    if (subs == null)
                        continue;

                    Report rjd = new Report();
                    rjd.subscriptionID = subs.Id;
                    rjd.organizationID = subs.OrganizationId;
                    rr.repReqs.Add(rjd);
                }

                db.ReportRequests.Add(rr);
                db.SaveChanges();


                var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(rr));
                await reportRequestsQueue.AddMessageAsync(queueMessage);
            }
            catch (Exception e)
            {
                //dm.Reset();
                return RedirectToAction("Error", "Home", new { msg = e.Message } );
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}