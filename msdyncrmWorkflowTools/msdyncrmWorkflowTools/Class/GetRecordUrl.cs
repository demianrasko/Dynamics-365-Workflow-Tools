using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Builds the record URL of any record from its entity name and id, e.g. to pass the record CloneRecord
    /// created to CloneChildren. Ported from demianrasko/Dynamics-365-Workflow-Tools#274 by vinaymenda.
    /// </summary>
    public class GetRecordUrl : WorkflowActivityBase
    {
        /// <summary>
        /// A record URL from the same environment (e.g. the record the workflow runs on); its address is reused.
        /// </summary>
        [RequiredArgument]
        [Input("Reference Record URL")]
        public InArgument<string> ReferenceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Record ID")]
        public InArgument<string> RecordId { get; set; }

        [RequiredArgument]
        [Input("Entity Logical Name")]
        public InArgument<string> EntityName { get; set; }

        [Output("Record URL")]
        public OutArgument<string> RecordUrl { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var referenceRecordUrl = ReferenceRecordUrl.Get(executionContext);
            var recordId = RecordId.Get(executionContext);
            var entityName = EntityName.Get(executionContext);

            if (string.IsNullOrEmpty(referenceRecordUrl) || string.IsNullOrEmpty(entityName))
            {
                throw new InvalidPluginExecutionException("Reference Record URL and Entity Logical Name are required.");
            }

            if (!Guid.TryParse(recordId, out var id))
            {
                throw new InvalidPluginExecutionException($"Record ID '{recordId}' is not a valid GUID.");
            }

            var recordUrl = Utility.BuildRecordUrl(referenceRecordUrl, common.GetEntityTypeCode(entityName), entityName, id);
            common.Trace($"Record URL: {recordUrl}");

            RecordUrl.Set(executionContext, recordUrl);
        }
    }
}
