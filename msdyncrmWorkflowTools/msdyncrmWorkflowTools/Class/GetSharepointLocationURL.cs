using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GetSharepointLocationURL : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        public InArgument<string> RecordURL { get; set; }

        [Output("SharepointLocationURL")]
        public OutArgument<string> SharepointLocationURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordId = Utility.ParseRecordUrl(RecordURL.Get(executionContext)).Id;

            var locations = common.GetSharepointLocations(new Guid(recordId));

            SharepointLocationURL.Set(executionContext, common.GetAbsoluteUrlFromLocation(locations));
        }
    }
}
