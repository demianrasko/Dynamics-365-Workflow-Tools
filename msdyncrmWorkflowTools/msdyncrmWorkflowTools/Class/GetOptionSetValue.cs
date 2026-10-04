using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GetOptionSetValue : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Output("Value")]
        public OutArgument<int> SelectedValue { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            var sourceEntityReference = GetSourceEntityReference(objCommon, executionContext, objCommon.service);
            var attributeName = GetAttributeName(objCommon, executionContext);

            var value= GetValue(sourceEntityReference, attributeName, objCommon, objCommon.service);

            SelectedValue.Set(executionContext, value);
        }

        private EntityReference GetSourceEntityReference(Common objCommon, CodeActivityContext executionContext, IOrganizationService organizationService)
        {
            var sourceRecordUrl = SourceRecordUrl.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Source URL is empty");
            objCommon.Trace("Source Record URL:'{0}'", sourceRecordUrl);
            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(organizationService);
        }


        private string GetAttributeName(Common objCommon, CodeActivityContext executionContext)
        {
            var attributeName = AttributeName.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Attribute Name is empty");
            objCommon.Trace("Attribute name:'{0}'", attributeName);
            return attributeName;
        }

        

        private int GetValue(EntityReference sourceEntityReference, string attributeName, Common objCommon, IOrganizationService organizationService)
        {
            if (sourceEntityReference == null || attributeName == null)
            {
                objCommon.Trace("Null parameters have been passed, so string will be empty");
                return 0;
            }

            var sourceEntity = organizationService.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(attributeName));
            objCommon.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            if (!sourceEntity.Contains(attributeName))
            {
                objCommon.Trace("Attribues {0} was not found", attributeName);
                return 0;
            }
            var value = 0;
            if (sourceEntity.Attributes.Contains(attributeName))
            {
                value = ((OptionSetValue)sourceEntity.Attributes[attributeName]).Value;
            }
            
            return value;
        }
    }
}