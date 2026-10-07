using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        // Shared HttpClient for the external web services
        private static readonly HttpClient HttpClient;

        /// <summary>
        /// Class Constructor: Inits Singletion objects
        /// </summary>
        static Utility()
        {
            //Set up a common HttpClient as a best practice to avoid leaving open connections
            //more details https://docs.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/
            HttpClient = new HttpClient();
            HttpClient.Timeout = new TimeSpan(0, 0, 30); //30 second timeout as recommend by Microsoft Support to prevent TimeOut on Sandbox
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
        }

        private static void Trace(ITracingService tracingService, string message)
        {
            tracingService?.Trace("{0}", message);
        }

        /// <summary>
        /// Translates text with Azure AI Translator (Text Translation v3.0).
        /// </summary>
        /// <param name="textToTranslate">Text to translate; the source language is detected automatically.</param>
        /// <param name="language">Target language code, e.g. "en", "es", "pt", "fr-ca".</param>
        /// <param name="key">Translator resource key (Ocp-Apim-Subscription-Key).</param>
        /// <param name="region">Azure region of the Translator resource, e.g. "westeurope". Required for regional and
        /// multi-service resources; leave empty for a global Translator resource.</param>
        /// <param name="tracingService">Optional tracing service for the response.</param>
        /// <returns>The translated text, or an empty string when there is nothing to translate.</returns>
        public static string TranslateText(string textToTranslate, string language, string key, string region = null, ITracingService tracingService = null)
        {
            if (string.IsNullOrEmpty(textToTranslate))
            {
                return string.Empty;
            }

            var url = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=" +
                      Uri.EscapeDataString((language ?? string.Empty).Trim());

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(BuildTranslatorRequest(textToTranslate), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);

            if (!string.IsNullOrWhiteSpace(region))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Region", region.Trim());
            }

            var response = ExecuteAsyncRequest(request, tracingService);
            Trace(tracingService, $"Translator response: {response}");

            return ParseTranslatorResponse(response);
        }

        /// <summary>
        /// Converts an amount between currencies using the European Central Bank reference rates published by
        /// Frankfurter (https://frankfurter.dev, free, no API key). Rates are updated once per working day.
        /// </summary>
        /// <remarks>Only the ~30 currencies the ECB publishes are supported (USD, EUR, GBP, JPY, CAD, AUD, ...).</remarks>
        /// <param name="amount">Amount in <paramref name="fromCurrency"/>.</param>
        /// <param name="fromCurrency">ISO 4217 code, e.g. USD.</param>
        /// <param name="toCurrency">ISO 4217 code, e.g. EUR.</param>
        /// <param name="tracingService">Optional tracing service for the request and response.</param>
        /// <returns>The converted amount in <paramref name="toCurrency"/>.</returns>
        public static decimal CurrencyConvert(decimal amount, string fromCurrency, string toCurrency, ITracingService tracingService = null)
        {
            var from = (fromCurrency ?? string.Empty).Trim().ToUpperInvariant();
            var to = (toCurrency ?? string.Empty).Trim().ToUpperInvariant();

            if (from == to)
            {
                return amount;
            }

            var url = FormattableString.Invariant(
                $"https://api.frankfurter.dev/v1/latest?amount={amount}&base={Uri.EscapeDataString(from)}&symbols={Uri.EscapeDataString(to)}");

            Trace(tracingService, $"Currency conversion request: {url}");
            var response = ExecuteAsyncRequest(new HttpRequestMessage(HttpMethod.Get, url), tracingService);
            Trace(tracingService, $"Currency conversion response: {response}");

            return ParseCurrencyConversion(response, from, to);
        }

        /// <summary>
        /// Finds the latitude and longitude of an address with Azure Maps when an Azure Maps key is given,
        /// otherwise with Bing Maps (Bing Maps for Enterprise keys stop working on June 30, 2028).
        /// </summary>
        /// <param name="address">The address, e.g. "1 Microsoft Way, Redmond, WA".</param>
        /// <param name="bingMapsKey">Bing Maps key; ignored when <paramref name="azureMapsKey"/> is set.</param>
        /// <param name="azureMapsKey">Azure Maps subscription key; empty to use Bing Maps.</param>
        /// <param name="tracingService">Optional tracing service for the request.</param>
        /// <returns>The location, or null when the address is empty or was not found.</returns>
        public static GeoLocation GeocodeAddress(string address, string bingMapsKey, string azureMapsKey = null, ITracingService tracingService = null)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            var useAzureMaps = !string.IsNullOrWhiteSpace(azureMapsKey);
            Trace(tracingService, $"Geocoding with {(useAzureMaps ? "Azure Maps" : "Bing Maps")}");

            var url = useAzureMaps ? BuildAzureMapsGeocodeUrl(address, azureMapsKey) : BuildBingGeocodeUrl(address, bingMapsKey);
            var response = ExecuteAsyncRequest(new HttpRequestMessage(HttpMethod.Get, url), tracingService);

            return useAzureMaps ? ParseAzureMapsGeocodeResponse(response) : ParseBingGeocodeResponse(response);
        }

        /// <summary>
        /// Bing Maps Locations API request for an address (one result).
        /// </summary>
        public static string BuildBingGeocodeUrl(string address, string key)
        {
            var query = Uri.EscapeDataString((address ?? string.Empty).Trim());

            return $"https://dev.virtualearth.net/REST/v1/Locations?maxResults=1&query={query}&key={Uri.EscapeDataString((key ?? string.Empty).Trim())}";
        }

        /// <summary>
        /// Azure Maps Geocoding API (2023-06-01) request for an address (one result).
        /// </summary>
        public static string BuildAzureMapsGeocodeUrl(string address, string key)
        {
            var query = Uri.EscapeDataString((address ?? string.Empty).Trim());

            return $"https://atlas.microsoft.com/geocode?api-version=2023-06-01&top=1&query={query}&subscription-key={Uri.EscapeDataString((key ?? string.Empty).Trim())}";
        }

        /// <summary>
        /// Reads the first location from a Bing Maps Locations response.
        /// </summary>
        /// <returns>The location, or null when nothing was found.</returns>
        /// <exception cref="InvalidPluginExecutionException">Bing Maps returned an error.</exception>
        public static GeoLocation ParseBingGeocodeResponse(string json)
        {
            var root = JObject.Parse(json);
            var statusCode = (int?)root["statusCode"];

            if (statusCode.HasValue && statusCode.Value != 200)
            {
                var details = root["errorDetails"]?.Values<string>().ToArray() ?? Array.Empty<string>();
                var message = details.Length > 0 ? string.Join(" ", details) : (string)root["statusDescription"];

                throw new InvalidPluginExecutionException($"Bing Maps error {statusCode}: {message}");
            }

            var resource = root.SelectToken("resourceSets[0].resources[0]");
            var coordinates = resource?.SelectToken("geocodePoints[0].coordinates") ?? resource?.SelectToken("point.coordinates");

            // Bing returns [latitude, longitude]
            return coordinates == null ? null : new GeoLocation((decimal)coordinates[0], (decimal)coordinates[1]);
        }

        /// <summary>
        /// Reads the first location from an Azure Maps Geocoding response (GeoJSON).
        /// </summary>
        /// <returns>The location, or null when nothing was found.</returns>
        /// <exception cref="InvalidPluginExecutionException">Azure Maps returned an error.</exception>
        public static GeoLocation ParseAzureMapsGeocodeResponse(string json)
        {
            var root = JObject.Parse(json);
            var error = root["error"];

            if (error != null)
            {
                throw new InvalidPluginExecutionException($"Azure Maps error {(string)error["code"]}: {(string)error["message"]}");
            }

            var coordinates = root.SelectToken("features[0].geometry.coordinates");

            // GeoJSON is [longitude, latitude]
            return coordinates == null ? null : new GeoLocation((decimal)coordinates[1], (decimal)coordinates[0]);
        }

        /// <summary>
        /// Sends an HTTP request synchronously (shared HttpClient, 30-second timeout) and returns the response body.
        /// </summary>
        /// <remarks>
        /// An error status with a JSON body is returned to the caller, which turns the API's own error message into a
        /// readable exception (Frankfurter, Translator). Any other error status throws with the status and the start of the body.
        /// </remarks>
        /// <param name="message">The request to send.</param>
        /// <param name="tracingService">Optional tracing service for the response status.</param>
        /// <returns>The response body.</returns>
        private static string ExecuteAsyncRequest(HttpRequestMessage message, ITracingService tracingService)
        {
            HttpResponseMessage response;
            string body;

            try
            {
                // Task.Run keeps the async calls off any synchronization context; GetResult blocks without spinning
                response = Task.Run(() => HttpClient.SendAsync(message)).GetAwaiter().GetResult();
                body = Task.Run(() => response.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException($"Timeout waiting for HttpResponse {message.Method}:{message.RequestUri}");
            }

            Trace(tracingService, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} from {message.RequestUri.Host}");

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

            if (response.IsSuccessStatusCode ||
                mediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return body;
            }

            var detail = body.Length > 500 ? $"{body.Substring(0, 500)}..." : body;
            throw new InvalidPluginExecutionException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} from {message.RequestUri.Host}: {detail}");
        }
    }
}
