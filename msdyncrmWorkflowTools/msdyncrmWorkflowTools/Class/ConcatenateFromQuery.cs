using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Concatenate From Query")]
    public class ConcatenateFromQuery : WorkflowActivityBase
    {
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            ConcatenatedString.Set(executionContext, common.ConcatenateFromQuery(FetchXml.Get(executionContext), common.Context.PrimaryEntityId,
                AttributeName.Get(executionContext), Separator.Get(executionContext), FormatString.Get(executionContext), TopRecordCount.Get(executionContext)));
        }
    }
}
