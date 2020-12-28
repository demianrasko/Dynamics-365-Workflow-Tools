using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
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

        [Input("Retrieve Options Names")]
        [Default("False")]
        public InArgument<bool> RetrieveOptionsNames { get; set; }

        [Output("Selected Values")]
        public OutArgument<string> SelectedValues { get; set; }

        [Output("Selected Names")]
        public OutArgument<string> SelectedNames { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            Common context = new Common(executionContext);
            context.tracingService.Trace("Context has been loaded correctly");

            EntityReference sourceEntityReference = GetSourceEntityReference(context);
            string attributeName = GetAttributeName(context);
            bool retrieveOptionNames = GetRetrieveOptionsNames(context);

            GetSelectedItems(sourceEntityReference, attributeName, retrieveOptionNames, context);
        }

        private EntityReference GetSourceEntityReference(Common context)
        {
            string sourceRecordUrl = SourceRecordUrl.Get<string>(context.codeActivityContext) ?? throw new ArgumentNullException("Source URL is empty");
            context.tracingService.Trace("Source Record URL:'{0}'", sourceRecordUrl);
            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(context.service);
        }


        private string GetAttributeName(Common context)
        {
            string attributeName = AttributeName.Get<string>(context.codeActivityContext) ?? throw new ArgumentNullException("Attribute Name is empty");
            context.tracingService.Trace("Attribute name:'{0}'", attributeName);
            return attributeName;
        }

        private bool GetRetrieveOptionsNames(Common context)
        {
            bool retrieveOptionsNames = RetrieveOptionsNames.Get<bool>(context.codeActivityContext);
            context.tracingService.Trace($"Retrieve Item Name:'{retrieveOptionsNames}'");
            return retrieveOptionsNames;
        }

        private Dictionary<int, string> GetOptionsNames(EntityReference sourceEntityReference, string attributeName, Common context)
        {
            RetrieveAttributeRequest attributeRequest = new RetrieveAttributeRequest
            {
                EntityLogicalName = sourceEntityReference.LogicalName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = false
            };

            RetrieveAttributeResponse response = context.service.Execute(attributeRequest) as RetrieveAttributeResponse;

            MultiSelectPicklistAttributeMetadata attributeMetadata = response.AttributeMetadata as MultiSelectPicklistAttributeMetadata;
            if (attributeMetadata == null)
                throw new InvalidPluginExecutionException($"Attribute {attributeName} is not an expected multi-select optionset / choices type");

            OptionMetadataCollection options = attributeMetadata.OptionSet.Options;
            Dictionary<int, string> optionsTable = new Dictionary<int, string>(options.Count);
            foreach (OptionMetadata optionMetadata in options)
            {
                optionsTable.Add(optionMetadata.Value.Value, optionMetadata.Label.UserLocalizedLabel.Label);
            }

            return optionsTable;
        }


        private void GetSelectedItems(EntityReference sourceEntityReference, string attributeName, bool retrieveOptionNames, Common context)
        {
            if (sourceEntityReference == null || attributeName == null)
            { 
                context.tracingService.Trace("Null parameters have been passed, so string will be empty");
                return;
            }

            Entity sourceEntity = context.service.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(attributeName));
            context.tracingService.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            if (!sourceEntity.Contains(attributeName))
            {
                context.tracingService.Trace("Attribues {0} was not found", attributeName);
                return;
            }

            OptionSetValueCollection optionSetValues = sourceEntity[attributeName] as OptionSetValueCollection;
            if (optionSetValues == null)
                return;

            int numberOptions = optionSetValues.Count;

            if (numberOptions == 0)
            {
                context.tracingService.Trace("No selected options");
                return;
            }

            context.tracingService.Trace("Number of selected options: ", numberOptions);

            Dictionary<int, string> optionsNames = null;
            StringBuilder stringNamesBuilder = null;
            if (retrieveOptionNames)
            {
                optionsNames = GetOptionsNames(sourceEntityReference, attributeName, context);
                stringNamesBuilder = new StringBuilder();
            }

            StringBuilder stringValuesBuilder = new StringBuilder();             

            OptionSetValue value = null;
            for (int i = 0; i < numberOptions; i++)
            {
                value = optionSetValues[i];
                stringValuesBuilder.Append(value.Value);

                if (retrieveOptionNames)
                    stringNamesBuilder.Append(optionsNames[value.Value]);

                if ((i + 1) < numberOptions)
                {
                    stringValuesBuilder.Append(",");
                    if(retrieveOptionNames)
                        stringNamesBuilder.Append(",");
                }
            }

            string values = stringValuesBuilder.ToString();
            this.SelectedValues.Set(context.codeActivityContext, values);
            context.tracingService.Trace($"Values have been retrieved correctly. Values: {values}");

            if (retrieveOptionNames)
            {
                string names = stringNamesBuilder.ToString();
                this.SelectedNames.Set(context.codeActivityContext, names);
                context.tracingService.Trace($"Names have been retrieved correctly. Names: {values}");
            }

        }
    }
}