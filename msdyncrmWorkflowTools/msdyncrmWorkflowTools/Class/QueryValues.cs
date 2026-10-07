using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Query Values")]
    public class QueryValues : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("EntityName")]
        [Default("")]
        public InArgument<string> EntityName { get; set; }

        [RequiredArgument]
        [Input("Attribute1")]
        [ReferenceTarget("")]
        public InArgument<string> Attribute1 { get; set; }

        [RequiredArgument]
        [Input("Attribute2")]
        [ReferenceTarget("")]
        public InArgument<string> Attribute2 { get; set; }

        [RequiredArgument]
        [Input("FilterAttribute1")]
        [ReferenceTarget("")]
        public InArgument<string> FilterAttribute1 { get; set; }

        [RequiredArgument]
        [Input("ValueAttribute1")]
        [ReferenceTarget("")]
        public InArgument<string> ValueAttribute1 { get; set; }

        [Input("FilterAttribute2")]
        [ReferenceTarget("")]
        public InArgument<string> FilterAttribute2 { get; set; }

        [Input("ValueAttribute2")]
        [ReferenceTarget("")]
        public InArgument<string> ValueAttribute2 { get; set; }

        [Output("ResultValue1")]
        public OutArgument<string> ResultValue1 { get; set; }

        [Output("ResultValue2")]
        public OutArgument<string> ResultValue2 { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            ResultValue1.Set(executionContext, common.QueryValues(EntityName.Get(executionContext), Attribute1.Get(executionContext), Attribute2.Get(executionContext),
                FilterAttribute1.Get(executionContext), ValueAttribute1.Get(executionContext), FilterAttribute2.Get(executionContext), ValueAttribute2.Get(executionContext),
                out var value2));
            ResultValue2.Set(executionContext, value2);
        }
    }
}
