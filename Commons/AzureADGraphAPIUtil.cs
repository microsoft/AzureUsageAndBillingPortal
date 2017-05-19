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
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Helpers;
using System;
using System.Threading.Tasks;

namespace Commons
{
	public static class AzureAdGraphApiUtil
	{
		private static readonly string ClientId = ConfigurationManager.AppSettings["ida:ClientId"];
		private static readonly string Password = ConfigurationManager.AppSettings["ida:Password"];
		private static readonly string Authority = ConfigurationManager.AppSettings["ida:Authority"];
		private static readonly string GraphApiIdentifier = ConfigurationManager.AppSettings["ida:GraphApiIdentifier"];
		private static readonly string GraphApiVersion = ConfigurationManager.AppSettings["ida:GraphApiVersion"];

		public static string GetObjectIdOfServicePrincipalInOrganization(string organizationId, string applicationId)
		{
			string objectId = null;

			try {
				// Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
				ClientCredential credential = new ClientCredential(ClientId, Password);

				// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
				AuthenticationContext authContext = new AuthenticationContext(String.Format(Authority, organizationId));
				AuthenticationResult result = authContext.AcquireTokenAsync(GraphApiIdentifier, credential).GetAwaiter().GetResult();

				// Get a list of Organizations of which the user is a member
				string requestUrl = $"{GraphApiIdentifier}{organizationId}/servicePrincipals?api-version={GraphApiVersion}&$filter=appId eq '{applicationId}'";

				// Make the GET request
				using (HttpClient client = new HttpClient()) {
					using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl)) {
						request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
						HttpResponseMessage response = client.SendAsync(request).Result;

						// Endpoint should return JSON with one or none serviePrincipal object
						if (response.IsSuccessStatusCode) {
							string responseContent = response.Content.ReadAsStringAsync().Result;
							var servicePrincipalResult = (Json.Decode(responseContent)).value;
							if (servicePrincipalResult != null && servicePrincipalResult.Length > 0) objectId = servicePrincipalResult[0].objectId;
						}
					}
				}
			} catch { }

			return objectId;
		}
	}
}
