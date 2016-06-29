//----------------------------------------------------------------------------------------------
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration; // access to configuration files
using System.Data.Entity;
using System.Data.SqlClient;

namespace Commons
{
    public class DataAccess : DbContext
    {
        // Name of the connection string to the SQL Server
        public DataAccess() : base("ASQLConn") { }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<ReportRequest> ReportRequests { get; set; }
        public DbSet<PerUserTokenCache> PerUserTokenCacheList { get; set; }
    }

    public class DataAccessInitializer : System.Data.Entity.CreateDatabaseIfNotExists<DataAccess>
    {
    }

    public class ReportRequest
    {
        [Key]
        public string repReqID { get; set; }
        public DateTime reportDate { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public bool detailedReport { get; set; }
        public bool dailyReport { get; set; }
        public string url { get; set; }
        public List<Report> repReqs { get; set; }

        public ReportRequest()
        {
            repReqs = new List<Report>();
            repReqID = Guid.NewGuid().ToString();
            reportDate = DateTime.UtcNow;
            startDate = DateTime.UtcNow;
            endDate = DateTime.UtcNow;
            detailedReport = false;
            dailyReport = true;
            url = "";
        }
    }

    public class Report
    {
        [Key]
        public string reportID { get; set; }
        public string subscriptionID { get; set; }
        public string organizationID { get; set; }

        public Report()
        {
            reportID = Guid.NewGuid().ToString();
        }
    }

    public class Subscription
    {
        [Key]
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string OrganizationId { get; set; }
        public bool IsConnected { get; set; }
        public DateTime ConnectedOn { get; set; }
        public string ConnectedBy { get; set; }
        public bool AzureAccessNeedsToBeRepaired { get; set; }
        public string DisplayTag { get; set; }
        public DataGenStatus DataGenStatus { get; set; }
        public DateTime DataGenDate { get; set; }

        public Subscription()
        {
            DataGenDate = DateTime.UtcNow;
            DataGenStatus = DataGenStatus.Pending;
        }

        public Subscription(string id, string displayName, string organizationId, bool isConnected, DateTime connectedOn , string connectedBy , bool azureAccessNeedsToBeRepaired, string displayTag , DataGenStatus dataGenStatus, DateTime dataGenDate)
        {
            Id = id;
            DisplayName = displayName;
            OrganizationId = organizationId;
            IsConnected = isConnected;
            ConnectedOn = connectedOn;
            ConnectedBy = connectedBy;
            AzureAccessNeedsToBeRepaired = azureAccessNeedsToBeRepaired;
            DisplayTag = displayTag;
            DataGenStatus = dataGenStatus;
            DataGenDate = dataGenDate;
         }
    }

    public class BillingRequest
    {
        public string SubscriptionId { get; set; }
        public string OrganizationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public BillingRequest() { }

        public BillingRequest(string subscriptionId, string organizationId, DateTime startDate, DateTime endDate)
        {
            SubscriptionId = subscriptionId;
            OrganizationId = organizationId;
            StartDate = startDate;
            EndDate = endDate;
        }
    }

    public enum DataGenStatus
    {
        Completed,
        Running,
        Failed,
        Pending
    }

    public class PerUserTokenCache
    {
        [Key]
        public int Id { get; set; }
        public string webUserUniqueId { get; set; }
        public byte[] cacheBits { get; set; }
        public DateTime LastWrite { get; set; }
    }

    public class Organization
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string objectIdOfUsageServicePrincipal { get; set; }
    }

    public class ContinuationToken
    {
        public string nextLink { get; set; }
    }

    public class UsagePayload
    {
        public List<UsageAggregate> value { get; set; }
    }

    public class UsageAggregate
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public Properties properties { get; set; }
    }

    public class RateCardPayload
    {
        public List<OfferTerms> OfferTerms { get; set; }
        public List<Meter> Meters { get; set; }
        public string Currency { get; set; }
        public string Locale { get; set; }
        public bool IsTaxIncluded { get; set; }
        public string MeterRegion { get; set; }

    }

    public class OfferTerms
    {
        public string Name { get; set; }
        public double Credit { get; set; }
        public List<string> ExcludedMeterIds { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class Meter
    {
        public string MeterId { get; set; }
        public string MeterName { get; set; }
        public string MeterCategory { get; set; }
        public string MeterSubCategory { get; set; }
        public string Unit { get; set; }
        public IDictionary<string, decimal> MeterRates { get; set; }
        public DateTime EffectiveDate { get; set; }
        public Double IncludedQuantity { get; set; }
    }

    public class Properties
    {
        public string subscriptionId { get; set; }
        public string usageStartTime { get; set; }
        public string usageEndTime { get; set; }
        public string meterId { get; set; }
        public InfoFields infoFields { get; set; }

        [JsonProperty("instanceData")]
        public string instanceDataRaw { get; set; }

        public InstanceDataType InstanceData
        {
            get
            {
                //return null; 
                if (instanceDataRaw != null)
                {
                    return JsonConvert.DeserializeObject<InstanceDataType>(instanceDataRaw.Replace("\\\"", ""));
                }
                else
                {
                    return null;
                }
            }
        }

        public double quantity { get; set; }
        public string unit { get; set; }
        public string meterName { get; set; }
        public string meterCategory { get; set; }
        public string meterSubCategory { get; set; }
        public string meterRegion { get; set; }
    }

    public class InfoFields
    {
        public string meteredRegion { get; set; }
        public string meteredService { get; set; }
        public string project { get; set; }
        public string meteredServiceType { get; set; }
        public string serviceInfo1 { get; set; }
    }

    public class InstanceDataType
    {
        [JsonProperty("Microsoft.Resources")]
        public MicrosoftResourcesDataType MicrosoftResources { get; set; }
    }

    public class MicrosoftResourcesDataType
    {
        public string resourceUri { get; set; }
        public string location { get; set; }
        public IDictionary<string, string> tags { get; set; }
        public IDictionary<string, string> additionalInfo { get; set; }
        public string partNumber { get; set; }
        public string orderNumber { get; set; }
    }

    public class UsageRecord
    {
        public Guid uid { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string subscriptionId { get; set; }
        public string usageStartTime { get; set; }
        public string usageEndTime { get; set; }
        public string meterId { get; set; }
        public string meteredRegion { get; set; }
        public string meteredService { get; set; }
        public string project { get; set; }
        public string meteredServiceType { get; set; }
        public string serviceInfo1 { get; set; }
        public string instanceDataRaw { get; set; }
        public string resourceUri { get; set; }
        public string location { get; set; }
        public IDictionary<string, string> tags { get; set; }
        public IDictionary<string, string> additionalInfo { get; set; }
        public string partNumber { get; set; }
        public string orderNumber { get; set; }
        public double quantity { get; set; }
        public string unit { get; set; }
        public string meterName { get; set; }
        public string meterCategory { get; set; }
        public string meterSubCategory { get; set; }
        public string meterRegion { get; set; }
        public decimal cost { get; set; }

        public UsageRecord()
        {
            this.uid = Guid.NewGuid();
        }

        public UsageRecord(UsageAggregate ua)
        {
            this.uid = Guid.NewGuid();
            this.tags = new Dictionary<string, string>();
            this.additionalInfo = new Dictionary<string, string>();

            this.id = "";
            this.name = "";
            this.type = "";
            this.subscriptionId = "";
            this.usageStartTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now).ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            this.usageEndTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now).ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            this.meterId = "";
            this.meterName = "";
            this.meterCategory = "";
            this.meterSubCategory = "";
            this.meterRegion = "";
            this.quantity = 0.0;
            this.unit = "";
            this.instanceDataRaw = "";
            this.meteredRegion = "";
            this.meteredService = "";
            this.meteredServiceType = "";
            this.project = "";
            this.serviceInfo1 = "";
            this.location = "";
            this.orderNumber = "";
            this.partNumber = "";
            this.resourceUri = "";
            this.cost = 0.0M;

            bool notNull = true;
            if (notNull)
            {
                try
                {
                    this.id = ua.id;
                    this.name = ua.name;
                    this.type = ua.type;
                }
                catch (Exception)
                {
                    notNull = false;
                }
            }

            if (!notNull)
                return;

            if (notNull)
            {
                try
                {
                    this.subscriptionId = ua.properties.subscriptionId;
                    this.usageStartTime = ua.properties.usageStartTime;
                    this.usageEndTime = ua.properties.usageEndTime;
                    this.meterId = ua.properties.meterId;
                    this.meterName = ua.properties.meterName;
                    this.meterCategory = ua.properties.meterCategory;
                    this.meterSubCategory = ua.properties.meterSubCategory;
                    this.meterRegion = ua.properties.meterRegion;
                    this.quantity = ua.properties.quantity;
                    this.unit = ua.properties.unit;
                    this.instanceDataRaw = ua.properties.instanceDataRaw;
                }
                catch (Exception)
                {
                    notNull = false;
                }
            }

            if (notNull)
            {
                try
                {
                    this.meteredRegion = ua.properties.infoFields.meteredRegion;
                    this.meteredService = ua.properties.infoFields.meteredService;
                    this.meteredServiceType = ua.properties.infoFields.meteredServiceType;
                    this.project = ua.properties.infoFields.project;
                    this.serviceInfo1 = ua.properties.infoFields.serviceInfo1;
                }
                catch (Exception)
                {
                    notNull = false;
                }
            }

            try
            {
                this.location = ua.properties.InstanceData.MicrosoftResources.location;
                this.orderNumber = ua.properties.InstanceData.MicrosoftResources.orderNumber;
                this.partNumber = ua.properties.InstanceData.MicrosoftResources.partNumber;
                this.resourceUri = ua.properties.InstanceData.MicrosoftResources.resourceUri;
                this.tags = ua.properties.InstanceData.MicrosoftResources.tags;
                this.additionalInfo = ua.properties.InstanceData.MicrosoftResources.additionalInfo;
            }
            catch (Exception)
            {
                notNull = false;
            }
        }
    }

    public static class Utils
    {
        public static void UpdateSubscriptionStatus(string id, DataGenStatus dgs, DateTime dt)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ASQLConn"].ToString();
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                SqlCommand sqlCommand = new SqlCommand(string.Format("UPDATE Subscriptions SET DataGenStatus = {1},  DataGenDate = DATETIMEFROMPARTS({2}, {3}, {4}, {5}, {6}, {7}, {8}) WHERE Id = '{0}'", id, ((int)dgs), dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond), connection);
                connection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception (UpdateSubscriptionStatus): " + e.Message);
            }
        }

        public static List<Subscription> GetSubscriptions()
        {
            List<Subscription> subscriptionList = new List<Subscription>();

            // To prevent DB Connection count problem in other parallel threads with nested loops below, fetch all records (assumed to be small amount) locally.
            string connectionString = ConfigurationManager.ConnectionStrings["ASQLConn"].ToString();
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                SqlCommand sqlCommand = new SqlCommand("SELECT * FROM Subscriptions", connection);
                connection.Open();
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    subscriptionList.Add(new Subscription(Convert.ToString(reader[0]),
                                                          Convert.ToString(reader[1]),
                                                          Convert.ToString(reader[2]),
                                                          Convert.ToBoolean(reader[3]),
                                                          Convert.ToDateTime(reader[4]),
                                                          Convert.ToString(reader[5]),
                                                          Convert.ToBoolean(reader[6]),
                                                          Convert.ToString(reader[7]),
                                                          ((DataGenStatus)Convert.ToInt32(reader[8])),
                                                          Convert.ToDateTime(reader[9])
                                                          ));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception (GetSubscriptions): " + e.Message);
            }

            return subscriptionList;
        }
    }

}