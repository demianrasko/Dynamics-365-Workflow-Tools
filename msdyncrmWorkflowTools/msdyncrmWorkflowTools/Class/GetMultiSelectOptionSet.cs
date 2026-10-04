using System.Activities;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;


namespace msdyncrmWorkflowTools
{
    public class GetMultiSelectOptionSet : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Output("Selected Values")]
        public OutArgument<string> SelectedValues { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            var sourceEntityReference = GetSourceEntityReference(objCommon, executionContext, objCommon.service);
            var attributeName = GetAttributeName(objCommon, executionContext);

            var selectedValues = GetSelectedValues(sourceEntityReference, attributeName, objCommon, objCommon.service);

            SelectedValues.Set(executionContext, selectedValues);
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


        private string GetSelectedValues(EntityReference sourceEntityReference, string attributeName, Common objCommon, IOrganizationService organizationService)
        {
            if (sourceEntityReference == null || attributeName == null)
            {
                objCommon.Trace("Null parameters have been passed, so string will be empty");
                return string.Empty;
            }

            var sourceEntity = organizationService.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(attributeName));
            objCommon.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            if (!sourceEntity.Contains(attributeName))
            {
                objCommon.Trace("Attribues {0} was not found", attributeName);
                return string.Empty;
            }

            var optionSetValues = sourceEntity[attributeName] as OptionSetValueCollection;
            if (optionSetValues == null)
                return string.Empty;

            var numberOptions = optionSetValues.Count;

            if (numberOptions == 0)
            {
                objCommon.Trace("No selected options");
                return string.Empty;
            }

            objCommon.Trace("Number of selected options: {0}", numberOptions);

            var stringBuilder = new StringBuilder();
            OptionSetValue value = null;
            for (var i = 0; i < numberOptions; i++)
            {
                value = optionSetValues[i];
                stringBuilder.Append(value.Value);
                if ((i + 1) < numberOptions)
                    stringBuilder.Append(",");
            }

            var values = stringBuilder.ToString();
            objCommon.Trace("Values have been retrieved correctly. Values: {0}", values);

            return values;
        }
    }
}