using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// The date a FetchXML query returns, usually an aggregate such as max(createdon). When the query finds no date,
    /// Value is 1-1-1753 (the earliest date Dataverse stores; a workflow date output can't be empty) and Ok is No, so
    /// check Ok before using Value.
    /// </summary>
    [ActivityName("Calculate Aggregate Date")]
    public class CalculateAgregateDate : WorkflowActivityBase
    {
        private static readonly DateTime NoDate = new DateTime(1753, 1, 1);

        [RequiredArgument]
        [Input("FetchXML")]
        [Default("")]
        public InArgument<string> FetchXML { get; set; }

        [Output("Value")]
        public OutArgument<DateTime> Value { get; set; }

        [Output("Ok")]
        public OutArgument<bool> Ok { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var fetchXml = FetchXML.Get(executionContext);

            if (string.IsNullOrEmpty(fetchXml))
            {
                throw new InvalidPluginExecutionException("FetchXML is required.");
            }

            var date = common.CalculateAggregateDate(fetchXml, common.Context.PrimaryEntityId);

            Value.Set(executionContext, date ?? NoDate);
            Ok.Set(executionContext, date.HasValue);
        }
    }
}