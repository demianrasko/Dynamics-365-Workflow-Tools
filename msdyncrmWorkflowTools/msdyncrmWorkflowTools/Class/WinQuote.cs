// Not in the Power Platform build: it needs Dynamics 365 tables (quote).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Win Quote")]
    public class WinQuote : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Quote")]
        [ReferenceTarget(EntityNames.Quote)]
        public InArgument<EntityReference> Quote { get; set; }

        [Input("Message")]
        public InArgument<string> Message { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.WinQuote(Utility.Required(Quote.Get(executionContext), "Quote"), Message.Get(executionContext));
        }
    }
}
#endif
