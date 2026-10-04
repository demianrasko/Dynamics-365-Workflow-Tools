using System;
using System.Activities;
using System.Linq;
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
        public InArgument<String> FieldName { get; set; }

        [RequiredArgument]
        [Input("Parent Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> ParentRecordURL { get; set; }
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

            if (_ParentRecordURL == null || _ParentRecordURL == "")
            {
                return;
            }
            objCommon.tracingService.Trace("_ParentRecordURL=" + _ParentRecordURL);
            var urlParts = _ParentRecordURL.Split("?".ToArray());
            var urlParams=urlParts[1].Split("&".ToCharArray());
            
            var ParentObjectTypeCode=urlParams[0].Replace("etc=","");
            var ParentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "CalculateRollupField Execution"
            var ParentEntityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            var calculateRollup = new CalculateRollupFieldRequest();
            calculateRollup.FieldName = _FieldName;
            calculateRollup.Target = new EntityReference(ParentEntityName, new Guid(ParentId));
            var resp = (CalculateRollupFieldResponse)objCommon.service.Execute(calculateRollup);
            #endregion
            
        }

        
    }
}
