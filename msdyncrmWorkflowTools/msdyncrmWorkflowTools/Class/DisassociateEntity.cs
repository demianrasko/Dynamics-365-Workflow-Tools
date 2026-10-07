using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Disassociate Entity")]
    public class DisassociateEntity : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Relationship Name")]
        [Default("")]        
        public InArgument<string> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.DisassociateEntity(common.PrimaryRecord, RelationshipName.Get(executionContext), common.GetRecordReference(RecordURL.Get(executionContext), "Record URL"));
        }
    }
}
