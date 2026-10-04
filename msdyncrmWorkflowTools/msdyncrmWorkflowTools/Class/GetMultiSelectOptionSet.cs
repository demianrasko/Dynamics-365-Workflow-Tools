using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Text;

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var sourceEntityReference = GetSourceEntityReference(common, executionContext, common.service);
            var attributeName = GetAttributeName(common, executionContext);

            var selectedValues = GetSelectedValues(sourceEntityReference, attributeName, common, common.service);

            SelectedValues.Set(executionContext, selectedValues);
        }

        private EntityReference GetSourceEntityReference(Common common, CodeActivityContext executionContext, IOrganizationService organizationService)
        {
            var sourceRecordUrl = SourceRecordUrl.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Source URL is empty");
            common.Trace("Source Record URL:'{0}'", sourceRecordUrl);
            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(organizationService);
        }

        private string GetAttributeName(Common common, CodeActivityContext executionContext)
        {
            var attributeName = AttributeName.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Attribute Name is empty");
            common.Trace("Attribute name:'{0}'", attributeName);
            return attributeName;
        }

        private string GetSelectedValues(EntityReference sourceEntityReference, string attributeName, Common common, IOrganizationService organizationService)
        {
            if (sourceEntityReference == null || attributeName == null)
            {
                common.Trace("Null parameters have been passed, so string will be empty");
                return string.Empty;
            }

            var sourceEntity = organizationService.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(attributeName));
            common.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            if (!sourceEntity.Contains(attributeName))
            {
                common.Trace("Attribues {0} was not found", attributeName);
                return string.Empty;
            }

            var optionSetValues = sourceEntity[attributeName] as OptionSetValueCollection;
            if (optionSetValues == null)
            {
                return string.Empty;
            }

            var numberOptions = optionSetValues.Count;

            if (numberOptions == 0)
            {
                common.Trace("No selected options");
                return string.Empty;
            }

            common.Trace("Number of selected options: {0}", numberOptions);

            var stringBuilder = new StringBuilder();
            OptionSetValue value = null;
            for (var i = 0; i < numberOptions; i++)
            {
                value = optionSetValues[i];
                stringBuilder.Append(value.Value);
                if ((i + 1) < numberOptions)
                {
                    stringBuilder.Append(",");
                }
            }

            var values = stringBuilder.ToString();
            common.Trace("Values have been retrieved correctly. Values: {0}", values);

            return values;
        }
    }
}