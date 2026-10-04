using Microsoft.Xrm.Sdk;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class RemoveFromAllMarketingLists : WorkflowActivityBase
    {
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.RemoveFromAllMarketingLists(new EntityReference(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId));
        }
    }
}
