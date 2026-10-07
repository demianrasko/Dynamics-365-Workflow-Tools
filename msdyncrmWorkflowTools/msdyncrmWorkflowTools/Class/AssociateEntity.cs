using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Associate Entity")]
    public class AssociateEntity : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Relationship Name")]
        [Default("")]        
        public InArgument<string> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("Relationship Entity Name")]
        [Default("")]
        public InArgument<string> RelationshipEntityName { get; set; }

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.AssociateEntity(common.PrimaryRecord, RelationshipName.Get(executionContext), RelationshipEntityName.Get(executionContext), RecordURL.Get(executionContext));
        }
    }
}
