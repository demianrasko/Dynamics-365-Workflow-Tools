using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CreateOpportunityProduct : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Opportunity")]
        [ReferenceTarget("opportunity")]
        public InArgument<EntityReference> Opportunity { get; set; }

        [RequiredArgument]
        [Input("Existing Product")]
        [ReferenceTarget("product")]
        public InArgument<EntityReference> ExistingProduct { get; set; }

        [RequiredArgument]
        [Input("Unit")]
        [ReferenceTarget("uom")]
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

            var opportunityProduct = new Entity("opportunityproduct")
            {
                ["opportunityid"] = new EntityReference(opportunity.LogicalName, opportunity.Id),
                ["productid"] = new EntityReference(existingProduct.LogicalName, existingProduct.Id),
                ["uomid"] = new EntityReference(uom.LogicalName, uom.Id),
                ["quantity"] = quantity
            };

            var id = common.Service.Create(opportunityProduct);
            common.Trace($"Opportunity product {id} created");
        }
    }
}
