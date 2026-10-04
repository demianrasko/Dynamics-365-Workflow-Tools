using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class SetLookupFieldFromRecordUrl : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        public InArgument<string> RecordUrl { get; set; }

        [RequiredArgument]
        [Input("Lookup Field Name")]
        public InArgument<string> LookupFieldName { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = RecordUrl.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            var lookupFieldName = LookupFieldName.Get(executionContext);

            if (string.IsNullOrEmpty(lookupFieldName))
            {
                throw new InvalidPluginExecutionException("Lookup Field Name is required.");
            }

            // the record the URL points at becomes the lookup value on the workflow's primary record
            common.SetLookup(
                new EntityReference(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId),
                lookupFieldName,
                common.GetRecordReference(recordUrl));
        }
    }
}
