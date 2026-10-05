// Not in the Power Platform build: it needs Dynamics 365 tables (listmember).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Remove From All Marketing Lists")]
    public class RemoveFromAllMarketingLists : WorkflowActivityBase
    {
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.RemoveFromAllMarketingLists(new EntityReference(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId));
        }
    }
}
#endif
