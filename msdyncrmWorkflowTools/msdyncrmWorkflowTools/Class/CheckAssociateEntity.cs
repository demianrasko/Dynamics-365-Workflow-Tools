using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Check Associate Entity")]
    public class CheckAssociateEntity : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Relationship Name")]
        [Default("")]        
        public InArgument<string> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        [Output("Result")]
        public OutArgument<bool> Result { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            Result.Set(executionContext, common.IsAssociated(common.PrimaryRecord, RelationshipName.Get(executionContext), RecordURL.Get(executionContext)));
        }
    }
}
