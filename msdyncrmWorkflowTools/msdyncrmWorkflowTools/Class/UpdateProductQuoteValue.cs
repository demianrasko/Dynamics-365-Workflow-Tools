// Not in the Power Platform build: it needs Dynamics 365 tables (quotedetail).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Update Product Quote Value")]
    public class UpdateProductQuoteValue : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Quote Product")]
        [ReferenceTarget(EntityNames.QuoteDetail)]
        public InArgument<EntityReference> Quote { get; set; }

        [RequiredArgument]
        [Input("Discount Amount")]
        public InArgument<decimal> Discountamount { get; set; }

        [RequiredArgument]
        [Input("Field name to update (manualdiscountamount)")]
        public InArgument<string> Fieldname { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.SetMoney(Utility.Required(Quote.Get(executionContext), "Quote Product"), Utility.Required(Fieldname.Get(executionContext), "Field name"), Discountamount.Get(executionContext));
        }
    }
}
#endif
