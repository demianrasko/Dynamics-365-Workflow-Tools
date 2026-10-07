// Not in the Power Platform build: it needs Dynamics 365 tables (opportunity and quote).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Create Quote From Opportunity")]
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
            Quote.Set(executionContext, common.CreateQuoteFromOpportunity(Utility.Required(Opportunity.Get(executionContext), "Opportunity").Id));
        }
    }
}
#endif
