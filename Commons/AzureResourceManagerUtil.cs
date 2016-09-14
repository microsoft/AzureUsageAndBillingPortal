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

namespace Commons
{
    /// <summary>
    /// Azure Graph API wrappers 
    /// Used to give / take user object's consent to AD access
    /// Add Resource manager to access users' AD, resources
    /// </summary>
    public static class AzureResourceManagerUtil
    {
        public static List<Organization> GetUserOrganizations()
        {
            List<Organization> organizations = new List<Organization>();

            string tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(
                                                            ConfigurationManager.AppSettings["ida:ClientID"],
                                                            ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                                                            string.Format(ConfigurationManager.AppSettings["ida:Authority"], tenantId),
                                                            new ADALTokenCache(signedInUserUniqueName));

                var items = authContext.TokenCache.ReadItems().ToList();

                AuthenticationResult result = authContext.AcquireTokenSilent(
                                                            ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                                                            credential,
                                                            new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                items = authContext.TokenCache.ReadItems().ToList();

                // Get a list of Organizations of which the user is a member            
                string requestUrl = string.Format("{0}/tenants?api-version={1}",
                                                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                                                    ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

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

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var organizationsResult = (Json.Decode(responseContent)).value;

                    foreach (var organization in organizationsResult)
                        organizations.Add(new Organization()
                        {
                            Id = organization.tenantId,
                            //DisplayName = AzureADGraphAPIUtil.GetOrganizationDisplayName(organization.tenantId),
                            objectIdOfUsageServicePrincipal =
                                AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInOrganization(organization.tenantId, ConfigurationManager.AppSettings["ida:ClientID"])
                        });
                }
            }
            catch
            {
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                                                        string.Format(ConfigurationManager.AppSettings["ida:Authority"], tenantId),
                                                        new ADALTokenCache(signedInUserUniqueName));

                var items = authContext.TokenCache.ReadItems().ToList();

                AuthenticationResult result = authContext.AcquireToken(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential);

                //(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                //new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                items = authContext.TokenCache.ReadItems().ToList();

                // Get a list of Organizations of which the user is a member            
                string requestUrl = string.Format("{0}/tenants?api-version={1}",
                                                ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                                                ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

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

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var organizationsResult = (Json.Decode(responseContent)).value;

                    foreach (var organization in organizationsResult)
                        organizations.Add(new Organization()
                        {
                            Id = organization.tenantId,
                            //DisplayName = AzureADGraphAPIUtil.GetOrganizationDisplayName(organization.tenantId),
                            objectIdOfUsageServicePrincipal =
                                AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInOrganization(organization.tenantId, ConfigurationManager.AppSettings["ida:ClientID"])
                        });
                }
            }
            return organizations;
        }

        public static List<Subscription> GetUserSubscriptions(string organizationId)
        {
            List<Subscription> subscriptions = null;

            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                                                        string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId),
                                                        new ADALTokenCache(signedInUserUniqueName));

                AuthenticationResult result = authContext.AcquireTokenSilent(
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                                                        credential,
                                                        new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                subscriptions = new List<Subscription>();

                // Get subscriptions to which the user has some kind of access
                string requestUrl = string.Format("{0}/subscriptions?api-version={1}",
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var subscriptionsResult = (Json.Decode(responseContent)).value;

                    foreach (var subscription in subscriptionsResult)
                        subscriptions.Add(new Subscription()
                        {
                            Id = subscription.subscriptionId,
                            DisplayName = subscription.displayName,
                            OrganizationId = organizationId
                        });
                }
            }
            catch
            {

                //ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                //   ConfigurationManager.AppSettings["ida:Password"]);
                //// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                //AuthenticationContext authContext = new AuthenticationContext(
                //    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));
                //string resource = ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"];


                //AuthenticationResult result = authContext.AcquireToken(resource, credential.ClientId,
                //    new UserCredential(signedInUserUniqueName));

                ////authContext.AcquireToken(resource,userAssertion:)

                //    //new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)));

                //    //AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                //    //new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                //subscriptions = new List<Subscription>();

                //// Get subscriptions to which the user has some kind of access
                //string requestUrl = string.Format("{0}/subscriptions?api-version={1}", ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                //    ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

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

        public static bool UserCanManageAccessForSubscription(string subscriptionId, string organizationId)
        {
            bool ret = false;

            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                                                        string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId),
                                                        new ADALTokenCache(signedInUserUniqueName));

                AuthenticationResult result = authContext.AcquireTokenSilent(
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                                                        credential,
                                                        new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                // Get permissions of the user on the subscription
                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                                                        subscriptionId,
                                                        ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

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

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var permissionsResult = (Json.Decode(responseContent)).value;

                    foreach (var permissions in permissionsResult)
                    {
                        bool permissionMatch = false;
                        foreach (string action in permissions.actions)
                        {
                            var actionPattern = "^" + Regex.Escape(action.ToLower()).Replace("\\*", ".*") + "$";
                            permissionMatch = Regex.IsMatch("microsoft.authorization/roleassignments/write", actionPattern);
                            if (permissionMatch) break;
                        }

                        // if one of the actions match, check that the NotActions don't
                        if (permissionMatch)
                        {
                            foreach (string notAction in permissions.notActions)
                            {
                                var notActionPattern = "^" + Regex.Escape(notAction.ToLower()).Replace("\\*", ".*") + "$";
                                if (Regex.IsMatch("microsoft.authorization/roleassignments/write", notActionPattern))
                                    permissionMatch = false;
                                if (!permissionMatch) break;
                            }
                        }

                        if (permissionMatch)
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch { }

            return ret;
        }

        public static bool ServicePrincipalHasReadAccessToSubscription(string subscriptionId, string organizationId)
        {
            bool ret = false;

            try
            {
                // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));
                AuthenticationResult result = authContext.AcquireToken(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential);


                // Get permissions of the app on the subscription
                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], 
                                                        subscriptionId, 
                                                        ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

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

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var permissionsResult = (Json.Decode(responseContent)).value;

                    foreach (var permissions in permissionsResult)
                    {
                        bool permissionMatch = false;
                        foreach (string action in permissions.actions)
                            if (action.Equals("*/read", StringComparison.CurrentCultureIgnoreCase) || action.Equals("*", StringComparison.CurrentCultureIgnoreCase))
                            {
                                permissionMatch = true;
                                break;
                            }

                        // if one of the actions match, check that the NotActions don't
                        if (permissionMatch)
                        {
                            foreach (string notAction in permissions.notActions)
                                if (notAction.Equals("*", StringComparison.CurrentCultureIgnoreCase) || notAction.EndsWith("/read", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    permissionMatch = false;
                                    break;
                                }
                        }

                        if (permissionMatch)
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch { }

            return ret;
        }

        public static void RevokeRoleFromServicePrincipalOnSubscription(string objectId, string subscriptionId, string organizationId)
        {
            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                                                        string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), 
                                                        new ADALTokenCache(signedInUserUniqueName));

                AuthenticationResult result = authContext.AcquireTokenSilent(
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], 
                                                        credential,
                                                        new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));


                // Get rolesAssignments to application on the subscription
                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/roleassignments?api-version={2}&$filter=principalId eq '{3}'",
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                                                        ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"], objectId);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of role assignments
                // properties                                  id                                          type                                        name
                // ----------                                  --                                          ----                                        ----
                // @{roleDefinitionId=/subscriptions/e91d47... /subscriptions/e91d4...1-a796-2...          Microsoft.Authorization/roleAssignments     9db2cd....b1b8

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var roleAssignmentsResult = (Json.Decode(responseContent)).value;

                    //remove all role assignments
                    foreach (var roleAssignment in roleAssignmentsResult)
                    {
                        requestUrl = string.Format("{0}{1}?api-version={2}",
                                                ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], roleAssignment.id,
                                                ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"]);

                        request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                        response = client.SendAsync(request).Result;
                    }
                }
            }
            catch { }
        }

        public static void GrantRoleToServicePrincipalOnSubscription(string objectId, string subscriptionId, string organizationId)
        {
            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                                                        string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), 
                                                        new ADALTokenCache(signedInUserUniqueName));

                AuthenticationResult result = authContext.AcquireTokenSilent(
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                                                        new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                // Create role assignment for application on the subscription
                string roleAssignmentId = Guid.NewGuid().ToString();
                string roleDefinitionId = GetRoleId(ConfigurationManager.AppSettings["ida:RequiredARMRoleOnSubscription"], subscriptionId, organizationId);

                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/roleassignments/{2}?api-version={3}",
                                            ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId, roleAssignmentId,
                                            ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"]);

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                StringContent content = new StringContent("{\"properties\": {\"roleDefinitionId\":\"" + roleDefinitionId + "\",\"principalId\":\"" + objectId + "\"}}");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
                HttpResponseMessage response = client.SendAsync(request).Result;
            }
            catch { }
        }

        public static string GetRoleId(string roleName, string subscriptionId, string organizationId)
        {
            string roleId = null;

            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                                                        string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), 
                                                        new ADALTokenCache(signedInUserUniqueName));
                
                AuthenticationResult result = authContext.AcquireTokenSilent(
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], 
                                                        credential,
                                                        new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                // Get subscriptions to which the user has some kind of access
                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/Microsoft.Authorization/roleDefinitions?api-version={2}",
                                                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                                                        ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleDefinitionsAPIVersion"]);

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

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var roleDefinitionsResult = (Json.Decode(responseContent)).value;

                    foreach (var roleDefinition in roleDefinitionsResult)
                        if ((roleDefinition.properties.roleName as string).Equals(roleName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            roleId = roleDefinition.id;
                            break;
                        }
                }
            }
            catch { }

            return roleId;
        }

        public static HttpWebResponse GetUsage(string subscriptionId, string organizationId, bool dailyReport, bool detailedReport, DateTime startDate, DateTime endDate, string contURL = "")
        {
            //string UsageResponse = "";
            try
            {
                string requesturl = "";

                // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));

                AuthenticationResult result = authContext.AcquireToken(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential);

                if (contURL == "")
                {
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
                    if (!dailyReport)
                        aggregationGranularity = "Hourly";

                    string showDetails = "false";
                    if (detailedReport)
                        showDetails = "true";

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
                }
                else
                {
                    requesturl = contURL;
                }

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
            }
            catch (Exception e)
            {
                Console.WriteLine("GetUsage exception: {0}", e.Message);
            }

            //return UsageResponse;
            return null;
        }

        public static string GetBillingRestApiCallURL(string subscriptionId, bool dailyReport, bool detailedReport, DateTime startDate, DateTime endDate)
        {
            string url = string.Format("https://management.azure.com/subscriptions/{0}/providers/Microsoft.Commerce/UsageAggregates", subscriptionId);
            DateTime dtstart, dtend;
            string reportedStartTime, reportedEndTime;

            // remove minute and seconds part
            if (dailyReport)
            {
                dtstart = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
                dtend = new DateTime(endDate.Year, endDate.Month, endDate.Day, 0, 0, 0);
                reportedStartTime = dtstart.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
                reportedEndTime = dtend.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
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
            if (!dailyReport)
                urlWParameters.AppendFormat("&{0}={1}", "aggregationGranularity", HttpUtility.UrlEncode("Hourly"));
            else
                urlWParameters.AppendFormat("&{0}={1}", "aggregationGranularity", HttpUtility.UrlEncode("Daily"));

            if (detailedReport)
                urlWParameters.AppendFormat("&{0}={1}", "showDetails", HttpUtility.UrlEncode("true"));
            else
                urlWParameters.AppendFormat("&{0}={1}", "showDetails", HttpUtility.UrlEncode("false"));

            return urlWParameters.ToString();
        }

        public static string GetRateCardRestApiCallURL(string subscriptionId, string offerId, string currency, string locale, string regionInfo)
        {
            string url = string.Format("https://management.azure.com/subscriptions/{0}/providers/Microsoft.Commerce/RateCard", subscriptionId);
            
            StringBuilder urlWParameters = new StringBuilder();
            urlWParameters.Append(url);
            urlWParameters.AppendFormat("?{0}={1}", "api-version", HttpUtility.UrlEncode("2015-06-01-preview"));
            urlWParameters.AppendFormat("&$filter=OfferDurableId eq '{0}' and Currency eq '{1}' and Locale eq '{2}' and RegionInfo eq '{3}'", offerId, currency, locale, regionInfo);
            
            return urlWParameters.ToString();
        }

        public static HttpWebResponse RateCardRestApiCall(string requesturl, string organizationId)
        {
            HttpWebResponse response = null;

            try
            {
                // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));

                AuthenticationResult result = authContext.AcquireToken(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential);

                //Crafting the HTTP call
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requesturl);
                request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + result.AccessToken);
                request.ContentType = "application/json";
                //request.KeepAlive = true;
                //request.Timeout = 1000 * 60 * 2;  // 2 minutes
                //request.ReadWriteTimeout = 1000 * 60 * 2;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException webExc)
            {
                Stream str = webExc.Response.GetResponseStream();
                using (StreamReader readStream = new StreamReader(str, Encoding.UTF8))
                {
                    string content = readStream.ReadToEnd();
                    Console.WriteLine("Exception: RateCardRestApiCall->e.message: " + webExc.Message);
                    Console.WriteLine("Response content: " + content);
                }
                response = null;
            }
            catch (Exception e)
            {
                // Exception occurs because of:
                //      "The operation has timed out"
                //      "The remote server returned an error: (403) Forbidden."
                //      "The remote server returned an error: (400) Bad Request."
                //      "The remote server returned an error: (404) Not Found."
                Console.WriteLine("Exception: RateCardRestApiCall->e.message: {0}", e.Message);
                response = null;
            }

            return response;
        }

        public static HttpWebResponse BillingRestApiCall(string requesturl, string organizationId)
        {
            HttpWebResponse response = null;

            try
            {
                // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
                ClientCredential credential = new ClientCredential(
                                                        ConfigurationManager.AppSettings["ida:ClientID"],
                                                        ConfigurationManager.AppSettings["ida:Password"]);

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));

                AuthenticationResult result = authContext.AcquireToken(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential);

                //Crafting the HTTP call
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requesturl);
                request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + result.AccessToken);
                request.ContentType = "application/json";
                //request.KeepAlive = true;
                //request.Timeout = 1000 * 60 * 2;  // 2 minutes
                //request.ReadWriteTimeout = 1000 * 60 * 2;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception e)
            {
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

        public static HttpWebResponse BillingRestApiCall(string subscriptionId, string organizationId, bool dailyReport, bool detailedReport, DateTime startDate, DateTime endDate)
        {
            HttpWebResponse response = null;

            try
            {
                string requesturl = GetBillingRestApiCallURL(subscriptionId, dailyReport, detailedReport, startDate, endDate);
                response = BillingRestApiCall(requesturl, organizationId);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: BillingRestApiCall2-e.message: {0}", e.Message);
            }

            return response;
        }
    }
}