using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class ConcatenateFromQuery : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("FetchXML")]
        [Default("")]
        public InArgument<string> FetchXml { get; set; }

        [Input("AttributeName")]
        [Default("")]
        public InArgument<string> AttributeName { get; set; }

        [Input("Separator")]
        [Default(", ")]
        public InArgument<string> Separator { get; set; }

        [Input("FormatString")]
        [Default("")]
        public InArgument<string> FormatString { get; set; }

        [Input("TopRecordCount")]
        public InArgument<int> TopRecordCount { get; set; }

        [Output("ConcatenatedString")]
        public OutArgument<string> ConcatenatedString { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var fetchXml = FetchXml.Get(executionContext);

            if (string.IsNullOrEmpty(fetchXml))
            {
                throw new InvalidPluginExecutionException("FetchXML is required.");
            }

            fetchXml = fetchXml.Replace("{PARENT_GUID}", common.Context.PrimaryEntityId.ToString());

            var attributeName = AttributeName.Get(executionContext);
            var separator = Separator.Get(executionContext);
            var format = FormatString.Get(executionContext);
            var topRecordCount = TopRecordCount.Get(executionContext);

            common.Trace($"FetchXML={fetchXml}, AttributeName={attributeName}, Separator={separator}, FormatString={format}, TopRecordCount={topRecordCount}");

            var concatenatedString = common.ConcatenateFromQuery(fetchXml, attributeName, separator, format, topRecordCount);

            if (concatenatedString == null)
            {
                common.Trace("No data found to concatenate");
                return;
            }

            common.Trace($"Concatenated string: {concatenatedString}");
            ConcatenatedString.Set(executionContext, concatenatedString);
        }
    }
}
