using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class GetInitiatingUser : WorkflowActivityBase
    {
        [Output("Initiating User")]
        [ReferenceTarget("systemuser")]
        public OutArgument<EntityReference> InitiatingUser
        {
            get;
            set;
        }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            InitiatingUser.Set(executionContext, new EntityReference("systemuser", common.Context.InitiatingUserId));
        }
    }
}
