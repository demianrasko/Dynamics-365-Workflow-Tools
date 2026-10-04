using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class SetMultiSelectOptionSet : CodeActivity
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
        public InArgument<Boolean> KeepExistingValues { get; set; } 

        protected override void Execute(CodeActivityContext executionContext)
        {
            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            var sourceEntityReference = GetTargeteEntityReference(executionContext,objCommon.tracingService, objCommon.service);
            var attributeName = GetAttributeName(executionContext,objCommon.tracingService);
            var newValues = GetNewAttributeValues(executionContext, objCommon.tracingService);
            var existingValues = GetExistingAttributeValues(sourceEntityReference, attributeName,executionContext, objCommon.tracingService, objCommon.service);


            //UpdateRecord(sourceEntityReference, attributeName, values,objCommon.service,objCommon.tracingService);
            UpdateRecord(sourceEntityReference, attributeName, newValues, existingValues, objCommon.service, objCommon.tracingService);

        }

        private EntityReference GetTargeteEntityReference(CodeActivityContext executionContext, ITracingService tracingService, IOrganizationService organizationService)
        {
            var sourceRecordUrl = TargetRecordUrl.Get<string>(executionContext) ?? throw new ArgumentNullException("Source URL is empty");
            tracingService.Trace("Source Record URL:'{0}'", sourceRecordUrl);
            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(organizationService);
        }


        private string GetAttributeName(CodeActivityContext executionContext, ITracingService tracingService)
        {
            var attributeName = AttributeName.Get<string>(executionContext) ?? throw new ArgumentNullException("Attribute Name is empty");
            tracingService.Trace("Attribute name:'{0}'", attributeName);
            return attributeName;
        }

        //private OptionSetValueCollection GetAttributeValues(CodeActivityContext executionContext, ITracingService tracingService)
        private OptionSetValueCollection GetNewAttributeValues(CodeActivityContext executionContext, ITracingService tracingService)

        {
            var attributeValues = AttributeValues.Get<string>(executionContext) ?? throw new ArgumentNullException("Attribute Values is empty");
            tracingService.Trace("Attribute Values:'{0}'", attributeValues);

            if (string.IsNullOrEmpty(attributeValues))
            {
                tracingService.Trace("No values found. Setting attribute to null");
                return new OptionSetValueCollection();
            }

            var values = attributeValues.Split(',');

            if (values == null || values.Length == 0)
            {
                tracingService.Trace("No values found in array. Setting attribute to null");
                return new OptionSetValueCollection();
            }

            var optionSetValueCollection = new OptionSetValueCollection();

            int intValue;
            foreach (var value in values)
            {
                if (int.TryParse(value, out intValue))
                {
                    tracingService.Trace("Value '{0}' added correctly", value);
                    optionSetValueCollection.Add(new OptionSetValue(intValue));
                }
                else
                {
                    tracingService.Trace("Value '{0}' couldn't be parsed", value);
                }
            }

            return optionSetValueCollection;
        }
        private OptionSetValueCollection GetExistingAttributeValues(EntityReference targetEntityReference, string attributeName, CodeActivityContext executionContext, ITracingService tracingService, IOrganizationService organizationService)
        {
            tracingService.Trace("Retrieving existing values");

            var attributeValues = KeepExistingValues.Get<Boolean>(executionContext);

            if (attributeValues == false)
                return null;

            var record = organizationService.Retrieve(targetEntityReference.LogicalName, targetEntityReference.Id, new ColumnSet(new string[] { attributeName }));

            tracingService.Trace("Existing values have been retrieved correctly");

            if (record.Contains(attributeName))
                return record[attributeName] as OptionSetValueCollection;
            else
                return null;
        }


        //private void UpdateRecord(EntityReference targetEntityReference, string attributeName, OptionSetValueCollection values, IOrganizationService organizationService, ITracingService tracingService)
        private void UpdateRecord(EntityReference targetEntityReference, string attributeName, OptionSetValueCollection newValues, OptionSetValueCollection existingValues, IOrganizationService organizationService, ITracingService tracingService)

        {
            if (targetEntityReference == null || attributeName == null || newValues == null)
                throw new ArgumentNullException(string.Format("Unexpected null parameters when trying to update record. Record reference '{0}' - attibute name '{1}' - values '{2}'", targetEntityReference, attributeName, newValues));


            var targetEntity = new Entity(targetEntityReference.LogicalName, targetEntityReference.Id);
            targetEntity[attributeName] = MergeOptionSetCollections(newValues, existingValues, tracingService);

            organizationService.Update(targetEntity);

            tracingService.Trace("Multi-select option set attribute '{0}' has been updated correctly for the record type '{1}' with id '{2}'", attributeName, targetEntityReference.LogicalName, targetEntityReference.Id);
        }
        private OptionSetValueCollection MergeOptionSetCollections(OptionSetValueCollection newValues, OptionSetValueCollection existingValues, ITracingService tracingService)
        {
            tracingService.Trace("Merging new and exiting multi-select optionset values");

            if (existingValues == null && newValues == null)
                return new OptionSetValueCollection();

            if (existingValues == null)
                return newValues;

            if (newValues == null)
                return existingValues;

            foreach (var newValue in newValues)
            {
                if (!existingValues.Contains(newValue))
                    existingValues.Add(newValue);
            }

            tracingService.Trace("New and exiting multi-select optionset values have been merged correctly. Total options: {0} ", existingValues.Count);
            return existingValues;
        }

    }
}
