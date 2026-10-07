using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Linq;

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
            var fetchXml = FetchXML.Get(executionContext);

            if (string.IsNullOrEmpty(fetchXml))
            {
                throw new InvalidPluginExecutionException("FetchXML is required.");
            }

            fetchXml = fetchXml.Replace("{PARENT_GUID}", common.Context.PrimaryEntityId.ToString());
            common.Trace($"FetchXML={fetchXml}");

            var records = common.RetrieveAllWithFetchXml(fetchXml).ToList();

            // the calculations use the first attribute in the fetch
            var key = Utility.GetFirstFetchAttributeKey(fetchXml);
            var values = records
                .Select(r => Utility.ToDecimal(Utility.GetFirstFetchValue(r, key)))
                .ToList();

            var result = Utility.CalculateRollup(values);
            common.Trace($"Records={result.Count}, Sum={result.Sum}, Average={result.Average}, Min={result.Min}, Max={result.Max}");

            Count.Set(executionContext, result.Count);
            Sum.Set(executionContext, result.Sum);
            Average.Set(executionContext, result.Average);
            Min.Set(executionContext, result.Min);
            Max.Set(executionContext, result.Max);
        }
    }
}
