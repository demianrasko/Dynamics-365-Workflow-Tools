// Not in the Power Platform build: it needs Dynamics 365 tables (opportunity, product and uom).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
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
            var opportunity = Opportunity.Get(executionContext);
            var existingProduct = ExistingProduct.Get(executionContext);
            var uom = UoM.Get(executionContext);
            var quantity = Quantity.Get(executionContext);

            if (opportunity == null || existingProduct == null || uom == null)
            {
                throw new InvalidPluginExecutionException("Opportunity, Existing Product and Unit are required.");
            }

            var id = common.CreateOpportunityProduct(opportunity, existingProduct, uom, quantity);

            common.Trace($"Opportunity product {id} created");
        }
    }
}
#endif
