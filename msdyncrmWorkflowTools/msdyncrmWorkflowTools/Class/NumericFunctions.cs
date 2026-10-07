using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Numeric Functions")]
    public class NumericFunctions : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Number 1")]
        public InArgument<decimal> Number1 { get; set; }

        [RequiredArgument]
        [Input("Number 2")]
        public InArgument<decimal> Number2 { get; set; }

        [Output("Add")]
        public OutArgument<decimal> Add { get; set; }

        [Output("Subtract")]
        public OutArgument<decimal> Subtract { get; set; }

        [Output("Multiply")]
        public OutArgument<decimal> Multiply { get; set; }

        [Output("Divide")]
        public OutArgument<decimal> Divide { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var number1 = Number1.Get(executionContext);
            var number2 = Number2.Get(executionContext);

            Add.Set(executionContext, number1 + number2);
            Subtract.Set(executionContext, number1 - number2);
            Multiply.Set(executionContext, number1 * number2);
            Divide.Set(executionContext, Utility.DivideOrZero(number1, number2));
        }
    }
}
