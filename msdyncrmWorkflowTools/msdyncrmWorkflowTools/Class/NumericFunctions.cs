using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Globalization;

namespace msdyncrmWorkflowTools.Class
{
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
            #region "Read Parameters"
            var number1= Number1.Get(executionContext);
            var number2 = Number2.Get(executionContext);

            common.Trace($"number 1 / number 2: {number1.ToString(CultureInfo.InvariantCulture)} / {number2.ToString(CultureInfo.InvariantCulture)}");

            #endregion

            Add.Set(executionContext, number1+number2);
            Subtract.Set(executionContext, number1 - number2);
            Multiply.Set(executionContext, number1 * number2);

            if (number2 != 0)
            {
                Divide.Set(executionContext, number1 / number2);
            }
            else
            {
                Divide.Set(executionContext, 0);
            }
        }
    }
}
