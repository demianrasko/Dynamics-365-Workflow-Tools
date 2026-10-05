using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Count Child Entity Records")]
    public class CountChildEntityRecords : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Child Entity Schema Name")]
        [Default("")]
        public InArgument<string> ChildEntityName { get; set; }

        [RequiredArgument]
        [Input("Parent Lookup Field Name on Child")]
        [Default("")]
        public InArgument<string> ParentLookupName { get; set; }

        [RequiredArgument]
        [Input("Record URL (Parent)")]
        [ReferenceTarget("")]
        public InArgument<string> RecordUrl { get; set; }

        [Input("FetchXML Filter (Child)")]
        [ReferenceTarget("")]
        public InArgument<string> FilterExpressionXml { get; set; }

        [Output("Result")]
        public OutArgument<int> Result { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var childEntityName = ChildEntityName.Get(executionContext);
            var parentLookupName = ParentLookupName.Get(executionContext);
            var recordUrl = RecordUrl.Get(executionContext);
            common.Trace($"ChildEntityName={childEntityName}--ParentLookupName={parentLookupName}--RecordURL={recordUrl}");

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL (Parent) is required.");
            }
            var parsedUrl = common.ParseRecordUrl(recordUrl);

            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            var count = common.CountChildRecords(childEntityName, parentLookupName, parsedUrl.Id, FilterExpressionXml.Get(executionContext));
            common.Trace($"{childEntityName} records with {parentLookupName} = {parsedUrl.Id}: {count}");

            Result.Set(executionContext, count);
        }
    }
}
