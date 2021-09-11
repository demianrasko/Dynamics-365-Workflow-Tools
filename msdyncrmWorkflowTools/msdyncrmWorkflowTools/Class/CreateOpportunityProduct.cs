using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;


namespace msdyncrmWorkflowTools
{
    public class CreateOpportunityProduct : CodeActivity
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

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion


            EntityReference opportunity = this.Opportunity.Get(executionContext);
            EntityReference existingProduct = this.ExistingProduct.Get(executionContext);
            EntityReference uom = this.UoM.Get(executionContext);
            decimal quantity = this.Quantity.Get(executionContext);

            // Create an opportunity product 
            Entity newOpportunityProduct = new Entity("opportunityproduct");

            newOpportunityProduct["opportunityid"] = new EntityReference(opportunity.LogicalName, opportunity.Id);
            newOpportunityProduct["productid"] = new EntityReference(existingProduct.LogicalName, existingProduct.Id);
            newOpportunityProduct["uomid"] = new EntityReference(uom.LogicalName, uom.Id);
            newOpportunityProduct["quantity"] = quantity;

            Guid _opportunityProduct1Id = new Guid();
            _opportunityProduct1Id = objCommon.service.Create(newOpportunityProduct);
        }
    }
}
