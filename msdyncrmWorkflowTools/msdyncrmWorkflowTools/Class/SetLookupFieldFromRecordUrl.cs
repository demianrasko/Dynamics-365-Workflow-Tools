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
            #region "Read Parameters"
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

            common.Trace($"Inputs -- RecordUrl: {recordUrl} | LookupFieldName: {lookupFieldName}");
            #endregion

            // the record the URL points at becomes the lookup value on the workflow's primary record
            var entityReference = new DynamicUrlParser(recordUrl).ToEntityReference(common.service);

            var recordToUpdate = new Entity(common.context.PrimaryEntityName, common.context.PrimaryEntityId)
            {
                [lookupFieldName] = entityReference
            };

            common.Trace($"PrimaryEntityName: {common.context.PrimaryEntityName} | PrimaryEntityId: {common.context.PrimaryEntityId}");
            common.service.Update(recordToUpdate);
        }
    }
}
