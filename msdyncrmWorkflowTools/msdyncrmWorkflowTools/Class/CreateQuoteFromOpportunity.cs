// Not in the Power Platform build: it needs Dynamics 365 tables (opportunity and quote).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CreateQuoteFromOpportunity : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Opportunity")]
        [ReferenceTarget(EntityNames.Opportunity)]
        public InArgument<EntityReference> Opportunity { get; set; }

        [Output("Quote")]
        [ReferenceTarget(EntityNames.Quote)]
        public OutArgument<EntityReference> Quote { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var opportunity = Opportunity.Get(executionContext) ?? throw new InvalidPluginExecutionException("Opportunity is required.");

            Quote.Set(executionContext, common.CreateQuoteFromOpportunity(opportunity.Id));
        }
    }
}
#endif
