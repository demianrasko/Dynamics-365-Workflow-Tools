// Not in the Power Platform build: it needs Dynamics 365 tables (opportunity, product and uom).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Create Opportunity Product")]
    public class CreateOpportunityProduct : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Opportunity")]
        [ReferenceTarget(EntityNames.Opportunity)]
        public InArgument<EntityReference> Opportunity { get; set; }

        [RequiredArgument]
        [Input("Existing Product")]
        [ReferenceTarget(EntityNames.Product)]
        public InArgument<EntityReference> ExistingProduct { get; set; }

        [RequiredArgument]
        [Input("Unit")]
        [ReferenceTarget(EntityNames.Uom)]
        public InArgument<EntityReference> UoM { get; set; }

        [RequiredArgument]
        [Input("Quantity")]
        public InArgument<decimal> Quantity { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.CreateOpportunityProduct(Opportunity.Get(executionContext), ExistingProduct.Get(executionContext), UoM.Get(executionContext), Quantity.Get(executionContext));
        }
    }
}
#endif
