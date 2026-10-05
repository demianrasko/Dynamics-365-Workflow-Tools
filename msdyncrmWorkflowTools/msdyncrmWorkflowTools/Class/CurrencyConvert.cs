using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CurrencyConvert : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Amount")]
        [Default("0")]
        public InArgument<decimal> Amount { get; set; }

        [RequiredArgument]
        [Input("From Currency")]
        [Default("")]
        public InArgument<string> FromCurrency { get; set; }

        [RequiredArgument]
        [Input("To Currency")]
        [Default("")]
        public InArgument<string> ToCurrency { get; set; }

        [Output("Result")]
        public OutArgument<decimal> Result { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var amount = Amount.Get(executionContext);
            var fromCurrency = FromCurrency.Get(executionContext);
            var toCurrency = ToCurrency.Get(executionContext);

            var result = Utility.CurrencyConvert(amount, fromCurrency, toCurrency, common.TracingService);

            Result.Set(executionContext, result);
        }
    }
}
