using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Get Initiating User")]
    public class GetInitiatingUser : WorkflowActivityBase
    {
        [Output("Initiating User")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public OutArgument<EntityReference> InitiatingUser
        {
            get;
            set;
        }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            InitiatingUser.Set(executionContext, new EntityReference(EntityNames.SystemUser, common.Context.InitiatingUserId));
        }
    }
}
