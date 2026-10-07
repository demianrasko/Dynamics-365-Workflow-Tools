// Not in the Power Platform build: it needs Dynamics 365 tables (quote).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Update Quote Value")]
    public class UpdateQuoteValue : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Quote")]
        [ReferenceTarget(EntityNames.Quote)]
        public InArgument<EntityReference> Quote { get; set; }

        [RequiredArgument]
        [Input("Discount Amount")]
        public InArgument<decimal> Discountamount { get; set; }

        [RequiredArgument]
        [Input("Field name to update (discountamount)")]
        public InArgument<string> Fieldname { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.SetMoney(Utility.Required(Quote.Get(executionContext), "Quote"), Utility.Required(Fieldname.Get(executionContext), "Field name"), Discountamount.Get(executionContext));
        }
    }
}
#endif
