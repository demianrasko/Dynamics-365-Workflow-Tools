using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        /// <summary>
        /// Builds the Azure AI Translator v3 request body: <c>[{"Text": "..."}]</c>.
        /// </summary>
        public static string BuildTranslatorRequest(string text)
        {
            return new JArray(new JObject { ["Text"] = text }).ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <summary>
        /// Reads the translated text from an Azure AI Translator v3 response, e.g.
        /// <c>[{"detectedLanguage":{"language":"en","score":1.0},"translations":[{"text":"Hola","to":"es"}]}]</c>.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The service returned an error
        /// (<c>{"error":{"code":401001,"message":"..."}}</c>) or an unexpected response.</exception>
        public static string ParseTranslatorResponse(string response)
        {
            JToken json;
            try
            {
                json = JToken.Parse(response ?? string.Empty);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                throw new InvalidPluginExecutionException($"Translator returned an unexpected response: {response}");
            }

            if (json is JObject error && error["error"] != null)
            {
                throw new InvalidPluginExecutionException(
                    $"Translator error {(string)error["error"]["code"]}: {(string)error["error"]["message"]}");
            }

            var text = json.SelectToken("[0].translations[0].text");
            if (text == null)
            {
                throw new InvalidPluginExecutionException($"Translator returned an unexpected response: {response}");
            }

            return (string)text;
        }
    }
}
