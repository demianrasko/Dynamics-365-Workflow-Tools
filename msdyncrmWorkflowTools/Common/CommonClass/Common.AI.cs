using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// Classifies text into one of the given categories (Dataverse AIClassify, AI Builder).
        /// </summary>
        /// <remarks>
        /// Newer environments require AllowMultipleCategories and older ones reject it, so it is sent, and the request
        /// is sent again without it when the environment doesn't know the parameter.
        /// </remarks>
        public string AIClassify(string text, IEnumerable<string> categories)
        {
            var parameters = new Dictionary<string, object>
            {
                ["Text"] = text,
                ["Categories"] = categories.ToArray(),
                ["AllowMultipleCategories"] = false
            };

            try
            {
                return ExecuteAiFunction("AIClassify", "Classification", parameters);
            }
            catch (FaultException<OrganizationServiceFault> ex) when (ex.Detail?.Message?.Contains("Unrecognized request parameter: AllowMultipleCategories") == true)
            {
                parameters.Remove("AllowMultipleCategories");

                return ExecuteAiFunction("AIClassify", "Classification", parameters);
            }
        }

        /// <summary>
        /// Drafts a reply to a customer message (Dataverse AIReply, AI Builder).
        /// </summary>
        public string AIReply(string text)
        {
            return ExecuteAiFunction("AIReply", "PreparedResponse", new Dictionary<string, object> { ["Text"] = text });
        }

        /// <summary>
        /// The sentiment of a text, e.g. "Positive" (Dataverse AISentiment, AI Builder).
        /// </summary>
        public string AISentiment(string text)
        {
            return ExecuteAiFunction("AISentiment", "AnalyzedSentiment", new Dictionary<string, object> { ["Text"] = text });
        }

        /// <summary>
        /// Summarizes a text (Dataverse AISummarize, AI Builder).
        /// </summary>
        public string AISummarize(string text)
        {
            return ExecuteAiFunction("AISummarize", "SummarizedText", new Dictionary<string, object> { ["Text"] = text });
        }

        /// <summary>
        /// Summarizes a record (Dataverse AISummarizeRecord, AI Builder).
        /// </summary>
        /// <param name="record">The record to summarize.</param>
        /// <param name="includeCatchup">Merge the "catch up" changes into the summary (leads and opportunities only).</param>
        /// <param name="recordContext">Optional JSON with extra context for the summary.</param>
        public string AISummarizeRecord(EntityReference record, bool includeCatchup, string recordContext)
        {
            var parameters = new Dictionary<string, object>
            {
                ["EntityLogicalName"] = record.LogicalName,
                ["Id"] = record.Id.ToString()
            };

            if (includeCatchup)
            {
                parameters["IsMergedCatchupAndSummary"] = true;
            }

            if (!string.IsNullOrWhiteSpace(recordContext))
            {
                parameters["RecordContext"] = recordContext;
            }

            return ExecuteAiFunction("AISummarizeRecord", "SummarizedText", parameters);
        }

        /// <summary>
        /// Translates a text (Dataverse AITranslate, AI Builder).
        /// </summary>
        /// <param name="text">The text to translate.</param>
        /// <param name="targetLanguage">Target language code, e.g. "fr"; empty for the service default.</param>
        public string AITranslate(string text, string targetLanguage)
        {
            var parameters = new Dictionary<string, object> { ["Text"] = text };

            if (!string.IsNullOrWhiteSpace(targetLanguage))
            {
                parameters["TargetLanguage"] = targetLanguage.Trim();
            }

            return ExecuteAiFunction("AITranslate", "TranslatedText", parameters);
        }

        private string ExecuteAiFunction(string messageName, string resultName, IDictionary<string, object> parameters)
        {
            var request = new OrganizationRequest(messageName);

            foreach (var parameter in parameters)
            {
                request[parameter.Key] = parameter.Value;
            }

            Trace($"{messageName}: {string.Join(", ", parameters.Keys)}");

            var response = Service.Execute(request);

            if (!response.Results.Contains(resultName))
            {
                throw new InvalidPluginExecutionException($"{messageName} response missing '{resultName}'.");
            }

            return response.Results[resultName] as string ?? string.Empty;
        }
    }
}
