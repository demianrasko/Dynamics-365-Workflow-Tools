using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Count Child Entity Records")]
    public class CountChildEntityRecords : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Child Entity Schema Name")]
        [Default("")]
        public InArgument<string> ChildEntityName { get; set; }

        [RequiredArgument]
        [Input("Parent Lookup Field Name on Child")]
        [Default("")]
        public InArgument<string> ParentLookupName { get; set; }

        [RequiredArgument]
        [Input("Record URL (Parent)")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        [Input("FetchXML Filter (Child)")]
        [ReferenceTarget("")]
        public InArgument<string> FilterExpressionXml { get; set; }

        [Output("Result")]
        public OutArgument<int> Result { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            Result.Set(executionContext, common.CountChildRecords(ChildEntityName.Get(executionContext), ParentLookupName.Get(executionContext),
                RecordURL.Get(executionContext), FilterExpressionXml.Get(executionContext)));
        }
    }
}
