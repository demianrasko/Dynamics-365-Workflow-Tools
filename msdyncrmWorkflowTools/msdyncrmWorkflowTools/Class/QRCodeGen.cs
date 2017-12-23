using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using msdyncrmWorkflowTools;
using System.ServiceModel;

namespace msdyncrmWorkflowTools
{

   
    public class QRCodeGen : CodeActivity
    {
            #region "Parameter Definition"
            [RequiredArgument]
            [Input("Record URL")]
            public InArgument<String> RecordURL { get; set; }

            [RequiredArgument]
            [Input("QR Info")]
            public InArgument<String> QRInfo { get; set; }

            [Input("Note Subject")]
            public InArgument<String> noteSubject { get; set; }

            [Input("Note Text")]
            public InArgument<String> noteText { get; set; }

            [Input("File Name")]
            public InArgument<String> fileName { get; set; }

          /*  [Input("Image Format")]
            public InArgument<String> imageFormat { get; set; }*/

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _recordURL = this.RecordURL.Get(executionContext);
            if (_recordURL == null || _recordURL == "")
            {
                return;
            }
            string[] urlParts = _recordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            string entityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            string ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);

            string _QRInfo = this.QRInfo.Get(executionContext);
            objCommon.tracingService.Trace("QR Ok");
            string _noteSubject = this.noteSubject.Get(executionContext);
            if (_noteSubject == null || _noteSubject == "")
            {
                _noteSubject = "QR";
            }
            objCommon.tracingService.Trace("noteSubject Ok");

            string _noteText = this.noteText.Get(executionContext);
            objCommon.tracingService.Trace("noteText Ok");

            string _fileName = this.fileName.Get(executionContext);
            objCommon.tracingService.Trace("FileName Ok");

            string _imageFormat = "jpg";//this.imageFormat.Get(executionContext);
            objCommon.tracingService.Trace("ImageFormat Ok");

            if (_imageFormat == null || _imageFormat == "")
            {
                _imageFormat = "jpg";
            }
            if (_fileName == null || _fileName == "")
            {
                _fileName = "QR." + _imageFormat;
            }



            #endregion


            #region "QR Execution"

            try
            {
                objCommon.tracingService.Trace("Start QR Creation");
                msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
                commonClass.QRCode(entityName, ParentId, _QRInfo, _noteSubject, _noteText, _fileName);


            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                throw ex;
                // if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                //{
                // throw ex;
                //}
            }
            catch (System.Exception ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                throw ex;
            }
            #endregion

        }


        }
    }
