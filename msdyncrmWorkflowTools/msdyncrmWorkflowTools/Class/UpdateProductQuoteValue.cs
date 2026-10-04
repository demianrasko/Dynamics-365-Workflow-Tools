using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class UpdateProductQuoteValue : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Quote Product")]
        [ReferenceTarget("quotedetail")]
        public InArgument<EntityReference> Quote { get; set; }

        [RequiredArgument]
        [Input("Discount Amount")]
        public InArgument<decimal> Discountamount { get; set; }

        //"manualdiscountamount"
        [RequiredArgument]
        [Input("Field name to update (manualdiscountamount)")]
        public InArgument<string> Fieldname { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var quoteProduct = Quote.Get(executionContext);
            var discountAmount = Discountamount.Get(executionContext);
            var fieldName = Fieldname.Get(executionContext);

            if (quoteProduct == null || string.IsNullOrEmpty(fieldName))
            {
                throw new InvalidPluginExecutionException("Quote Product and Field name are required.");
            }

            common.Trace($"quotedetail: {quoteProduct.Id} Discountamount: {discountAmount} Fieldname: {fieldName}");
            #endregion

            common.service.Update(new Entity(quoteProduct.LogicalName, quoteProduct.Id)
            {
                [fieldName] = new Money(discountAmount)
            });
        }
    }
}
