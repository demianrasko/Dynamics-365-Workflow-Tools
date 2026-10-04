using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class SetMultiSelectOptionSet : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Target Record URL")]
        public InArgument<string> TargetRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [RequiredArgument]
        [Input("Attribute Values")]
        public InArgument<string> AttributeValues { get; set; }
        /// <summary>
        /// Indicate if the existing selected values in the target multi-select optionset attribute will be maintained.
        /// By default, it will remove the existing values and assign the new ones given by the argument "AttributesValues"
        /// </summary>
        [Input("Keep Existing Values")]
        [Default("false")]
        public InArgument<bool> KeepExistingValues { get; set; } 

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            var sourceEntityReference = GetTargetEntityReference(executionContext,objCommon, objCommon.service);
            var attributeName = GetAttributeName(executionContext,objCommon);
            var newValues = GetNewAttributeValues(executionContext, objCommon);
            var existingValues = GetExistingAttributeValues(sourceEntityReference, attributeName,executionContext, objCommon, objCommon.service);


            //UpdateRecord(sourceEntityReference, attributeName, values,objCommon.service,objCommon);
            UpdateRecord(sourceEntityReference, attributeName, newValues, existingValues, objCommon.service, objCommon);

        }

        private EntityReference GetTargetEntityReference(CodeActivityContext executionContext, Common objCommon, IOrganizationService organizationService)
        {
            var sourceRecordUrl = TargetRecordUrl.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Source URL is empty");
            objCommon.Trace("Source Record URL:'{0}'", sourceRecordUrl);

            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(organizationService);
        }

        private string GetAttributeName(CodeActivityContext executionContext, Common objCommon)
        {
            var attributeName = AttributeName.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Attribute Name is empty");
            objCommon.Trace("Attribute name:'{0}'", attributeName);
            return attributeName;
        }

        private OptionSetValueCollection GetNewAttributeValues(CodeActivityContext executionContext, Common objCommon)
        {
            var attributeValues = AttributeValues.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Attribute Values is empty");

            objCommon.Trace($"Attribute Values:'{attributeValues}'");

            if (string.IsNullOrEmpty(attributeValues))
            {
                objCommon.Trace("No values found. Setting attribute to null");
                return new OptionSetValueCollection();
            }

            var values = attributeValues.Split(',');

            if (values.Length == 0)
            {
                objCommon.Trace("No values found in array. Setting attribute to null");
                return new OptionSetValueCollection();
            }

            var optionSetValueCollection = new OptionSetValueCollection();

            foreach (var value in values)
            {
                if (int.TryParse(value, out var intValue))
                {
                    objCommon.Trace("Value '{0}' added correctly", value);
                    optionSetValueCollection.Add(new OptionSetValue(intValue));
                }
                else
                {
                    objCommon.Trace("Value '{0}' couldn't be parsed", value);
                }
            }

            return optionSetValueCollection;
        }
        private OptionSetValueCollection GetExistingAttributeValues(EntityReference targetEntityReference, string attributeName, CodeActivityContext executionContext, Common objCommon, IOrganizationService organizationService)
        {
            objCommon.Trace("Retrieving existing values");

            var attributeValues = KeepExistingValues.Get<bool>(executionContext);

            if (!attributeValues)
            {
                return null;
            }

            var record = organizationService.Retrieve(targetEntityReference.LogicalName, targetEntityReference.Id, new ColumnSet(attributeName));

            objCommon.Trace("Existing values have been retrieved correctly");

            if (record.Contains(attributeName))
            {
                return record[attributeName] as OptionSetValueCollection;
            }

            return null;
        }

        private void UpdateRecord(EntityReference targetEntityReference, string attributeName, OptionSetValueCollection newValues, OptionSetValueCollection existingValues, IOrganizationService organizationService, Common objCommon)
        {
            if (targetEntityReference == null || attributeName == null || newValues == null)
            {
                throw new InvalidPluginExecutionException(
                    $"Unexpected null parameters when trying to update record. Record reference '{targetEntityReference}' - attibute name '{attributeName}' - values '{newValues}'");
            }
            
            var targetEntity = new Entity(targetEntityReference.LogicalName, targetEntityReference.Id)
                {
                    [attributeName] = MergeOptionSetCollections(newValues, existingValues, objCommon)
                };

            organizationService.Update(targetEntity);

            objCommon.Trace("Multi-select option set attribute '{0}' has been updated correctly for the record type '{1}' with id '{2}'", attributeName, targetEntityReference.LogicalName, targetEntityReference.Id);
        }

        private static OptionSetValueCollection MergeOptionSetCollections(OptionSetValueCollection newValues, OptionSetValueCollection existingValues, Common objCommon)
        {
            objCommon.Trace("Merging new and exiting multi-select optionset values");

            switch (existingValues)
            {
                case null when newValues == null:
                    return new OptionSetValueCollection();
                case null:
                    return newValues;
            }

            if (newValues == null)
            {
                return existingValues;
            }

            foreach (var newValue in newValues)
            {
                if (!existingValues.Contains(newValue))
                {
                    existingValues.Add(newValue);
                }
            }

            objCommon.Trace("New and exiting multi-select optionset values have been merged correctly. Total options: {0} ", existingValues.Count);
            
            return existingValues;
        }
    }
}
