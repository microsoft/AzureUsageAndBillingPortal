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
using System.Configuration; // access to configuration files
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace WebJobUsageDaily
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        static void Main()
        {
            Console.WriteLine("*************************************************************************");
            Console.WriteLine("WebJobUsageDaily:Main starting. DateTimeUTC: {0}", DateTime.UtcNow);

            List<Subscription> abis = Commons.Utils.GetSubscriptions();

            foreach (Subscription s in abis)
            {
                //Commons.Utils.UpdateSubscriptionStatus(s.Id, DataGenStatus.Pending, DateTime.UtcNow.AddYears(-3));
                try
                {
                    //DateTime sdt = DateTime.Now.AddYears(-3);
                    DateTime sdt = DateTime.Now.AddDays(-2);
                    DateTime edt = DateTime.Now;
                    BillingRequest br = new BillingRequest(s.Id, s.OrganizationId, sdt, edt);

                    // Insert into Azure Storage Queue
                    var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                    CloudQueue subscriptionsQueue = queueClient.GetQueueReference(ConfigurationManager.AppSettings["ida:QueueBillingDataRequests"].ToString());
                    subscriptionsQueue.CreateIfNotExists();
                    var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(br));
                    subscriptionsQueue.AddMessageAsync(queueMessage);
                    Console.WriteLine(String.Format("Sent id for daily billing log: {0}", s.Id));

                    Commons.Utils.UpdateSubscriptionStatus(s.Id, DataGenStatus.Pending, DateTime.UtcNow);
                }
                catch (Exception e)
                {
                    Console.WriteLine("WebJobUsageDaily - SendQueue: " + e.Message);
                }
            } // foreach

        }
    }
}
