using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
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
            #region "Read Parameters"

            // Get parameters
            var mainRecordUrl = MainRecordURL.Get(executionContext);
            var fileName = FileName.Get(executionContext);
            var email = Email.Get(executionContext);

            var retrieveActivityMimeAttachment = RetrieveActivityMimeAttachment.Get(executionContext);

            var mostRecent = MostRecent.Get(executionContext);
            int? topRecords = TopRecords.Get(executionContext);

            // Extract values from URL
            var parsedUrl = Utility.ParseRecordUrl(mainRecordUrl);

            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            // Treat file name
            if (fileName == "*")
            {
                fileName = string.Empty;
            }

            fileName = fileName.Replace("*", "%");
            #endregion

            common.EntityAttachmentToEmail(fileName, parsedUrl.Id, email, retrieveActivityMimeAttachment, mostRecent, topRecords);
        }
    }
}