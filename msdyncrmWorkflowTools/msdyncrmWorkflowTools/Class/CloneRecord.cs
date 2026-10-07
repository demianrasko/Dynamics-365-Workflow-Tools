using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Clone Record")]
    public class CloneRecord : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Clonning Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ClonningRecordURL { get; set; }

        [Input("Prefix")]
        [Default("")]
        public InArgument<string> Prefix { get; set; }

        [Input("Fields to Ignore")]
        [Default("")]
        public InArgument<string> FieldstoIgnore { get; set; }

        [Output("Cloned Guid")]
        public OutArgument<string> ClonedGuid { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var createdGuid = common.CloneRecord(ClonningRecordURL.Get(executionContext), FieldstoIgnore.Get(executionContext), Prefix.Get(executionContext));

            ClonedGuid.Set(executionContext, createdGuid.ToString());
        }
    }
}
