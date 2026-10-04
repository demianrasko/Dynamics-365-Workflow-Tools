using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;


namespace msdyncrmWorkflowTools.Class
{
    public class EntityAttachmentToEmail : CodeActivity
    {
        [RequiredArgument]
        [Input("Main Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> MainRecordURL { get; set; }

        [RequiredArgument]
        [Input("File Name (use * for filter)")]
        [ReferenceTarget("")]
        public InArgument<String> FileName { get; set; }

        [RequiredArgument]
        [Input("Email")]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> Email { get; set; }

        [Input("Retrieve ActivityMimeAttachment")]
        public InArgument<Boolean> RetrieveActivityMimeAttachment { get; set; }

        [Input("Select Most Recent Distinct Files")]
        public InArgument<Boolean> MostRecent { get; set; }

        [Input("Top Attachments (Most Recent)")]
        public InArgument<int> TopRecords { get; set; }



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            #endregion

            #region "Read Parameters"

            // Get parameters
            var mainRecordURL = MainRecordURL.Get(executionContext);
            var fileName = FileName.Get(executionContext);
            var email = Email.Get(executionContext);
            var retrieveActivityMimeAttachment = RetrieveActivityMimeAttachment.Get(executionContext);
            var mostRecent = MostRecent.Get(executionContext);
            int? topRecords = TopRecords.Get(executionContext);


            // Extract values from URL
            var urlParts = mainRecordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            var ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);

            // Treat file name
            if (fileName == "*") fileName = "";
            fileName = fileName.Replace("*", "%");

            #endregion

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            commonClass.EntityAttachmentToEmail(fileName, ParentId, email, retrieveActivityMimeAttachment, mostRecent, topRecords);
        }
    }
}