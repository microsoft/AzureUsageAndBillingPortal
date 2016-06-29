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
using System.Linq;
using System.Web;

using Owin;
using System.Configuration; // access to configuration files
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Web.Mvc;
using System.Security.Claims;  // Claim types
using System.Threading.Tasks;

using Commons;

namespace Registration
{
    public partial class OwinStartup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            string ClientId = ConfigurationManager.AppSettings["ida:ClientID"];
            string Password = ConfigurationManager.AppSettings["ida:Password"];
            string Authority = string.Format(ConfigurationManager.AppSettings["ida:Authority"], "common");
            string GraphAPIIdentifier = ConfigurationManager.AppSettings["ida:GraphAPIIdentifier"];

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions { });
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ClientId,
                    Authority = Authority,
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // we inject our own multitenant validation logic
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        RedirectToIdentityProvider = (context) =>
                        {
                            // This ensures that the address used for sign in and sign out is picked up dynamically from the request
                            // this allows you to deploy your app (to Azure Web Sites, for example) without having to change settings
                            // Remember that the base URL of the address used here must be provisioned in Azure AD beforehand.
                            //string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;

                            object obj = null;
                            if (context.OwinContext.Environment.TryGetValue("Authority", out obj))
                            {
                                string authority = obj as string;
                                if (authority != null)
                                {
                                    context.ProtocolMessage.IssuerAddress = authority;
                                }
                            }
                            if (context.OwinContext.Environment.TryGetValue("DomainHint", out obj))
                            {
                                string domainHint = obj as string;
                                if (domainHint != null)
                                {
                                    context.ProtocolMessage.SetParameter("domain_hint", domainHint);
                                }
                            }
                            context.ProtocolMessage.RedirectUri = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path);
                            context.ProtocolMessage.PostLogoutRedirectUri = new UrlHelper(HttpContext.Current.Request.RequestContext).Action("Index", "Home", null, HttpContext.Current.Request.Url.Scheme);
                            context.ProtocolMessage.Resource = GraphAPIIdentifier;
                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = (context) =>
                        {
                            ClientCredential credential = new ClientCredential(ClientId, Password);
                            string tenantID = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                            string signedInUserUniqueName = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.Name).Value.Split('#')[context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

                            ADALTokenCache cache = new ADALTokenCache(signedInUserUniqueName);

                            cache.Clear();

                            AuthenticationContext authContext = new AuthenticationContext(
                                                                        string.Format("https://login.microsoftonline.com/{0}", tenantID),
                                                                        new ADALTokenCache(signedInUserUniqueName));

                            var items = authContext.TokenCache.ReadItems().ToList();

                            AuthenticationResult result1 = authContext.AcquireTokenByAuthorizationCode(
                                                                                context.Code,
                                                                                new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)),
                                                                                credential);

                            items = authContext.TokenCache.ReadItems().ToList();

                            AuthenticationResult result2 = authContext.AcquireTokenSilent(
                                                                            ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                                                                            credential,
                                                                            new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                            items = authContext.TokenCache.ReadItems().ToList();

                            return Task.FromResult(0);
                        },
                        // we use this notification for injecting our custom logic
                        SecurityTokenValidated = (context) =>
                    {
                        // retriever caller data from the incoming principal
                        string issuer = context.AuthenticationTicket.Identity.FindFirst("iss").Value;
                        if (!issuer.StartsWith("https://sts.windows.net/"))
                            // the caller is not from a trusted issuer - throw to block the authentication flow
                            throw new System.IdentityModel.Tokens.SecurityTokenValidationException();

                        return Task.FromResult(0);
                    },
                        AuthenticationFailed = (context) =>
                        {
                            context.OwinContext.Response.Redirect(new UrlHelper(HttpContext.Current.Request.RequestContext).
                                Action("Error", "Home", new { ExceptionDetails = context.Exception.Message }, HttpContext.Current.Request.Url.Scheme));
                            context.HandleResponse(); // Suppress the exception
                            return Task.FromResult(0);
                        }
                    }
                });
        }
    }
}