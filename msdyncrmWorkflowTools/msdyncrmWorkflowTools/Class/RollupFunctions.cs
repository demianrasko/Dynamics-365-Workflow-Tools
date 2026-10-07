using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Rollup Functions")]
    public class RollupFunctions : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("FetchXML")]
        [Default("")]
        public InArgument<string> FetchXML { get; set; }

        [Output("Count")]
        public OutArgument<decimal> Count { get; set; }

        [Output("Sum")]
        public OutArgument<decimal> Sum { get; set; }

        [Output("Average")]
        public OutArgument<decimal> Average { get; set; }

        [Output("Max")]
        public OutArgument<decimal> Max { get; set; }

        [Output("Min")]
        public OutArgument<decimal> Min { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var result = common.CalculateRollup(FetchXML.Get(executionContext), common.Context.PrimaryEntityId);

            Count.Set(executionContext, result.Count);
            Sum.Set(executionContext, result.Sum);
            Average.Set(executionContext, result.Average);
            Min.Set(executionContext, result.Min);
            Max.Set(executionContext, result.Max);
        }
    }
}
