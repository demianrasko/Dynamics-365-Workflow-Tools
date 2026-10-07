using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Builds the record URL of any record from its entity name and id, e.g. to pass the record CloneRecord
    /// created to CloneChildren. Ported from demianrasko/Dynamics-365-Workflow-Tools#274 by vinaymenda.
    /// </summary>
    [ActivityName("Get Record URL")]
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
            RecordUrl.Set(executionContext, common.GetRecordUrl(ReferenceRecordUrl.Get(executionContext), RecordId.Get(executionContext), EntityName.Get(executionContext)));
        }
    }
}
