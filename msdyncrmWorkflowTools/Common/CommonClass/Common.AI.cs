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
        /// <exception cref="InvalidPluginExecutionException">The text is empty, or there are fewer than two categories.</exception>
        public string AIClassify(string text, IEnumerable<string> categories)
        {
            var categoryList = categories?.ToList() ?? new List<string>();

            if (string.IsNullOrWhiteSpace(text) || categoryList.Count == 0)
            {
                throw new InvalidPluginExecutionException("Text or Categories are empty.");
            }

            if (categoryList.Count < 2)
            {
                throw new InvalidPluginExecutionException("At least two categories are required.");
            }

            Trace($"Text length: {text.Length}, categories: {string.Join("|", categoryList)}");

            var parameters = new Dictionary<string, object>
            {
                ["Text"] = text,
                ["Categories"] = categoryList.ToArray(),
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
        /// <exception cref="InvalidPluginExecutionException">The text is empty.</exception>
        public string AIReply(string text)
        {
            RequireText(text);

            return ExecuteAiFunction("AIReply", "PreparedResponse", new Dictionary<string, object> { ["Text"] = text });
        }

        /// <summary>
        /// The sentiment of a text, e.g. "Positive" (Dataverse AISentiment, AI Builder).
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The text is empty.</exception>
        public string AISentiment(string text)
        {
            RequireText(text);

            return ExecuteAiFunction("AISentiment", "AnalyzedSentiment", new Dictionary<string, object> { ["Text"] = text });
        }

        /// <summary>
        /// Summarizes a text (Dataverse AISummarize, AI Builder).
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The text is empty.</exception>
        public string AISummarize(string text)
        {
            RequireText(text);

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
            Trace($"EntityName={record.LogicalName}--Id={record.Id}, include catchup: {includeCatchup}");

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
        /// Summarizes the record a required record URL points at (see <see cref="AISummarizeRecord(EntityReference, bool, string)"/>).
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The record URL is empty.</exception>
        public string AISummarizeRecord(string recordUrl, bool includeCatchup, string recordContext)
        {
            return AISummarizeRecord(GetRecordReference(recordUrl, "Record URL"), includeCatchup, recordContext);
        }

        /// <summary>
        /// Translates a text (Dataverse AITranslate, AI Builder).
        /// </summary>
        /// <param name="text">The text to translate.</param>
        /// <param name="targetLanguage">Target language code, e.g. "fr"; empty for the service default.</param>
        /// <exception cref="InvalidPluginExecutionException">The text is empty.</exception>
        public string AITranslate(string text, string targetLanguage)
        {
            RequireText(text);
            Trace($"Target language: '{targetLanguage}'");

            var parameters = new Dictionary<string, object> { ["Text"] = text };

            if (!string.IsNullOrWhiteSpace(targetLanguage))
            {
                parameters["TargetLanguage"] = targetLanguage.Trim();
            }

            return ExecuteAiFunction("AITranslate", "TranslatedText", parameters);
        }

        private void RequireText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidPluginExecutionException("Text is empty.");
            }

            Trace($"Text length: {text.Length}");
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
