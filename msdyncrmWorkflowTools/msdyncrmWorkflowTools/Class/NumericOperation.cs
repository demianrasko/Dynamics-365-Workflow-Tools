using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

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
            Result.Set(executionContext, Utility.NumericOperation(Number1.Get(executionContext), Operation.Get(executionContext), Number2.Get(executionContext)));
        }
    }
}
