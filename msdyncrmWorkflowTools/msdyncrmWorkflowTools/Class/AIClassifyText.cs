using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public class AIClassifyText : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text To Classify")]
        public InArgument<String> TextToClassify { get; set; }

        [RequiredArgument]
        [Input("Categories (Comma Separated)")]
        public InArgument<String> CategoriesCsv { get; set; }

        [Output("Classification")]
        public OutArgument<String> TopCategory { get; set; }

        [Output("Failed")]
        [Default("false")]
        public OutArgument<bool> Failed { get; set; }

        [Output("Failure Message")]
        public OutArgument<String> FailureMessage { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String textToClassify = this.TextToClassify.Get(executionContext);
            String categoriesCsv = this.CategoriesCsv.Get(executionContext);

            objCommon.tracingService.Trace(String.Format("AIClassifyText - Text length: {0}", textToClassify == null ? 0 : textToClassify.Length));
            #endregion

            if (String.IsNullOrWhiteSpace(textToClassify) || String.IsNullOrWhiteSpace(categoriesCsv))
            {
                this.Failed.Set(executionContext, true);
                this.TopCategory.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, "Text or Categories are empty.");
                return;
            }

            try
            {
                List<String> categories = categoriesCsv
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !String.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (categories.Count < 2)
                {
                    this.Failed.Set(executionContext, true);
                    this.TopCategory.Set(executionContext, String.Empty);
                    this.FailureMessage.Set(executionContext, "At least two categories are required.");
                    return;
                }

                objCommon.tracingService.Trace(String.Format("AIClassifyText - Categories: {0}", String.Join("|", categories)));

                OrganizationRequest request = new OrganizationRequest("AIClassify");
                request["Categories"] = categories.ToArray();
                request["Text"] = textToClassify;

                OrganizationResponse response = objCommon.service.Execute(request);

                if (!response.Results.Contains("Classification"))
                {
                    this.Failed.Set(executionContext, true);
                    this.TopCategory.Set(executionContext, String.Empty);
                    this.FailureMessage.Set(executionContext, "AIClassify response missing 'Classification'.");
                    return;
                }

                String classification = response["Classification"] as String;

                this.TopCategory.Set(executionContext, classification ?? String.Empty);
                this.Failed.Set(executionContext, false);
                this.FailureMessage.Set(executionContext, String.Empty);
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace(String.Format("AIClassifyText - Error: {0}", ex.ToString()));
                this.Failed.Set(executionContext, true);
                this.TopCategory.Set(executionContext, String.Empty);
                this.FailureMessage.Set(executionContext, ex.Message);
            }
        }
    }
}
