using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;


namespace msdyncrmWorkflowTools
{

   
    public class CalculateRollupField : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("FieldName")]
        [Default("")]        
        public InArgument<string> FieldName { get; set; }

        [RequiredArgument]
        [Input("Parent Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ParentRecordURL { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _FieldName = FieldName.Get(executionContext);
            objCommon.tracingService.Trace("_FieldName=" + _FieldName);
            var _ParentRecordURL = ParentRecordURL.Get(executionContext);

            if (_ParentRecordURL == null || _ParentRecordURL == string.Empty)
            {
                return;
            }
            objCommon.tracingService.Trace("_ParentRecordURL=" + _ParentRecordURL);
            var parsedUrl = Utility.ParseRecordUrl(_ParentRecordURL);
            
            var ParentObjectTypeCode=parsedUrl.ObjectTypeCode;
            var ParentId = parsedUrl.Id;
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "CalculateRollupField Execution"
            var ParentEntityName = objCommon.GetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            var calculateRollup = new CalculateRollupFieldRequest();
            calculateRollup.FieldName = _FieldName;
            calculateRollup.Target = new EntityReference(ParentEntityName, new Guid(ParentId));
            var resp = (CalculateRollupFieldResponse)objCommon.service.Execute(calculateRollup);
            #endregion
            
        }

        
    }
}
