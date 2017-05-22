//------------------------------------------ START OF LICENSE -----------------------------------------
//Azure Usage and Billing Insights
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
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebJobBillingData
{
	public static class Functions
	{
		private static readonly string OfferCode = ConfigurationManager.AppSettings["ida:OfferCode"];
		private static readonly string Currency = ConfigurationManager.AppSettings["ida:Currency"];
		private static readonly string Locale = ConfigurationManager.AppSettings["ida:Locale"];
		private static readonly string RegionInfo = ConfigurationManager.AppSettings["ida:RegionInfo"];
		private static readonly string RetryCountToProcessMessage = ConfigurationManager.AppSettings["ida:RetryCountToProcessMessage"];
		private static readonly string SqlConnectionString = ConfigurationManager.ConnectionStrings["ASQLConn"]?.ConnectionString;

		public static void ProcessQueueMessage([QueueTrigger("billingdatarequests")] BillingRequest billingRequest, TextWriter logWriter = null)
		{
			if (logWriter != null) {
				TextWriterTraceListener traceListener = new TextWriterTraceListener(logWriter, "LogWriter");
				Trace.Listeners.Remove("LogWriter");
				Trace.Listeners.Add(traceListener);
				Trace.TraceInformation("Azure WebJob Log Writer configured");
			}

			Trace.TraceInformation($"WebJob process started. {nameof(billingRequest.SubscriptionId)}: {billingRequest.SubscriptionId}");
			int retriesLeft = Convert.ToInt32(RetryCountToProcessMessage);
			Exception lastException = null;

			while (retriesLeft > 0) {
				--retriesLeft;

				if (retriesLeft < 1) {
					Trace.TraceInformation($"Finished internal retries, time:{DateTime.UtcNow}");

					if (lastException != null) {
						throw lastException;
					} else {
						return;
					}
				}

				Trace.TraceInformation($"Start time:{DateTime.UtcNow}, retries Left: {retriesLeft}");

				try {
					//Fetch RateCard information First
					string rateCardUrl = AzureResourceManagerUtil.GetRateCardRestApiCallURL(billingRequest.SubscriptionId, OfferCode, Currency, Locale, RegionInfo);
					Trace.TraceInformation("Request cost info from RateCard service.");

					RateCardPayload rateCardInfo = GetRateCardInfo(rateCardUrl, billingRequest.OrganizationId);

					if (rateCardInfo == null) {
						Trace.TraceWarning("Problem receiving cost info occured - see log for details.");
						continue;
					} else {
						Trace.TraceInformation("Received cost info: " + rateCardInfo.ToString());
					}

					// if granularity=hourly then report up to prev. hour,
					// if granularity=daily then report up to prev. day. Othervise will get 400 error
					//DateTime sdt = DateTime.UtcNow.Date.AddYears(-3);
					//DateTime edt = DateTime.UtcNow.Date.AddDays(-1);

					// see: https://msdn.microsoft.com/en-us/library/azure/mt219004.aspx
					string restUrl = AzureResourceManagerUtil.GetBillingRestApiCallUrl(billingRequest.SubscriptionId, true, true, billingRequest.StartDate, billingRequest.EndDate);

					Trace.TraceInformation("Request usage data from Billing service.");
					var usageRecords = GetUsageDetails(restUrl, billingRequest.OrganizationId, rateCardInfo);
					Trace.TraceInformation($"Received record count: {usageRecords.Count}");

					if (usageRecords.Count > 0) {
						Trace.TraceInformation ("Inserting usage records into SQL database.");
						Task<int> task = InsertIntoSqlDbAsync(usageRecords, billingRequest.SubscriptionId, billingRequest.StartDate, billingRequest.EndDate);
						int recordCount = task.GetAwaiter().GetResult();
						Trace.TraceInformation($"Total {recordCount} usage record(s) inserted.");
					} else {
						Trace.TraceInformation("No usage data found.");
					}

					break;
				} catch (Exception e) {
					Trace.TraceError($"Exception: {nameof(ProcessQueueMessage)} -> e.Message: " + e.Message);
					lastException = e;
					if (retriesLeft == 0) throw;
				}

				Trace.TraceInformation($"Sleeping in {nameof(ProcessQueueMessage)} while loop for 5 min. DateTime: {DateTime.UtcNow}");
				Thread.Sleep(1000 * 60 * 5);
			}   // while

			Utils.UpdateSubscriptionStatus(billingRequest.SubscriptionId, DataGenStatus.Completed, DateTime.UtcNow);

			Trace.TraceInformation($"WebJob process completed. SubscriptionId: {billingRequest.SubscriptionId}");
		}

		public static async Task<int> InsertIntoSqlDbAsync(IEnumerable<UsageRecord> usageRecords, Guid subscriptionId, DateTime startDate, DateTime endDate, CancellationToken token = default(CancellationToken))
		{
			int recordCount;
			DateTime startTime;
			TimeSpan processingTime;

			SqlConnection connection = new SqlConnection(SqlConnectionString);
			// note: it is important to specify at least repeatableread transaction isolation level - otherwise other transaction could simultaneously manipulate same record range
			SqlCommand deleteUsageRecordCommand = new SqlCommand(@"delete from dbo.AzureUsageRecords with(repeatableread, rowlock)
				where SubscriptionId = @subscriptionId and UsageStartTime = @usageStartTime and UsageEndTime = @usageEndTime", connection);
			deleteUsageRecordCommand.CommandType = CommandType.Text;
			var deleteParameters = deleteUsageRecordCommand.Parameters;
			deleteParameters.Add("@subscriptionId", SqlDbType.UniqueIdentifier).Value = subscriptionId;
			deleteParameters.Add("@usageStartTime", SqlDbType.DateTime2).Value = startDate;
			deleteParameters.Add("@usageEndTime", SqlDbType.DateTime2).Value = endDate;

			SqlTransaction transaction = null;

			try {
				connection.Open();
				transaction = connection.BeginTransaction();
				deleteUsageRecordCommand.Transaction = transaction;
				recordCount = deleteUsageRecordCommand.ExecuteNonQuery();
				transaction.Commit();
				if (recordCount > 0) Trace.TraceInformation($"{recordCount} existing record(s) deleted");
			} catch {
				transaction?.Rollback();
			} finally {
				transaction?.Dispose();
				connection.Close();
				connection.Dispose();
			}

			SqlBulkCopy bulkCopy = new SqlBulkCopy(SqlConnectionString, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.TableLock);
			bulkCopy.DestinationTableName = "dbo.AzureUsageRecords";
			bulkCopy.BatchSize = 500;
			bulkCopy.NotifyAfter = 1000;
			bulkCopy.BulkCopyTimeout = 30;
			bulkCopy.SqlRowsCopied += BulkCopy_SqlRowsCopied;

			startTime = DateTime.UtcNow;

			try {
				using (RecordDataReader<UsageRecord> recReader = new RecordDataReader<UsageRecord>(usageRecords, x => { return Sink(x, startDate.AddDays(-1), endDate); })) {
					try {
						await bulkCopy.WriteToServerAsync(recReader, token).ConfigureAwait(false);
					} catch (Exception ex) {
						throw;
					}

					recordCount = recReader.RecordsAffected;
				}
			} finally {
				bulkCopy.Close();
			}

			processingTime = DateTime.UtcNow.Subtract(startTime);

			return recordCount;
		}

		private static bool Sink(UsageRecord record, DateTime startDate, DateTime endDate)
		{
			return (record.UsageStartTime >= startDate && record.UsageEndTime <= endDate);
			// where !tag.Key.StartsWith("hidden-") && !tag.Key.StartsWith("link:")
		}

		private static void BulkCopy_SqlRowsCopied(object sender, SqlRowsCopiedEventArgs e)
		{
			if (Environment.UserInteractive) Trace.TraceInformation($"{e.RowsCopied:n0} rows inserted");
		}

		public static List<UsageRecord> GetUsageDetails(string restUrl, Guid orgId, RateCardPayload rateCardInfo)
		{
			string nextLink = "";
			List<UsageRecord> usageRecords = new List<UsageRecord>();

			do {
				HttpWebResponse httpWebResponse = null;

				try {
					if (nextLink != "") {
						httpWebResponse = AzureResourceManagerUtil.BillingRestApiCall(nextLink, orgId);
					} else {
						httpWebResponse = AzureResourceManagerUtil.BillingRestApiCall(restUrl, orgId);
					}

					if (httpWebResponse == null) {
						Trace.TraceWarning("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
						Trace.TraceWarning($"{nameof(httpWebResponse)} == null");
						Trace.TraceWarning($"     {nameof(GetUsageDetails)}({nameof(restUrl)}, {nameof(orgId)})");
						Trace.TraceWarning($"     {nameof(restUrl)}: {restUrl}");
						Trace.TraceWarning($"     {nameof(orgId)}: {orgId}");

						// throw exception to start from scretch and retry.
						//throw new Exception("Possible reason: Bad request (400), Forbidden (403) to access. Server busy. Client blacklisted.");
					} else {
						// look response codes @ https://msdn.microsoft.com/en-us/library/azure/mt219001.aspx
						if (httpWebResponse.StatusCode == HttpStatusCode.OK) {
							Trace.TraceInformation($"Received Rest Call Response: {nameof(HttpStatusCode.OK)}. Processing...");
							string streamContent;

							using (Stream receiveStream = httpWebResponse.GetResponseStream()) {
								// Pipes the stream to a higher level stream reader with the required encoding format. 
								using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8)) {
									streamContent = readStream.ReadToEnd();
								}
							}

							UsagePayload usagePayload = JsonConvert.DeserializeObject<UsagePayload>(streamContent);

							foreach (UsageAggregate ua in usagePayload.Value) {
								// handle adding cost in
								var usageRecord = new UsageRecord(ua);

								try {
									var meterInfo = rateCardInfo.Meters.Where(p => p.MeterId == usageRecord.MeterId).SingleOrDefault();

									if (meterInfo.MeterRates.Count > 1) {
										Trace.TraceWarning("Multiple rates for meter: " + usageRecord.MeterId);
									}

									usageRecord.Cost = (double)meterInfo.MeterRates["0"] * usageRecord.Quantity;
									//if (usageRecord.cost < 0.01) usageRecord.cost = 0; // TODO: usage cost is definitelly NOT rounded like this
								} catch (Exception ex) {
									Trace.TraceError("Exception trying to apply cost info for meter: " + usageRecord.MeterId);
								}

								usageRecords.Add(usageRecord);
							}

							ContinuationToken contToken = JsonConvert.DeserializeObject<ContinuationToken>(streamContent);
							nextLink = contToken.NextLink ?? "";
						} else if (httpWebResponse.StatusCode == HttpStatusCode.Accepted) {
							Trace.TraceWarning($"Data not ready. {nameof(HttpStatusCode.Accepted)}. Waiting 6 min. now: {DateTime.UtcNow}");
							Thread.Sleep(1000 * 60 * 6);  // wait a bit to have data get prepared by azure
							nextLink = restUrl;  // set next link to same URL for second call
						} else {
							Trace.TraceWarning("NEW RESPONSE TYPE. HANDLE THIS!");
							Trace.TraceWarning($"code:{httpWebResponse.StatusCode} desc:{httpWebResponse.StatusDescription}");
						}
					}
				} finally {
					httpWebResponse?.Dispose();
				}
			} while (nextLink != "");

			return usageRecords;
		}

		public static RateCardPayload GetRateCardInfo(string restUrl, Guid orgId)
		{
			HttpWebResponse httpWebResponse = AzureResourceManagerUtil.RateCardRestApiCall(restUrl, orgId);

			try {
				if (httpWebResponse == null) {
					Trace.TraceWarning("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
					Trace.TraceWarning($"{nameof(httpWebResponse)} == null");
					Trace.TraceWarning($"     {nameof(GetRateCardInfo)}({nameof(restUrl)}, {nameof(orgId)})");
					Trace.TraceWarning($"     {nameof(restUrl)}: {restUrl}");
					Trace.TraceWarning($"     {nameof(orgId)}: {orgId}");
					// throw exception to start from scretch and retry.
					//throw new Exception("Possible reason: Bad request (400), Forbidden (403) to access. Server busy. Client blacklisted.");
				} else {
					// look response codes @ https://msdn.microsoft.com/en-us/library/azure/mt219001.aspx
					// see: https://msdn.microsoft.com/en-us/library/azure/mt219004.aspx
					if (httpWebResponse.StatusCode == HttpStatusCode.OK) {
						Trace.TraceInformation($"Received Rest Call Response: {nameof(HttpStatusCode.OK)}. Processing...");
						string streamContent;

						using (Stream receiveStream = httpWebResponse.GetResponseStream()) {
							// Pipes the stream to a higher level stream reader with the required encoding format. 
							using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8)) {
								streamContent = readStream.ReadToEnd();
							}
						}

						RateCardPayload rateCardPayload = JsonConvert.DeserializeObject<RateCardPayload>(streamContent);
						return rateCardPayload;
					} else if (httpWebResponse.StatusCode == HttpStatusCode.Accepted) {
						Trace.TraceWarning($"Data not ready. {nameof(HttpStatusCode.Accepted)}. Not capable of handling this.");
					} else {
						Trace.TraceWarning("NEW RESPONSE TYPE. HANDLE THIS - GetRateCardInfo!");
						Trace.TraceWarning($"code:{httpWebResponse.StatusCode} desc:{httpWebResponse.StatusDescription}");
					}
				}

				return null;
			} finally {
				httpWebResponse?.Dispose();
			}
		}
	}
}
