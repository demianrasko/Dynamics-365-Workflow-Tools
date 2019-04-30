using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Linq;


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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            #endregion

            #region "Read Parameters"

            // Get parameters
            string mainRecordURL = MainRecordURL.Get(executionContext);
            string fileName = FileName.Get(executionContext);
            EntityReference email = Email.Get(executionContext);
            bool retrieveActivityMimeAttachment = RetrieveActivityMimeAttachment.Get(executionContext);
            bool mostRecent = MostRecent.Get(executionContext);
            int? topRecords = TopRecords.Get(executionContext);


            // Extract values from URL
            string[] urlParts = mainRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            string ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);

            // Treat file name
            if (fileName == "*") fileName = "";
            fileName = fileName.Replace("*", "%");

            #endregion

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            commonClass.EntityAttachmentToEmail(fileName, ParentId, email, retrieveActivityMimeAttachment, mostRecent, topRecords);
        }
    }
}