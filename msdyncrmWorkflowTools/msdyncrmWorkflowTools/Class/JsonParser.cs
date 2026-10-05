using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class JsonParser : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("JSON")]
        [Default("")]
        public InArgument<string> JSON { get; set; }

        [RequiredArgument]
        [Input("JSON Path")]
        [Default("")]
        public InArgument<string> JSONPath { get; set; }

        [Output("JSON Result")]
        public OutArgument<string> JSONResult { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var json = JSON.Get(executionContext);
            var jsonPath = JSONPath.Get(executionContext);

            var res = Utility.JsonParser(json, jsonPath) ?? string.Empty;

            JSONResult.Set(executionContext, res);
        }
    }
}
