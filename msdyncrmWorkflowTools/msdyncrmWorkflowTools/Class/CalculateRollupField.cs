using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;


namespace msdyncrmWorkflowTools
{

   
    public class CalculateRollupField : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var _FieldName = FieldName.Get(executionContext);
            objCommon.Trace("_FieldName=" + _FieldName);
            var _ParentRecordURL = ParentRecordURL.Get(executionContext);

            if (_ParentRecordURL == null || _ParentRecordURL == string.Empty)
            {
                return;
            }
            objCommon.Trace("_ParentRecordURL=" + _ParentRecordURL);
            var parsedUrl = Utility.ParseRecordUrl(_ParentRecordURL);
            
            var ParentObjectTypeCode=parsedUrl.ObjectTypeCode;
            var ParentId = parsedUrl.Id;
            objCommon.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "CalculateRollupField Execution"
            var ParentEntityName = objCommon.GetEntityNameFromCode(ParentObjectTypeCode);
            var calculateRollup = new CalculateRollupFieldRequest();
            calculateRollup.FieldName = _FieldName;
            calculateRollup.Target = new EntityReference(ParentEntityName, new Guid(ParentId));
            var resp = (CalculateRollupFieldResponse)objCommon.service.Execute(calculateRollup);
            #endregion
            
        }

        
    }
}
