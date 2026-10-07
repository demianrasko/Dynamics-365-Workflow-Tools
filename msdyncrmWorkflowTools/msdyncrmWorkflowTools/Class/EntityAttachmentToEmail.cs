using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Entity Attachment To Email")]
    public class EntityAttachmentToEmail : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Main Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> MainRecordURL { get; set; }

        [RequiredArgument]
        [Input("File Name (use * for filter)")]
        [ReferenceTarget("")]
        public InArgument<string> FileName { get; set; }

        [RequiredArgument]
        [Input("Email")]
        [ReferenceTarget(EntityNames.Email)]
        public InArgument<EntityReference> Email { get; set; }

        [Input("Retrieve ActivityMimeAttachment")]
        public InArgument<bool> RetrieveActivityMimeAttachment { get; set; }

        [Input("Select Most Recent Distinct Files")]
        public InArgument<bool> MostRecent { get; set; }

        [Input("Top Attachments (Most Recent)")]
        public InArgument<int> TopRecords { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.EntityAttachmentToEmail(MainRecordURL.Get(executionContext), FileName.Get(executionContext), Email.Get(executionContext),
                RetrieveActivityMimeAttachment.Get(executionContext), MostRecent.Get(executionContext), TopRecords.Get(executionContext));
        }
    }
}