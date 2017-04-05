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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using System.Net;
using System.Web;
using System.Text;
using System.IO;

/*
see:
https://azure.microsoft.com/en-us/documentation/articles/billing-usage-rate-card-overview/
https://msdn.microsoft.com/pl-pl/library/azure/mt219001 (!)
https://msdn.microsoft.com/en-us/library/azure/dn848368.aspx
*/

namespace Commons
{
	/// <summary>
	/// Azure Graph API wrappers 
	/// Used to give / take user object's consent to AD access
	/// Add Resource manager to access users' AD, resources
	/// </summary>
	public static class AzureResourceManagerUtil
	{
		private static readonly string ClientId = ConfigurationManager.AppSettings["ida:ClientId"];
		private static readonly string Password = ConfigurationManager.AppSettings["ida:Password"];
		private static readonly string Authority = ConfigurationManager.AppSettings["ida:Authority"];
		private static readonly string AzureResourceManagerIdentifier = ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"];
		private static readonly string AzureResourceManagerUrl = ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"];
		private static readonly string AzureResourceManagerApiVersion = ConfigurationManager.AppSettings["ida:AzureResourceManagerApiVersion"];
		private static readonly string ArmAuthorizationPermissionsApiVersion = ConfigurationManager.AppSettings["ida:ArmAuthorizationPermissionsApiVersion"];
		private static readonly string ArmAuthorizationRoleAssignmentsApiVersion = ConfigurationManager.AppSettings["ida:ArmAuthorizationRoleAssignmentsApiVersion"];
		private static readonly string ArmAuthorizationRoleDefinitionsApiVersion = ConfigurationManager.AppSettings["ida:ArmAuthorizationRoleDefinitionsApiVersion"];
		private static readonly string RequiredArmRoleOnSubscription = ConfigurationManager.AppSettings["ida:RequiredArmRoleOnSubscription"];

		public static List<Organization> GetUserOrganizations()
		{
			List<Organization> organizations = new List<Organization>();

			string tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
			string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

			try {
				// Aquire Access Token to call Azure Resource Manager
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(String.Format(Authority, tenantId), new AdalTokenCache(signedInUserUniqueName));

				var items = authContext.TokenCache.ReadItems().ToList();

				AuthenticationResult result = authContext.AcquireTokenSilentAsync(AzureResourceManagerIdentifier, credential,
					new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)).GetAwaiter().GetResult();

				items = authContext.TokenCache.ReadItems().ToList();

				// Get a list of Organizations of which the user is a member            
				string requestUrl = $"{AzureResourceManagerUrl}/tenants?api-version={AzureResourceManagerApiVersion}";

				// Make the GET request
				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				HttpResponseMessage response = client.SendAsync(request).Result;

				// Endpoint returns JSON with an array of Tenant Objects
				// id                                            tenantId
				// --                                            --------
				// /tenants/7fe8....304dfa0 7fe877...fa0
				// /tenants/62e1.....c1aa24 62e......a24

				if (response.IsSuccessStatusCode) {
					string responseContent = response.Content.ReadAsStringAsync().Result;
					var organizationsResult = (Json.Decode(responseContent)).value;

					foreach (var organization in organizationsResult) {
						organizations.Add(new Organization() {
							Id = new Guid(organization.tenantId),
							//DisplayName = AzureADGraphAPIUtil.GetOrganizationDisplayName(organization.tenantId),
							ObjectIdOfUsageServicePrincipal = AzureAdGraphApiUtil.GetObjectIdOfServicePrincipalInOrganization(organization.tenantId, ClientId)
						});
					}
				}
			} catch {
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(String.Format(Authority, tenantId), new AdalTokenCache(signedInUserUniqueName));

				var items = authContext.TokenCache.ReadItems().ToList();

				AuthenticationResult result = authContext.AcquireTokenAsync(AzureResourceManagerIdentifier, credential).GetAwaiter().GetResult();

				//(_azureResourceManagerIdentifier, credential,
				//new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

				items = authContext.TokenCache.ReadItems().ToList();

				// Get a list of Organizations of which the user is a member            
				string requestUrl = $"{AzureResourceManagerUrl}/tenants?api-version={AzureResourceManagerApiVersion}";

				// Make the GET request
				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				HttpResponseMessage response = client.SendAsync(request).Result;

				// Endpoint returns JSON with an array of Tenant Objects
				// id                                            tenantId
				// --                                            --------
				// /tenants/7fe87...4dfa0 7fe8...a0
				// /tenants/62e1......a24 62....a24

				if (response.IsSuccessStatusCode) {
					string responseContent = response.Content.ReadAsStringAsync().Result;
					var organizationsResult = (Json.Decode(responseContent)).value;

					foreach (var organization in organizationsResult) {
						organizations.Add(new Organization() {
							Id = new Guid(organization.tenantId),
							//DisplayName = AzureADGraphAPIUtil.GetOrganizationDisplayName(organization.tenantId),
							ObjectIdOfUsageServicePrincipal = AzureAdGraphApiUtil.GetObjectIdOfServicePrincipalInOrganization(organization.tenantId, ClientId)
						});
					}
				}
			}
			return organizations;
		}

		public static List<Subscription> GetUserSubscriptions(Guid organizationId)
		{
			List<Subscription> subscriptions = null;

			string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

			try {
				// Aquire Access Token to call Azure Resource Manager
				ClientCredential credential = new ClientCredential(
					ClientId,
					Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(
					String.Format(Authority, organizationId),
					new AdalTokenCache(signedInUserUniqueName));

				AuthenticationResult result = authContext.AcquireTokenSilentAsync(
					AzureResourceManagerIdentifier, credential,
					new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)).GetAwaiter().GetResult();

				subscriptions = new List<Subscription>();

				// Get subscriptions to which the user has some kind of access
				string requestUrl = $"{AzureResourceManagerUrl}/subscriptions?api-version={AzureResourceManagerApiVersion}";

				// Make the GET request
				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				HttpResponseMessage response = client.SendAsync(request).Result;

				if (response.IsSuccessStatusCode) {
					string responseContent = response.Content.ReadAsStringAsync().Result;
					var subscriptionsResult = (Json.Decode(responseContent)).value;

					foreach (var subscription in subscriptionsResult) {
						subscriptions.Add(new Subscription() {
							Id = new Guid(subscription.subscriptionId),
							DisplayName = subscription.displayName,
							OrganizationId = organizationId
						});
					}
				}
			} catch {
				//ClientCredential credential = new ClientCredential(_applicationId,
				//   _password);
				//// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				//AuthenticationContext authContext = new AuthenticationContext(
				//    String.Format(_authority, organizationId), new ADALTokenCache(signedInUserUniqueName));
				//string resource = _azureResourceManagerIdentifier;


				//AuthenticationResult result = authContext.AcquireToken(resource, credential.ClientId,
				//    new UserCredential(signedInUserUniqueName));

				////authContext.AcquireToken(resource,userAssertion:)

				//    //new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)));

				//    //AcquireTokenSilent(_azureResourceManagerIdentifier, credential,
				//    //new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

				//subscriptions = new List<Subscription>();

				//// Get subscriptions to which the user has some kind of access
				//string requestUrl = String.Format("{0}/subscriptions?api-version={1}", _azureResourceManagerUrl,
				//    _azureResourceManagerAPIVersion);

				//// Make the GET request
				//HttpClient client = new HttpClient();
				//HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				//request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				//HttpResponseMessage response = client.SendAsync(request).Result;

				//if (response.IsSuccessStatusCode)
				//{
				//    string responseContent = response.Content.ReadAsStringAsync().Result;
				//    var subscriptionsResult = (Json.Decode(responseContent)).value;

				//    foreach (var subscription in subscriptionsResult)
				//        subscriptions.Add(new Subscription()
				//        {
				//            Id = subscription.subscriptionId,
				//            DisplayName = subscription.displayName,
				//            OrganizationId = organizationId
				//        });
				//}
			}

			return subscriptions;
		}

		public static bool UserCanManageAccessForSubscription(Guid subscriptionId, Guid organizationId)
		{
			bool ret = false;

			string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

			try {
				// Aquire Access Token to call Azure Resource Manager
				ClientCredential credential = new ClientCredential(
					ClientId,
					Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(
					String.Format(Authority, organizationId),
					new AdalTokenCache(signedInUserUniqueName));

				AuthenticationResult result = authContext.AcquireTokenSilentAsync(
					AzureResourceManagerIdentifier,
					credential, new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)).GetAwaiter().GetResult();

				// Get permissions of the user on the subscription
				string requestUrl = $"{AzureResourceManagerUrl}/subscriptions/{subscriptionId}/providers/microsoft.authorization/permissions?api-version={ArmAuthorizationPermissionsApiVersion}";

				// Make the GET request
				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				HttpResponseMessage response = client.SendAsync(request).Result;

				// Endpoint returns JSON with an array of Actions and NotActions
				// actions  notActions
				// -------  ----------
				// {*}      {Microsoft.Authorization/*/Write, Microsoft.Authorization/*/Delete}
				// {*/read} {}

				if (response.IsSuccessStatusCode) {
					string responseContent = response.Content.ReadAsStringAsync().Result;
					var permissionsResult = (Json.Decode(responseContent)).value;

					foreach (var permissions in permissionsResult) {
						bool permissionMatch = false;
						foreach (string action in permissions.actions) {
							var actionPattern = "^" + Regex.Escape(action.ToLower()).Replace("\\*", ".*") + "$";
							permissionMatch = Regex.IsMatch("microsoft.authorization/roleassignments/write", actionPattern);
							if (permissionMatch) break;
						}

						// if one of the actions match, check that the NotActions don't
						if (permissionMatch) {
							foreach (string notAction in permissions.notActions) {
								var notActionPattern = "^" + Regex.Escape(notAction.ToLower()).Replace("\\*", ".*") + "$";
								if (Regex.IsMatch("microsoft.authorization/roleassignments/write", notActionPattern)) permissionMatch = false;
								if (!permissionMatch) break;
							}
						}

						if (permissionMatch) {
							ret = true;
							break;
						}
					}
				}
			} catch { }

			return ret;
		}

		public static bool ServicePrincipalHasReadAccessToSubscription(Guid subscriptionId, Guid organizationId)
		{
			bool ret = false;

			try {
				// Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(String.Format(Authority, organizationId));
				AuthenticationResult result = authContext.AcquireTokenAsync(AzureResourceManagerIdentifier, credential).GetAwaiter().GetResult();

				// Get permissions of the app on the subscription
				string requestUrl = $"{AzureResourceManagerUrl}/subscriptions/{subscriptionId}/providers/microsoft.authorization/permissions?api-version={ArmAuthorizationPermissionsApiVersion}";

				// Make the GET request
				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				HttpResponseMessage response = client.SendAsync(request).Result;

				// Endpoint returns JSON with an array of Actions and NotActions
				// actions  notActions
				// -------  ----------
				// {*}      {Microsoft.Authorization/*/Write, Microsoft.Authorization/*/Delete}
				// {*/read} {}

				if (response.IsSuccessStatusCode) {
					string responseContent = response.Content.ReadAsStringAsync().Result;
					var permissionsResult = (Json.Decode(responseContent)).value;

					foreach (var permissions in permissionsResult) {
						bool permissionMatch = false;

						foreach (string action in permissions.actions) {
							if (action.Equals("*/read", StringComparison.CurrentCultureIgnoreCase) || action.Equals("*", StringComparison.CurrentCultureIgnoreCase)) {
								permissionMatch = true;
								break;
							}
						}

						// if one of the actions match, check that the NotActions don't
						if (permissionMatch) {
							foreach (string notAction in permissions.notActions) {
								if (notAction.Equals("*", StringComparison.CurrentCultureIgnoreCase) || notAction.EndsWith("/read", StringComparison.CurrentCultureIgnoreCase)) {
									permissionMatch = false;
									break;
								}
							}
						}

						if (permissionMatch) {
							ret = true;
							break;
						}
					}
				}
			} catch { }

			return ret;
		}

		public static void RevokeRoleFromServicePrincipalOnSubscription(string objectId, Guid subscriptionId, Guid organizationId)
		{
			string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

			try {
				// Aquire Access Token to call Azure Resource Manager
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(
					String.Format(Authority, organizationId),
					new AdalTokenCache(signedInUserUniqueName));

				AuthenticationResult result = authContext.AcquireTokenSilentAsync(
					AzureResourceManagerIdentifier, credential,
					new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)).GetAwaiter().GetResult();

				// Get rolesAssignments to application on the subscription
				string requestUrl = $"{AzureResourceManagerUrl}/subscriptions/{subscriptionId}/providers/microsoft.authorization/roleassignments?api-version={ArmAuthorizationRoleAssignmentsApiVersion}&$filter=principalId eq '{objectId}'";

				// Make the GET request
				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				HttpResponseMessage response = client.SendAsync(request).Result;

				// Endpoint returns JSON with an array of role assignments
				// properties                                  id                                          type                                        name
				// ----------                                  --                                          ----                                        ----
				// @{roleDefinitionId=/subscriptions/e91d47... /subscriptions/e91d4...1-a796-2...          Microsoft.Authorization/roleAssignments     9db2cd....b1b8

				if (response.IsSuccessStatusCode) {
					string responseContent = response.Content.ReadAsStringAsync().Result;
					var roleAssignmentsResult = (Json.Decode(responseContent)).value;

					//remove all role assignments
					foreach (var roleAssignment in roleAssignmentsResult) {
						requestUrl = $"{AzureResourceManagerUrl}{roleAssignment.id}?api-version={ArmAuthorizationRoleAssignmentsApiVersion}";
						request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
						request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
						response = client.SendAsync(request).Result;
					}
				}
			} catch { }
		}

		public static void GrantRoleToServicePrincipalOnSubscription(string objectId, Guid subscriptionId, Guid organizationId)
		{
			string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

			try {
				// Aquire Access Token to call Azure Resource Manager
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(
					String.Format(Authority, organizationId),
					new AdalTokenCache(signedInUserUniqueName));

				AuthenticationResult result = authContext.AcquireTokenSilentAsync(
					AzureResourceManagerIdentifier, credential,
					new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)).GetAwaiter().GetResult();

				// Create role assignment for application on the subscription
				string roleAssignmentId = Guid.NewGuid().ToString();
				string roleDefinitionId = GetRoleId(RequiredArmRoleOnSubscription, subscriptionId, organizationId);

				string requestUrl = String.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/roleassignments/{2}?api-version={3}",
					AzureResourceManagerUrl, subscriptionId, roleAssignmentId,
					ArmAuthorizationRoleAssignmentsApiVersion);

				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				StringContent content = new StringContent("{\"properties\": {\"roleDefinitionId\":\"" + roleDefinitionId + "\",\"principalId\":\"" + objectId + "\"}}");
				content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				request.Content = content;
				HttpResponseMessage response = client.SendAsync(request).Result;
			} catch { }
		}

		public static string GetRoleId(string roleName, Guid subscriptionId, Guid organizationId)
		{
			string roleId = null;

			string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

			try {
				// Aquire Access Token to call Azure Resource Manager
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(
					String.Format(Authority, organizationId),
					new AdalTokenCache(signedInUserUniqueName));

				AuthenticationResult result = authContext.AcquireTokenSilentAsync(
					AzureResourceManagerIdentifier, credential,
					new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)).GetAwaiter().GetResult();

				// Get subscriptions to which the user has some kind of access
				string requestUrl = String.Format("{0}/subscriptions/{1}/providers/Microsoft.Authorization/roleDefinitions?api-version={2}",
					AzureResourceManagerUrl, subscriptionId,
					ArmAuthorizationRoleDefinitionsApiVersion);

				// Make the GET request
				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				HttpResponseMessage response = client.SendAsync(request).Result;

				// Endpoint returns JSON with an array of roleDefinition Objects
				// properties                                  id                                          type                                        name
				// ----------                                  --                                          ----                                        ----
				// @{roleName=Contributor; type=BuiltInRole... /subscriptions/e91.7c4-7.a.6-2...           Microsoft.Authorization/roleDefinitions     b249...24c
				// @{roleName=Owner; type=BuiltInRole; desc... /subscriptions/e91.7c4-7.96-2...            Microsoft.Authorization/roleDefinitions     8e3.....35
				// @{roleName=Reader; type=BuiltInRole; des... /subscriptions/e91.c4-.6-2...               Microsoft.Authorization/roleDefinitions     acd.....e7
				// ...

				if (response.IsSuccessStatusCode) {
					string responseContent = response.Content.ReadAsStringAsync().Result;
					var roleDefinitionsResult = (Json.Decode(responseContent)).value;

					foreach (var roleDefinition in roleDefinitionsResult) {
						if ((roleDefinition.properties.roleName as string).Equals(roleName, StringComparison.CurrentCultureIgnoreCase)) {
							roleId = roleDefinition.id;
							break;
						}
					}
				}
			} catch {
				// ignore
			}

			return roleId;
		}

		public static HttpWebResponse GetUsage(Guid subscriptionId, Guid organizationId, bool dailyReport, bool detailedReport, DateTime startDate, DateTime endDate, string contURL = "")
		{
			//string UsageResponse = "";
			try {
				string requesturl = "";

				// Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(String.Format(Authority, organizationId));

				AuthenticationResult result = authContext.AcquireTokenAsync(AzureResourceManagerIdentifier, credential).GetAwaiter().GetResult();

				if (contURL == "") {
					string apiVersion = "2015-06-01-preview";

					/* STATIC PARAMETERS FOR DEBUG
					string aggregationGranularity = "Daily";
					string showDetails = "false";
					DateTime d = DateTime.UtcNow;
					string reportedEndTime = String.Format("{0}-{1:00}-{2:00}T{3:00}%3a{4:00}%3a{5:00}%2b00%3a00", d.Year, d.Month, d.Day, 0, 0, 0);
					d = d.AddMonths(-1);
					string reportedstartTime = String.Format("{0}-{1:00}-{2:00}T{3:00}%3a{4:00}%3a{5:00}%2b00%3a00", d.Year, d.Month, d.Day, 0, 0, 0);
					*/

					string aggregationGranularity = "Daily";
					if (!dailyReport) aggregationGranularity = "Hourly";

					string showDetails = "false";
					if (detailedReport) showDetails = "true";

					DateTime d = endDate;
					string reportedEndTime = String.Format("{0}-{1:00}-{2:00}T{3:00}%3a{4:00}%3a{5:00}%2b00%3a00", d.Year, d.Month, d.Day, 0, 0, 0);
					d = startDate;
					string reportedstartTime = String.Format("{0}-{1:00}-{2:00}T{3:00}%3a{4:00}%3a{5:00}%2b00%3a00", d.Year, d.Month, d.Day, 0, 0, 0);

					//Making a call to the Azure Usage API for a set time frame with the input AzureSubID
					string baseurl = String.Format("https://management.azure.com/subscriptions/{0}/providers/Microsoft.Commerce/UsageAggregates", subscriptionId);
					requesturl = String.Format("{0}?api-version={1}&reportedstartTime={2}&reportedEndTime={3}&aggregationGranularity={4}&showDetails={5}",
						baseurl,
						apiVersion,
						reportedstartTime,
						reportedEndTime,
						aggregationGranularity,
						showDetails);
				} else {
					requesturl = contURL;
				}

				// TODO: process instanceData when showDetails=true

				//Crafting the HTTP call
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requesturl);
				request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + result.AccessToken);
				request.ContentType = "application/json";
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();

				// info for webjob console @ manage.windowsazure.com...
				//Console.WriteLine("Response code: " + response.StatusDescription);

				return response;

				//Stream receiveStream = response.GetResponseStream();

				//// Pipes the stream to a higher level stream reader with the required encoding format. 
				//StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
				//UsageResponse = readStream.ReadToEnd();
			} catch (Exception e) {
				Console.WriteLine("GetUsage exception: {0}", e.Message);
			}

			//return UsageResponse;
			return null;
		}

		public static string GetBillingRestApiCallUrl(Guid subscriptionId, bool dailyReport, bool detailedReport, DateTime startDate, DateTime endDate)
		{
			string url = String.Format("https://management.azure.com/subscriptions/{0}/providers/Microsoft.Commerce/UsageAggregates", subscriptionId);
			DateTime dtstart, dtend;
			string reportedStartTime, reportedEndTime;

			// remove minute and seconds part
			if (dailyReport) {
				dtstart = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
				dtend = new DateTime(endDate.Year, endDate.Month, endDate.Day, 0, 0, 0);
				reportedStartTime = dtstart.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
				reportedEndTime = dtend.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
			} else {
				dtstart = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, 0, 0);
				dtend = new DateTime(endDate.Year, endDate.Month, endDate.Day, endDate.Hour, 0, 0);
				reportedStartTime = TimeZoneInfo.ConvertTimeToUtc(dtstart).ToString("s", System.Globalization.CultureInfo.InvariantCulture);
				reportedEndTime = TimeZoneInfo.ConvertTimeToUtc(dtend).ToString("s", System.Globalization.CultureInfo.InvariantCulture);
			}

			StringBuilder urlWParameters = new StringBuilder();
			urlWParameters.Append(url);
			urlWParameters.AppendFormat("?{0}={1}", "api-version", HttpUtility.UrlEncode("2015-06-01-preview"));
			urlWParameters.AppendFormat("&{0}={1}", "reportedStartTime", HttpUtility.UrlEncode(reportedStartTime));
			urlWParameters.AppendFormat("&{0}={1}", "reportedEndTime", HttpUtility.UrlEncode(reportedEndTime));

			if (!dailyReport) {
				urlWParameters.AppendFormat("&{0}={1}", "aggregationGranularity", HttpUtility.UrlEncode("Hourly"));
			} else {
				urlWParameters.AppendFormat("&{0}={1}", "aggregationGranularity", HttpUtility.UrlEncode("Daily"));
			}

			if (detailedReport) {
				urlWParameters.AppendFormat("&{0}={1}", "showDetails", HttpUtility.UrlEncode("true"));
			} else {
				urlWParameters.AppendFormat("&{0}={1}", "showDetails", HttpUtility.UrlEncode("false"));
			}

			return urlWParameters.ToString();
		}

		public static string GetRateCardRestApiCallURL(Guid subscriptionId, string offerId, string currency, string locale, string regionInfo)
		{
			string url = String.Format("https://management.azure.com/subscriptions/{0}/providers/Microsoft.Commerce/RateCard", subscriptionId);

			StringBuilder urlWParameters = new StringBuilder();
			urlWParameters.Append(url);
			urlWParameters.AppendFormat("?{0}={1}", "api-version", HttpUtility.UrlEncode("2015-06-01-preview"));
			urlWParameters.AppendFormat("&$filter=OfferDurableId eq '{0}' and Currency eq '{1}' and Locale eq '{2}' and RegionInfo eq '{3}'", offerId, currency, locale, regionInfo);

			return urlWParameters.ToString();
		}

		public static HttpWebResponse RateCardRestApiCall(string requestUrl, Guid organizationId)
		{
			try {
				// Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				string authority = Authority;
				AuthenticationContext authContext = new AuthenticationContext(String.Format(authority, organizationId));

				string resourceManagerIdentifier = AzureResourceManagerIdentifier;
				AuthenticationResult result = authContext.AcquireTokenAsync(resourceManagerIdentifier, credential).GetAwaiter().GetResult();

				//Crafting the HTTP call
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
				request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + result.AccessToken);
				request.ContentType = "application/json";
				//request.KeepAlive = true;
				//request.Timeout = 1000 * 60 * 2;  // 2 minutes
				//request.ReadWriteTimeout = 1000 * 60 * 2;

				WebResponse response = request.GetResponse();
				return response as HttpWebResponse;
			} catch (WebException e) {
				Stream str = e.Response.GetResponseStream();

				using (StreamReader readStream = new StreamReader(str, Encoding.UTF8)) {
					string content = readStream.ReadToEnd();
					Console.WriteLine("Exception: RateCardRestApiCall1-e.message: " + e.Message);
					Console.WriteLine("Response content: " + content);
				}

				return null;
			} catch (Exception e) {
				// Exception occurs because of:
				//      "The operation has timed out"
				//      "The remote server returned an error: (403) Forbidden."
				//      "The remote server returned an error: (400) Bad Request."
				//      "The remote server returned an error: (404) Not Found."
				Console.WriteLine("Exception: RateCardRestApiCall1-e.message: " + e.Message);

				return null;
			}
		}

		public static HttpWebResponse BillingRestApiCall(string requestUrl, Guid organizationId)
		{
			HttpWebResponse response = null;

			try {
				// Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(String.Format(Authority, organizationId));

				AuthenticationResult result = authContext.AcquireTokenAsync(AzureResourceManagerIdentifier, credential).GetAwaiter().GetResult();

				//Crafting the HTTP call
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
				request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + result.AccessToken);
				request.ContentType = "application/json";
				//request.KeepAlive = true;
				//request.Timeout = 1000 * 60 * 2;  // 2 minutes
				//request.ReadWriteTimeout = 1000 * 60 * 2;

				response = (HttpWebResponse)request.GetResponse();
			} catch (Exception e) {
				// Exception occurs because of:
				//      "The operation has timed out"
				//      "The remote server returned an error: (403) Forbidden."
				//      "The remote server returned an error: (400) Bad Request."
				//      "The remote server returned an error: (404) Not Found."
				Console.WriteLine("Exception: BillingRestApiCall1-e.message: {0}", e.Message);
				response = null;
			}

			return response;
		}

		public static HttpWebResponse BillingRestApiCall(Guid subscriptionId, Guid organizationId, bool dailyReport, bool detailedReport, DateTime startDate, DateTime endDate)
		{
			HttpWebResponse response = null;

			try {
				string requesturl = GetBillingRestApiCallUrl(subscriptionId, dailyReport, detailedReport, startDate, endDate);
				response = BillingRestApiCall(requesturl, organizationId);
			} catch (Exception e) {
				Console.WriteLine("Exception: BillingRestApiCall2-e.message: {0}", e.Message);
			}

			return response;
		}
	}
}
