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

namespace Commons
{
	public class ReportRequest
	{
		[Key]
		public Guid RepReqId { get; set; }
		public DateTime ReportDate { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public bool DetailedReport { get; set; }
		public bool DailyReport { get; set; }
		public string Url { get; set; }
		public List<Report> RepReqs { get; set; }

		public ReportRequest()
		{
			RepReqs = new List<Report>();
			RepReqId = Guid.NewGuid();
			ReportDate = DateTime.UtcNow;
			StartDate = DateTime.UtcNow;
			EndDate = DateTime.UtcNow;
			DetailedReport = false;
			DailyReport = true;
			Url = "";
		}
	}

	public class Report
	{
		[Key]
		public Guid ReportId { get; set; }
		public Guid SubscriptionId { get; set; }
		public Guid OrganizationId { get; set; }

		public Report()
		{
			ReportId = Guid.NewGuid();
		}
	}

	public class Subscription
	{
		[Key]
		public Guid Id { get; set; }
		public string DisplayName { get; set; }
		public Guid OrganizationId { get; set; }
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

		public override string ToString()
		{
			return $"{DisplayName} ({Id})";
		}
	}

	public class BillingRequest
	{
		public Guid SubscriptionId { get; private set; }
		public Guid OrganizationId { get; private set; }
		public DateTime StartDate { get; private set; }
		public DateTime EndDate { get; private set; }

		public BillingRequest() { }

		public BillingRequest(Guid subscriptionId, Guid organizationId, DateTime startDate, DateTime endDate)
		{
			SubscriptionId = subscriptionId;
			OrganizationId = organizationId;
			StartDate = startDate;
			EndDate = endDate;
		}

		public override string ToString()
		{
			return $"sub:{SubscriptionId}, org:{OrganizationId}, start: {StartDate}, end: {EndDate}";
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
		public string WebUserUniqueId { get; set; }
		public byte[] CacheBits { get; set; }
		public DateTime LastWrite { get; set; }
	}

	public class Organization
	{
		public Guid Id { get; set; }
		public string DisplayName { get; set; }
		public string ObjectIdOfUsageServicePrincipal { get; set; }
	}

	public class ContinuationToken
	{
		public string NextLink { get; set; }
	}

	public class UsagePayload
	{
		public List<UsageAggregate> Value { get; set; }
	}

	public class UsageAggregate
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public Properties Properties { get; set; }
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
		public double IncludedQuantity { get; set; }
	}

	public class Properties
	{
		public string SubscriptionId { get; set; }
		public string UsageStartTime { get; set; }
		public string UsageEndTime { get; set; }
		public string MeterId { get; set; }
		public InfoFields InfoFields { get; set; }

		[JsonProperty("instanceData")]
		public string InstanceDataRaw { get; set; }

		public InstanceDataType InstanceData {
			get {
				// return null
				if (InstanceDataRaw != null) {
					return JsonConvert.DeserializeObject<InstanceDataType>(InstanceDataRaw.Replace("\\\"", ""));
				} else {
					return null;
				}
			}
		}

		public double Quantity { get; set; }
		public string Unit { get; set; }
		public string MeterName { get; set; }
		public string MeterCategory { get; set; }
		public string MeterSubCategory { get; set; }
		public string MeterRegion { get; set; }
	}

	public class InfoFields
	{
		public string MeteredRegion { get; set; }
		public string MeteredService { get; set; }
		public string Project { get; set; }
		public string MeteredServiceType { get; set; }
		public string ServiceInfo1 { get; set; }
	}

	public class InstanceDataType
	{
		[JsonProperty("Microsoft.Resources")]
		public MicrosoftResourcesDataType MicrosoftResources { get; set; }
	}

	public class MicrosoftResourcesDataType
	{
		public string ResourceUri { get; set; }
		public string Location { get; set; }
		public IDictionary<string, string> Tags { get; set; }
		public IDictionary<string, string> AdditionalInfo { get; set; }
		public string PartNumber { get; set; }
		public Guid? OrderNumber { get; set; }
	}

	public class UsageRecord
	{
		public Guid Uid { get; private set; }
		public string Id { get; private set; }
		public string Name { get; private set; }
		public string Type { get; private set; }
		public Guid SubscriptionId { get; private set; }
		public DateTime UsageStartTime { get; private set; }
		public DateTime UsageEndTime { get; private set; }
		public string MeterId { get; private set; }
		public string MeteredRegion { get; private set; }
		public string MeteredService { get; private set; }
		public string Project { get; private set; }
		public string MeteredServiceType { get; private set; }
		public string ServiceInfo1 { get; private set; }
		internal string InstanceDataRaw { get; private set; } // internal
		public string ResourceUri { get; private set; }
		public string Location { get; private set; }
		public IDictionary<string, string> Tags { get; private set; }
		public IDictionary<string, string> AdditionalInfo { get; private set; }
		public string PartNumber { get; private set; }
		public Guid? OrderNumber { get; private set; }
		public double Quantity { get; private set; }
		public string Unit { get; private set; }
		public string MeterName { get; private set; }
		public string MeterCategory { get; private set; }
		public string MeterSubCategory { get; private set; }
		public string MeterRegion { get; private set; }
		public double Cost { get; set; }

		public UsageRecord()
		{
			this.Uid = Guid.NewGuid();
		}

		public UsageRecord(UsageAggregate ua)
		{
			this.Uid = Guid.NewGuid();
			this.Id = "";
			this.Name = "";
			this.Type = "";
			this.SubscriptionId = Guid.Empty;
			this.UsageStartTime = DateTime.MinValue;
			this.UsageEndTime = DateTime.MinValue;
			this.MeterId = "";
			this.MeterName = "";
			this.MeterCategory = "";
			this.MeterSubCategory = "";
			this.MeterRegion = "";
			this.Quantity = 0.0;
			this.Unit = "";
			this.InstanceDataRaw = "";
			this.MeteredRegion = "";
			this.MeteredService = "";
			this.MeteredServiceType = "";
			this.Project = "";
			this.ServiceInfo1 = "";
			this.Location = "";
			this.OrderNumber = null;
			this.PartNumber = "";
			this.ResourceUri = "";
			this.Cost = 0.0;

			this.Id = ua.Id;
			this.Name = ua.Name;
			this.Type = ua.Type;

			if (ua.Properties != null) {
				this.SubscriptionId = new Guid(ua.Properties.SubscriptionId);
				this.UsageStartTime = Convert.ToDateTime(ua.Properties.UsageStartTime).ToUniversalTime();
				this.UsageEndTime = Convert.ToDateTime(ua.Properties.UsageEndTime).ToUniversalTime();
				this.MeterId = ua.Properties.MeterId ?? "";
				this.MeterName = ua.Properties.MeterName ?? "";
				this.MeterCategory = ua.Properties.MeterCategory ?? "";
				this.MeterSubCategory = ua.Properties.MeterSubCategory ?? "";
				this.MeterRegion = ua.Properties.MeterRegion ?? "";
				this.Quantity = ua.Properties.Quantity;
				this.Unit = ua.Properties.Unit ?? "";
				this.InstanceDataRaw = ua.Properties.InstanceDataRaw;
			}

			if (ua.Properties?.InfoFields != null) {
				this.MeteredRegion = ua.Properties.InfoFields.MeteredRegion ?? "";
				this.MeteredService = ua.Properties.InfoFields.MeteredService ?? "";
				this.MeteredServiceType = ua.Properties.InfoFields.MeteredServiceType ?? "";
				this.Project = ua.Properties.InfoFields.Project ?? "";
				this.ServiceInfo1 = ua.Properties.InfoFields.ServiceInfo1 ?? "";
			}

			if (ua.Properties?.InstanceData?.MicrosoftResources != null) {
				this.Location = ua.Properties.InstanceData.MicrosoftResources.Location ?? "";
				this.OrderNumber = ua.Properties.InstanceData.MicrosoftResources.OrderNumber;
				this.PartNumber = ua.Properties.InstanceData.MicrosoftResources.PartNumber ?? "";
				this.ResourceUri = ua.Properties.InstanceData.MicrosoftResources.ResourceUri ?? "";
				this.Tags = ua.Properties.InstanceData.MicrosoftResources.Tags;
				this.AdditionalInfo = ua.Properties.InstanceData.MicrosoftResources.AdditionalInfo;
			}
		}
	}
}
