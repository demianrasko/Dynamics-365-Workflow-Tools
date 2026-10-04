// Not in the Power Platform build: it needs Dynamics 365 tables (quote).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
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
            #region "Read Parameters"
            var quote = Quote.Get(executionContext);
            var discountAmount = Discountamount.Get(executionContext);
            var fieldName = Fieldname.Get(executionContext);

            if (quote == null || string.IsNullOrEmpty(fieldName))
            {
                throw new InvalidPluginExecutionException("Quote and Field name are required.");
            }

            common.Trace($"quote: {quote.Id} DiscountAmount: {discountAmount} Fieldname: {fieldName}");
            #endregion

            common.Service.Update(new Entity(quote.LogicalName, quote.Id)
            {
                [fieldName] = new Money(discountAmount)
            });
        }
    }
}
#endif
