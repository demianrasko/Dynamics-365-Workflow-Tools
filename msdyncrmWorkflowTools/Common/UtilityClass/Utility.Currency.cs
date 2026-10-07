using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using System.IO;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        /// <summary>
        /// Reads the converted amount from a Frankfurter response such as
        /// <c>{"amount":10.0,"base":"USD","date":"2026-10-02","rates":{"EUR":8.9087}}</c>.
        /// </summary>
        /// <param name="response">The response body.</param>
        /// <param name="fromCurrency">Source currency code (used in the error message).</param>
        /// <param name="toCurrency">Target currency code to read from "rates".</param>
        /// <returns>The converted amount.</returns>
        /// <exception cref="InvalidPluginExecutionException">The response has no rate for <paramref name="toCurrency"/>,
        /// e.g. an unsupported currency ({"message":"not found"}).</exception>
        public static decimal ParseCurrencyConversion(string response, string fromCurrency, string toCurrency)
        {
            JObject json;

            try
            {
                using (var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(response ?? string.Empty)))
                {
                    reader.FloatParseHandling = Newtonsoft.Json.FloatParseHandling.Decimal;
                    json = JObject.Load(reader);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                throw new InvalidPluginExecutionException($"Currency conversion from {fromCurrency} to {toCurrency} returned an unexpected response: {response}");
            }

            var rate = json["rates"]?[toCurrency];
            if (rate != null && rate.Type != JTokenType.Null)
            {
                return rate.Value<decimal>();
            }

            var reason = (string)json["message"] ?? "no rate returned";

            throw new InvalidPluginExecutionException(
                $"Currency conversion from {fromCurrency} to {toCurrency} is not available ({reason}). Supported currencies are the ECB reference currencies, e.g. USD, EUR, GBP, JPY, CAD, AUD, CHF.");
        }
    }
}
