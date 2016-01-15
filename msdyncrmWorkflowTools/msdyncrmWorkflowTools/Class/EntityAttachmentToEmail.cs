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

            #endregion

            #region "Query Attachments"
            string fetchXML = @"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='annotation'>
                        <attribute name='filename' />
                        <attribute name='annotationid' />
                        <attribute name='subject' />
                        <attribute name='documentbody' />
                        <attribute name='mimetype' />

                        <filter type='and'>
                          <condition attribute='filename' operator='like' value='%"+ _FileName + @"%' />
                          <condition attribute='isdocument' operator='eq' value='1' />
                          <condition attribute='objectid' operator='eq' value='"+ ParentId + @"' />
                        </filter>
                      </entity>
                    </fetch>";
            objCommon.tracingService.Trace(String.Format("FetchXML: {0} ", fetchXML));
            EntityCollection attachmentFiles = objCommon.service.RetrieveMultiple(new FetchExpression(fetchXML));

            if (attachmentFiles.Entities.Count == 0)
            {
                objCommon.tracingService.Trace(String.Format("No Attachment Files found."));
                return;
            }


            #endregion

            #region "Add Attachments to Email"
            int i= 1;
            foreach (Entity file in attachmentFiles.Entities)
            {
                Entity _Attachment = new Entity("activitymimeattachment");
                _Attachment["objectid"]= new EntityReference("email",email.Id);
                _Attachment["objecttypecode"]= "email";
                _Attachment["attachmentnumber"] = i;
                i++;

                if (file.Attributes.Contains("subject"))
                {
                    _Attachment["subject"]= file.Attributes["subject"].ToString();
                }
                if (file.Attributes.Contains("filename"))
                {
                    _Attachment["filename"]= file.Attributes["filename"].ToString();
                }
                if (file.Attributes.Contains("documentbody"))
                {
                    _Attachment["body"]= file.Attributes["documentbody"].ToString();
                }
                if (file.Attributes.Contains("mimetype"))
                {
                    _Attachment["mimetype"]= file.Attributes["mimetype"].ToString();
                }

                objCommon.service.Create(_Attachment);

               
            }

            #endregion

        }
    }
}
