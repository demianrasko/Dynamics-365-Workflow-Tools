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
        [ReferenceTarget("email")]
        public InArgument<EntityReference> Email { get; set; }

        [Input("Retrieve ActivityMimeAttachment")]
        public InArgument<bool> RetrieveActivityMimeAttachment { get; set; }

        [Input("Select Most Recent Distinct Files")]
        public InArgument<bool> MostRecent { get; set; }

        [Input("Top Attachments (Most Recent)")]
        public InArgument<int> TopRecords { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"

            // Get parameters
            var mainRecordURL = MainRecordURL.Get(executionContext);
            var fileName = FileName.Get(executionContext);
            var email = Email.Get(executionContext);
            var retrieveActivityMimeAttachment = RetrieveActivityMimeAttachment.Get(executionContext);
            var mostRecent = MostRecent.Get(executionContext);
            int? topRecords = TopRecords.Get(executionContext);

            // Extract values from URL
            var parsedUrl = Utility.ParseRecordUrl(mainRecordURL);
            var parentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var parentId = parsedUrl.Id;

            objCommon.Trace("ParentObjectTypeCode=" + parentObjectTypeCode + "--ParentId=" + parentId);

            // Treat file name
            if (fileName == "*") fileName = string.Empty;
            fileName = fileName.Replace("*", "%");

            #endregion

            objCommon.EntityAttachmentToEmail(fileName, parentId, email, retrieveActivityMimeAttachment, mostRecent, topRecords);
        }
    }
}