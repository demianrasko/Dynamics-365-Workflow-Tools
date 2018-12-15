using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        protected override void Execute(CodeActivityContext executionContext)
        {
            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            EntityReference sourceEntityReference = GetTargeteEntityReference(executionContext,objCommon.tracingService, objCommon.service);
            string attributeName = GetAttributeName(executionContext,objCommon.tracingService);
            OptionSetValueCollection values = GetAttributeValues(executionContext,objCommon.tracingService);

            UpdateRecord(sourceEntityReference, attributeName, values,objCommon.service,objCommon.tracingService);
        }

        private EntityReference GetTargeteEntityReference(CodeActivityContext executionContext, ITracingService tracingService, IOrganizationService organizationService)
        {
            string sourceRecordUrl = TargetRecordUrl.Get<string>(executionContext) ?? throw new ArgumentNullException("Source URL is empty");
            tracingService.Trace("Source Record URL:'{0}'", sourceRecordUrl);
            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(organizationService);
        }


        private string GetAttributeName(CodeActivityContext executionContext, ITracingService tracingService)
        {
            string attributeName = AttributeName.Get<string>(executionContext) ?? throw new ArgumentNullException("Attribute Name is empty");
            tracingService.Trace("Attribute name:'{0}'", attributeName);
            return attributeName;
        }

        private OptionSetValueCollection GetAttributeValues(CodeActivityContext executionContext, ITracingService tracingService)
        {
            string attributeValues = AttributeValues.Get<string>(executionContext) ?? throw new ArgumentNullException("Attribute Values is empty");
            tracingService.Trace("Attribute Values:'{0}'", attributeValues);

            if (string.IsNullOrEmpty(attributeValues))
            {
                tracingService.Trace("No values found. Setting attribute to null");
                return new OptionSetValueCollection();
            }

            string[] values = attributeValues.Split(',');

            if (values == null || values.Length == 0)
            {
                tracingService.Trace("No values found in array. Setting attribute to null");
                return new OptionSetValueCollection();
            }

            OptionSetValueCollection optionSetValueCollection = new OptionSetValueCollection();

            int intValue;
            foreach (string value in values)
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

        private void UpdateRecord(EntityReference targetEntityReference, string attributeName, OptionSetValueCollection values, IOrganizationService organizationService, ITracingService tracingService)
        {
            if (targetEntityReference == null || attributeName == null || values == null)
                throw new ArgumentNullException(string.Format("Unexpected null parameters when trying to update record. Record reference '{0}' - attibute name '{1}' - values '{2}'", targetEntityReference, attributeName, values));

            Entity targetEntity = new Entity(targetEntityReference.LogicalName, targetEntityReference.Id);
            targetEntity[attributeName] = values;

            organizationService.Update(targetEntity);

            tracingService.Trace("Multi-select option set attribute '{0}' has been updated correctly for the record type '{1}' with id '{2}'", attributeName, targetEntityReference.LogicalName, targetEntityReference.Id);
        }
    }
}
