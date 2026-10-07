using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Globalization;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// One operation on two numbers, with a single result (Numeric Functions gives every result at once).
    /// </summary>
    [ActivityName("Numeric Operation")]
    public class NumericOperation : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Number 1")]
        public InArgument<decimal> Number1 { get; set; }

        [RequiredArgument]
        [Input("Operation (+ - * / % min max)")]
        public InArgument<string> Operation { get; set; }

        [RequiredArgument]
        [Input("Number 2")]
        public InArgument<decimal> Number2 { get; set; }

        [Output("Result")]
        public OutArgument<decimal> Result { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var number1 = Number1.Get(executionContext);
            var operation = Operation.Get(executionContext);
            var number2 = Number2.Get(executionContext);

            var result = Utility.NumericOperation(number1, operation, number2);
            common.Trace($"{number1.ToString(CultureInfo.InvariantCulture)} {operation} {number2.ToString(CultureInfo.InvariantCulture)} = {result.ToString(CultureInfo.InvariantCulture)}");

            Result.Set(executionContext, result);
        }
    }
}
