using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CreateQuoteFromOpportunity : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Opportunity")]
        [ReferenceTarget("opportunity")]
        public InArgument<EntityReference> Opportunity { get; set; }

        [Output("Quote")]
        [ReferenceTarget("quote")]
        public OutArgument<EntityReference> Quote { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var opportunity = Opportunity.Get(executionContext);
            if (opportunity == null)
            {
                throw new InvalidPluginExecutionException("Opportunity is required.");
            }

            var response = (GenerateQuoteFromOpportunityResponse)common.Service.Execute(new GenerateQuoteFromOpportunityRequest
            {
                OpportunityId = opportunity.Id,
                ColumnSet = new ColumnSet("quoteid", "name")
            });

            var quote = response.Entity;
            common.Trace($"Quote {quote.Id} created from opportunity {opportunity.Id}");

            Quote.Set(executionContext, new EntityReference(quote.LogicalName, quote.Id));
        }
    }
}
