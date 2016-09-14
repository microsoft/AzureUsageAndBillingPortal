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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Azure.WebJobs;

using System.Threading;
using System.Configuration;
using Commons;
using System.Data.SqlClient;
using System.Net;
using Newtonsoft.Json;
using System.Data;
using System.Linq;


namespace WebJobBillingData
{
    public class Functions
    {
        private static void CheckParameters(SqlParameterCollection sqlpc)
        {
            foreach (SqlParameter p in sqlpc)
            {
                if (p.Value == null)
                    if (p.SqlDbType == SqlDbType.NVarChar)
                        p.Value = "";
                    else
                        p.Value = DBNull.Value;
            }
        }

        public static void InsertIntoSQLDB(List<UsageRecord> urs)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ASQLConn"].ToString();
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();

                SqlCommand sqlCommand = new SqlCommand("InsertUsageRecord", connection);
                sqlCommand.CommandType = CommandType.StoredProcedure;

                List<SqlParameter> sqlParameters = new List<SqlParameter>();
                sqlParameters.Add(new SqlParameter("@uid", SqlDbType.UniqueIdentifier));
                sqlParameters.Add(new SqlParameter("@id", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@name", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@type", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@subscriptionId", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@usageStartTime", SqlDbType.DateTime));
                sqlParameters.Add(new SqlParameter("@usageEndTime", SqlDbType.DateTime));
                sqlParameters.Add(new SqlParameter("@meterId", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@meteredRegion", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@meteredService", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@project", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@meteredServiceType", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@serviceInfo1", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@instanceDataRaw", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@resourceUri", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@location", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@partNumber", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@orderNumber", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@quantity", SqlDbType.Float));
                sqlParameters.Add(new SqlParameter("@unit", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@meterName", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@meterCategory", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@meterSubCategory", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@meterRegion", SqlDbType.NVarChar));
                sqlParameters.Add(new SqlParameter("@cost", SqlDbType.Decimal));
                sqlCommand.Parameters.AddRange(sqlParameters.ToArray());

                foreach (UsageRecord ur in urs)
                {
                    // check param/value validity
                    sqlCommand.Parameters["@uid"].Value = ur.uid;
                    sqlCommand.Parameters["@id"].Value = ur.id;
                    sqlCommand.Parameters["@name"].Value = ur.name;
                    sqlCommand.Parameters["@type"].Value = ur.type;
                    sqlCommand.Parameters["@subscriptionId"].Value = ur.subscriptionId;
                    sqlCommand.Parameters["@usageStartTime"].Value = Convert.ToDateTime(ur.usageStartTime).ToUniversalTime();
                    sqlCommand.Parameters["@usageEndTime"].Value = Convert.ToDateTime(ur.usageEndTime).ToUniversalTime();
                    sqlCommand.Parameters["@meterId"].Value = ur.meterId;
                    sqlCommand.Parameters["@meteredRegion"].Value = ur.meteredRegion;
                    sqlCommand.Parameters["@meteredService"].Value = ur.meteredService;
                    sqlCommand.Parameters["@project"].Value = ur.project;
                    sqlCommand.Parameters["@meteredServiceType"].Value = ur.meteredServiceType;
                    sqlCommand.Parameters["@serviceInfo1"].Value = ur.serviceInfo1;
                    sqlCommand.Parameters["@instanceDataRaw"].Value = ur.instanceDataRaw;
                    sqlCommand.Parameters["@resourceUri"].Value = ur.resourceUri;
                    sqlCommand.Parameters["@location"].Value = ur.location;
                    sqlCommand.Parameters["@partNumber"].Value = ur.partNumber;
                    sqlCommand.Parameters["@orderNumber"].Value = ur.orderNumber;
                    sqlCommand.Parameters["@quantity"].Value = ur.quantity;
                    sqlCommand.Parameters["@unit"].Value = ur.unit;
                    sqlCommand.Parameters["@meterName"].Value = ur.meterName;
                    sqlCommand.Parameters["@meterCategory"].Value = ur.meterCategory;
                    sqlCommand.Parameters["@meterSubCategory"].Value = ur.meterSubCategory;
                    sqlCommand.Parameters["@meterRegion"].Value = ur.meterRegion;
                    sqlCommand.Parameters["@cost"].Value = ur.cost;

                    CheckParameters(sqlCommand.Parameters);

                    try  // to catch duplicate inserts...
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: Possible Dublicate! InsertIntoSQLDB->e.Message: " + e.Message);
                    }
                }

                connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: InsertIntoSQLDB->e.Message: " + e.Message);
                connection.Close();
            }
        }

        public static List<UsageRecord> GetUsageDetails(string restURL, string orgID, RateCardPayload rateCardInfo)
        {
            string nextLink = "";
            List<UsageRecord> usageRecords = new List<UsageRecord>();

            do
            {
                HttpWebResponse httpWebResponse = null;

                if (nextLink != "")
                    httpWebResponse = AzureResourceManagerUtil.BillingRestApiCall(nextLink, orgID);
                else
                    httpWebResponse = AzureResourceManagerUtil.BillingRestApiCall(restURL, orgID);

                if (httpWebResponse == null)
                {
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.WriteLine("httpWebResponse == null");
                    Console.WriteLine("     GetUsageDetails(string restURL, string orgID)");
                    Console.WriteLine("     restURL: {0}", restURL);
                    Console.WriteLine("     orgID: {0}", orgID);

                    // throw exception to start from scretch and retry.
                    //throw new Exception("Possible reason: Bad request (400), Forbidden (403) to access. Server busy. Client blacklisted.");
                }
                else
                {
                    // look response codes @ https://msdn.microsoft.com/en-us/library/azure/mt219001.aspx
                    if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                    {
                        Console.WriteLine("Received Rest Call Response: HttpStatusCode.OK. Processing...");
                        Stream receiveStream = httpWebResponse.GetResponseStream();

                        // Pipes the stream to a higher level stream reader with the required encoding format. 
                        StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                        string streamContent = readStream.ReadToEnd();

                        UsagePayload usagePayload = JsonConvert.DeserializeObject<UsagePayload>(streamContent);

                        foreach (UsageAggregate ua in usagePayload.value)
                        {
                            //Handle adding cost in
                            var ur = new UsageRecord(ua);
                            try {
                                var meterInfo = rateCardInfo.Meters.Where(p => p.MeterId == ur.meterId).SingleOrDefault();
                                ur.cost = meterInfo.MeterRates["0"] * Convert.ToDecimal(ur.quantity);
                                if (ur.cost < 0.01M)
                                    ur.cost = 0;
                            } catch (Exception ex)
                            {
                                Console.WriteLine("Exception trying to apply cost info for meter: " + ur.meterId);
                            }
                            usageRecords.Add(ur);
                        }

                        ContinuationToken contToken = JsonConvert.DeserializeObject<ContinuationToken>(streamContent);
                        if (contToken.nextLink != null)
                            nextLink = contToken.nextLink;
                        else
                            nextLink = "";
                    }
                    else if (httpWebResponse.StatusCode == HttpStatusCode.Accepted)
                    {
                        Console.WriteLine("Data not ready. HttpStatusCode.Accepted. Waiting 6 min. now: {0}", DateTime.Now.ToString());
                        Thread.Sleep(1000 * 60 * 6);  // wait a bit to have data get prepared by azure
                        nextLink = restURL;  // set next link to same URL for second call
                    }
                    else
                    {
                        Console.WriteLine("NEW RESPONSE TYPE. HANDLE THIS!");
                        Console.WriteLine("code:{0} desc:{1}", httpWebResponse.StatusCode, httpWebResponse.StatusDescription);
                    }
                }
            } while (nextLink != "");

            return usageRecords;
        }

        public static RateCardPayload GetRateCardInfo(string restURL, string orgID)
        {            
            HttpWebResponse httpWebResponse = null;
            
            httpWebResponse = AzureResourceManagerUtil.RateCardRestApiCall(restURL, orgID);

            if (httpWebResponse == null)
            {
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine("httpWebResponse == null");
                Console.WriteLine("     GetRateCardInfo(string restURL, string orgID)");
                Console.WriteLine("     restURL: {0}", restURL);
                Console.WriteLine("     orgID: {0}", orgID);

                // throw exception to start from scretch and retry.
                //throw new Exception("Possible reason: Bad request (400), Forbidden (403) to access. Server busy. Client blacklisted.");
            }
            else
            {
                // look response codes @ https://msdn.microsoft.com/en-us/library/azure/mt219001.aspx
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("Received Rest Call Response: HttpStatusCode.OK. Processing...");
                    Stream receiveStream = httpWebResponse.GetResponseStream();

                    // Pipes the stream to a higher level stream reader with the required encoding format. 
                    StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                    string streamContent = readStream.ReadToEnd();

                    RateCardPayload rateCardPayload = JsonConvert.DeserializeObject<RateCardPayload>(streamContent);
                    return rateCardPayload;
                }
                else if (httpWebResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    Console.WriteLine("Data not ready. HttpStatusCode.Accepted. Not capable of handling this.");
                        
                }
                else
                {
                    Console.WriteLine("NEW RESPONSE TYPE. HANDLE THIS - GetRateCardInfo!");
                    Console.WriteLine("code:{0} desc:{1}", httpWebResponse.StatusCode, httpWebResponse.StatusDescription);
                }
            }
            return null;           
        }

        public static void ProcessQueueMessage([QueueTrigger("billingdatarequests")] BillingRequest br)
        {
            Console.WriteLine("Start webjob process. SubscriptionID: {0}", br.SubscriptionId);
            int retriesLeft = Convert.ToInt32(ConfigurationManager.AppSettings["ida:RetryCountToProcessMessage"].ToString());

            while (retriesLeft > 0)
            {
                --retriesLeft;
                if (retriesLeft < 1)
                {
                    Console.WriteLine("Finished internal retries, throwing exception. Time:{0}", DateTime.UtcNow.ToString());
                    throw new Exception();
                }

                Console.WriteLine("Start time:{0} Retries Left: {1}", DateTime.UtcNow.ToString(), retriesLeft);

                try
                {
                    //Fetch RateCard information First
                    string rateCardURL = AzureResourceManagerUtil.GetRateCardRestApiCallURL(br.SubscriptionId,
                        ConfigurationManager.AppSettings["ida:OfferCode"].ToString(),
                        ConfigurationManager.AppSettings["ida:Currency"].ToString(),
                        ConfigurationManager.AppSettings["ida:Locale"].ToString(),
                        ConfigurationManager.AppSettings["ida:RegionInfo"].ToString());
                    Console.WriteLine("Request cost info from RateCard service.");
                    RateCardPayload rateCardInfo = GetRateCardInfo(rateCardURL, br.OrganizationId);
                    if (rateCardInfo == null)
                    {
                        Console.WriteLine("Problem receiving cost info occured - see log for details.");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Received cost info: " + rateCardInfo.ToString());
                    }

                    // if granularity=hourly then report up to prev. hour,
                    // if granularity=daily then report up to prev. day. Othervise will get 400 error
                    //DateTime sdt = DateTime.Now.AddYears(-3);
                    //DateTime edt = DateTime.Now.AddDays(-1);

                    string restURL = AzureResourceManagerUtil.GetBillingRestApiCallURL(br.SubscriptionId, true, true, br.StartDate, br.EndDate);

                    Console.WriteLine("Request usage data from Billing service.");
                    List<UsageRecord> urs = GetUsageDetails(restURL, br.OrganizationId, rateCardInfo);
                    Console.WriteLine("Received record count: {0}", urs.Count);

                    Console.WriteLine("Insert usage data into SQL Server.");
                    InsertIntoSQLDB(urs);

                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: ProcessQueueMessage->e.Message: " + e.Message);

                    if (retriesLeft == 0)
                        throw;
                }

                Console.WriteLine("Sleeping in ProcessQueueMessage while loop for 5 min. DateTime: {0}", DateTime.Now.ToString());
                Thread.Sleep(1000 * 60 * 5);
            }   // while

            Commons.Utils.UpdateSubscriptionStatus(br.SubscriptionId, DataGenStatus.Completed, DateTime.UtcNow);

            Console.WriteLine("Complete webjob process. SubscriptionID: {0}", br.SubscriptionId);
        }
    }
}
