using Commons;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration; // access to configuration files
using System.Diagnostics;
using System.Linq;

namespace WebJobUsageDaily
{
	// see: https://github.com/Azure/azure-webjobs-sdk-extensions
	// see: http://www.cronmaker.com/

	public static class Functions
	{
		private static readonly TimeSpan[] DailySchedule;
		private static readonly string QueueBillingDataRequests = ConfigurationManager.AppSettings["ida:QueueBillingDataRequests"];
		private static readonly string JobDailySchedule = ConfigurationManager.AppSettings["JobDailySchedule"];
		private static readonly string AzureWebJobsStorage = ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"]?.ConnectionString;

		static Functions()
		{
			string[] dailySchedule = JobDailySchedule.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			DailySchedule = dailySchedule.Select(s => TimeSpan.Parse(s.Trim())).ToArray();
		}

		// Note: TimerTrigger requires website AlawaysOn option activated
		public static void TimerJob([TimerTrigger(typeof(MyDailySchedule))] TimerInfo timer)
		{
			EnqueueBillingDownload(DateTime.UtcNow.Date.AddDays(-2), DateTime.UtcNow.Date);
		}

		internal static void EnqueueBillingDownload(DateTime startDate, DateTime endDate)
		{
			List<Subscription> abis = Utils.GetSubscriptions();

			foreach (Subscription s in abis) {
				try {
					BillingRequest billingRequest = new BillingRequest(s.Id, s.OrganizationId, startDate, endDate);

					// Insert into Azure Storage Queue
					var storageAccount = CloudStorageAccount.Parse(AzureWebJobsStorage);
					CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
					CloudQueue subscriptionsQueue = queueClient.GetQueueReference(QueueBillingDataRequests);
					subscriptionsQueue.CreateIfNotExists();
					var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(billingRequest));
					subscriptionsQueue.AddMessageAsync(queueMessage);
					Trace.TraceInformation($"Enqueued id for daily billing log: {s.Id}");

					Utils.UpdateSubscriptionStatus(s.Id, DataGenStatus.Pending, DateTime.UtcNow);
				} catch (Exception e) {
					Trace.TraceError($"WebJobUsageDaily - SendQueue: {e.Message}");
				}
			} // foreach
		}

		internal static void ClearQueue()
		{
			var storageAccount = CloudStorageAccount.Parse(AzureWebJobsStorage);
			CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
			CloudQueue subscriptionsQueue = queueClient.GetQueueReference(QueueBillingDataRequests);
			subscriptionsQueue.Clear();
		}

		public class MyDailySchedule : DailySchedule
		{
			public MyDailySchedule() : base(DailySchedule) { }
		}
	}
}
