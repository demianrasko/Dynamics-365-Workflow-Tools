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
            common.Trace($"_FieldName={_FieldName}");
            var _ParentRecordURL = ParentRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(_ParentRecordURL))
            {
                throw new InvalidPluginExecutionException("Parent Record URL is required.");
            }
            common.Trace($"_ParentRecordURL={_ParentRecordURL}");
            var parsedUrl = Utility.ParseRecordUrl(_ParentRecordURL);

            var ParentObjectTypeCode=parsedUrl.ObjectTypeCode;
            var ParentId = parsedUrl.Id;
            common.Trace($"ParentObjectTypeCode={ParentObjectTypeCode}--ParentId={ParentId}");
            #endregion

            #region "CalculateRollupField Execution"
            var ParentEntityName = common.GetEntityNameFromCode(ParentObjectTypeCode);
            var request = new CalculateRollupFieldRequest();
            request.FieldName = _FieldName;
            request.Target = new EntityReference(ParentEntityName, new Guid(ParentId));
            var response = (CalculateRollupFieldResponse)common.Service.Execute(request);
            #endregion
        }
    }
}
