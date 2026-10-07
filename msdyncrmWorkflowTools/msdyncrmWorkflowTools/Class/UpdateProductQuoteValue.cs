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
            var record = Quote.Get(executionContext);
            var fieldName = Fieldname.Get(executionContext);

            if (record == null || string.IsNullOrEmpty(fieldName))
            {
                throw new InvalidPluginExecutionException("Quote Product and Field name are required.");
            }

            common.SetMoney(record, fieldName, Discountamount.Get(executionContext));
        }
    }
}
#endif
