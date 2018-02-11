using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Client;



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



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"

            String _MainRecordURL = this.MainRecordURL.Get(executionContext);
            if (_MainRecordURL == null || _MainRecordURL == "")
            {
                return;
            }
            string[] urlParts = _MainRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            string ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);


            String _FileName = this.FileName.Get(executionContext);
            if (_FileName == null || _FileName == "")
            {
                return;
            }
            if (_FileName == "*")
                _FileName = "";
            _FileName = _FileName.Replace("*", "%");

            EntityReference email = this.Email.Get(executionContext);
            bool _RetrieveActivityMimeAttachment = this.RetrieveActivityMimeAttachment.Get(executionContext);

            #endregion

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            commonClass.EntityAttachmentToEmail(_FileName, ParentId, email, _RetrieveActivityMimeAttachment);
        }

    }
}

