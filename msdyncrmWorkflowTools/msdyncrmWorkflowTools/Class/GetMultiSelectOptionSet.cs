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
    public class GetMultiSelectOptionSet : CodeActivity
    {
        [RequiredArgument]
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Output("Selected Values")]
        public OutArgument<string> SelectedValues { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            EntityReference sourceEntityReference = GetSourceEntityReference(objCommon.tracingService, executionContext, objCommon.service);
            string attributeName = GetAttributeName(objCommon.tracingService, executionContext);

            string selectedValues = GetSelectedValues(sourceEntityReference, attributeName, objCommon.tracingService, objCommon.service);

            this.SelectedValues.Set(executionContext, selectedValues);
        }

        private EntityReference GetSourceEntityReference(ITracingService tracingService, CodeActivityContext executionContext, IOrganizationService organizationService)
        {
            string sourceRecordUrl = SourceRecordUrl.Get<string>(executionContext) ?? throw new ArgumentNullException("Source URL is empty");
            tracingService.Trace("Source Record URL:'{0}'", sourceRecordUrl);
            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(organizationService);
        }


        private string GetAttributeName(ITracingService tracingService, CodeActivityContext executionContext)
        {
            string attributeName = AttributeName.Get<string>(executionContext) ?? throw new ArgumentNullException("Attribute Name is empty");
            tracingService.Trace("Attribute name:'{0}'", attributeName);
            return attributeName;
        }


        private string GetSelectedValues(EntityReference sourceEntityReference, string attributeName, ITracingService tracingService, IOrganizationService organizationService)
        {
            if (sourceEntityReference == null || attributeName == null)
            {
                tracingService.Trace("Null parameters have been passed, so string will be empty");
                return string.Empty;
            }

            Entity sourceEntity = organizationService.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(attributeName));
            tracingService.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            if (!sourceEntity.Contains(attributeName))
            {
                tracingService.Trace("Attribues {0} was not found", attributeName);
                return string.Empty;
            }

            OptionSetValueCollection optionSetValues = sourceEntity[attributeName] as OptionSetValueCollection;
            if (optionSetValues == null)
                return string.Empty;

            int numberOptions = optionSetValues.Count;

            if (numberOptions == 0)
            {
                tracingService.Trace("No selected options");
                return string.Empty;
            }

            tracingService.Trace("Number of selected options: ", numberOptions);

            StringBuilder stringBuilder = new StringBuilder();
            OptionSetValue value = null;
            for (int i = 0; i < numberOptions; i++)
            {
                value = optionSetValues[i];
                stringBuilder.Append(value.Value);
                if ((i + 1) < numberOptions)
                    stringBuilder.Append(",");
            }

            string values = stringBuilder.ToString();
            tracingService.Trace("Values have been retrieved correctly. Values: ", values);

            return values;
        }
    }
}