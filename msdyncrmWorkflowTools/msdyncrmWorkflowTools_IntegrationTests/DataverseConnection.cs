using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Connects the integration tests to a real Dataverse environment from a connection string in an environment
    /// variable (AuthType=ClientSecret;Url=...;ClientId=...;ClientSecret=...[;TenantId=...]), as saved by
    /// tools\Set-DataverseTestConnections.ps1. Uses only the SDK's OrganizationWebProxyClient and an OAuth token,
    /// so no extra packages are needed.
    /// </summary>
    internal static class DataverseConnection
    {
        private static readonly ConcurrentDictionary<string, IOrganizationService> Services = new ConcurrentDictionary<string, IOrganizationService>();

        /// <summary>
        /// The organization service for the environment in <paramref name="variableName"/>, or null when the variable
        /// isn't set (the caller marks its tests inconclusive).
        /// </summary>
        public static IOrganizationService Connect(string variableName)
        {
            var connectionString = Environment.GetEnvironmentVariable(variableName)
                ?? Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            return Services.GetOrAdd(variableName, _ => Create(Parse(connectionString)));
        }

        private static Dictionary<string, string> Parse(string connectionString)
        {
            return connectionString
                .Split(';')
                .Select(part => part.Split(new[] { '=' }, 2))
                .Where(pair => pair.Length == 2)
                .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim(), StringComparer.OrdinalIgnoreCase);
        }

        private static IOrganizationService Create(Dictionary<string, string> settings)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var url = settings["Url"].TrimEnd('/');
            settings.TryGetValue("TenantId", out var tenantId);

            using (var http = new HttpClient())
            {
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = FindTenant(http, url);
                }

                var token = GetToken(http, tenantId, settings["ClientId"], settings["ClientSecret"], url);

                // registering an assembly or importing a solution can take Dataverse several minutes
                return new OrganizationWebProxyClient(new Uri($"{url}/XRMServices/2011/Organization.svc/web?SdkClientVersion=9.2"), TimeSpan.FromMinutes(15), false)
                {
                    HeaderToken = token
                };
            }
        }

        /// <summary>The tenant of an environment, from the challenge an unauthenticated request gets back.</summary>
        private static string FindTenant(HttpClient http, string url)
        {
            var response = http.GetAsync($"{url}/api/data/v9.2/").GetAwaiter().GetResult();
            var challenge = string.Join(" ", response.Headers.WwwAuthenticate.Select(h => h.ToString()));
            var match = Regex.Match(challenge, @"login\.microsoftonline\.com/([0-9a-fA-F-]{36})");

            if (!match.Success)
            {
                throw new InvalidOperationException($"Could not find the tenant of {url}; add TenantId=... to the connection string.");
            }

            return match.Groups[1].Value;
        }

        private static string GetToken(HttpClient http, string tenantId, string clientId, string clientSecret, string url)
        {
            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = $"{url}/.default"
            });

            var response = http.PostAsync($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", body).GetAwaiter().GetResult();
            var json = JObject.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Could not get a token for {url}: {(string)json["error_description"]}");
            }

            return (string)json["access_token"];
        }
    }
}
