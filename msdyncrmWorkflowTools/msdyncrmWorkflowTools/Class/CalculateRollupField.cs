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
        public InArgument<string> ParentRecordUrl { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var fieldName = FieldName.Get(executionContext);
            common.Trace($"_FieldName={fieldName}");
            var parentRecordUrl = ParentRecordUrl.Get(executionContext);

            if (string.IsNullOrEmpty(parentRecordUrl))
            {
                throw new InvalidPluginExecutionException("Parent Record URL is required.");
            }

            common.Trace($"_ParentRecordURL={parentRecordUrl}");
            var parsedUrl = common.ParseRecordUrl(parentRecordUrl);

            //var parentObjectTypeCode = parsedUrl.ObjectTypeCode;
            //var parentId = parsedUrl.Id;

            common.Trace($"ParentObjectTypeCode={parsedUrl.EntityName}--ParentId={parsedUrl.Id}");
            #endregion

            #region "CalculateRollupField Execution"
            var parentEntityName = parsedUrl.EntityName;
            var request = new CalculateRollupFieldRequest
            {
                FieldName = fieldName,
                Target = new EntityReference(parentEntityName, new Guid(parsedUrl.Id))
            };

            common.Service.Execute(request);
            #endregion
        }
    }
}
