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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {

            #region "Read Parameters"
            var _FieldName = FieldName.Get(executionContext);
            common.Trace("_FieldName=" + _FieldName);
            var _ParentRecordURL = ParentRecordURL.Get(executionContext);

            if (_ParentRecordURL == null || _ParentRecordURL == string.Empty)
            {
                return;
            }
            common.Trace("_ParentRecordURL=" + _ParentRecordURL);
            var parsedUrl = Utility.ParseRecordUrl(_ParentRecordURL);
            
            var ParentObjectTypeCode=parsedUrl.ObjectTypeCode;
            var ParentId = parsedUrl.Id;
            common.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentId);
            #endregion


            #region "CalculateRollupField Execution"
            var ParentEntityName = common.GetEntityNameFromCode(ParentObjectTypeCode);
            var calculateRollup = new CalculateRollupFieldRequest();
            calculateRollup.FieldName = _FieldName;
            calculateRollup.Target = new EntityReference(ParentEntityName, new Guid(ParentId));
            var resp = (CalculateRollupFieldResponse)common.service.Execute(calculateRollup);
            #endregion
            
        }

        
    }
}
