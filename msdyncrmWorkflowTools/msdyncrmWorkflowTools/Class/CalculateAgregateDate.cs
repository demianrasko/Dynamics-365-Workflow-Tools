using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Calculate Aggregate Date")]
    public class CalculateAgregateDate : WorkflowActivityBase
    {
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

            fetchXml = fetchXml.Replace("{PARENT_GUID}", common.Context.PrimaryEntityId.ToString());
            common.Trace($"FetchXML={fetchXml}");

            // the date is the first attribute in the fetch, read from the first record
            var record = common.RetrieveFirstWithFetchXml(fetchXml);
            var value = record == null ? null : Utility.GetFirstFetchValue(record, Utility.GetFirstFetchAttributeKey(fetchXml));

            if (value is DateTime date)
            {
                common.Trace($"Date={date}");
                Value.Set(executionContext, date);
                Ok.Set(executionContext, true);
                return;
            }

            common.Trace(record == null ? "No record found." : $"The first attribute is not a date: '{value}'");
            Value.Set(executionContext, new DateTime(1753, 1, 1));
            Ok.Set(executionContext, false);
        }
    }
}