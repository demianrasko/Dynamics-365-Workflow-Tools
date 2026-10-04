using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class MapMultiSelectOptionSet : WorkflowActivityBase
    {
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [Input("Source Attributes")]
        public InArgument<string> SourceAttributes { get; set; }

        [Input("Target Record URL")]
        public InArgument<string> TargetRecordUrl { get; set; }

        [Input("Target Attributes")]
        public InArgument<string> TargetAttributes { get; set; }

        /// <summary>
        /// Indicate if the existing selected values in the target multi-select optionset attributes will be maintained.
        /// By default, it will remove the existing values and assign the new ones given by the argument "AttributesValues"
        /// </summary>
        [Input("Keep Existing Values")]
        [Default("false")]
        public InArgument<bool> KeepExistingValues { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var sourceAttributes = GetSourceAttributes(executionContext, common);
            var targetAttributes = GetTargetAttributes(executionContext, common);
            var sourceEntityReference = GetSourceEntityReference(executionContext, common.service);
            var targetEntityReference = GetTargetEntityReference(executionContext, common.service);
            var targetEntity = BuildTargetEntity(sourceEntityReference, targetEntityReference, sourceAttributes, targetAttributes,common,common.service, executionContext);

            if (targetEntity != null)
            {
                common.service.Update(targetEntity);
                common.Trace("Target entity record updated correctly.");
            }
            else
            {
                common.Trace("Target entity record was NOT updated. ");
            }
        }

        private EntityReference GetSourceEntityReference(CodeActivityContext executionContext, IOrganizationService organizationService)
        {
            var sourceRecordUrl = SourceRecordUrl.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Source URL is empty");
            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(organizationService);
        }

        private EntityReference GetTargetEntityReference(CodeActivityContext executionContext, IOrganizationService organizationService)
        {
            var targetRecordUrl = TargetRecordUrl.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Target URL is empty");
            return new DynamicUrlParser(targetRecordUrl).ToEntityReference(organizationService);
        }

        private string[] GetSourceAttributes(CodeActivityContext executionContext, Common common)
        {
            var sourceAttributes = SourceAttributes.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Source Attributes is empty");
            var sourceAttributesArray = sourceAttributes.Split(',');

            if (sourceAttributesArray == null || sourceAttributesArray.Length == 0)
            {
                common.Trace("No source attributes could be found");
                return null;
            }
            else
            {
                return sourceAttributesArray;
            }
        }

        private string[] GetTargetAttributes(CodeActivityContext executionContext, Common common)
        {
            var targetAttributes = TargetAttributes.Get<string>(executionContext) ?? throw new InvalidPluginExecutionException("Target Attributes is empty");
            var targetAttributesArray = targetAttributes.Split(',');

            if (targetAttributesArray == null || targetAttributesArray.Length == 0)
            {
                common.Trace("No target attributes could be found");
                return null;
            }
            else
            {
                return targetAttributesArray;
            }
        }

        private Entity BuildTargetEntity(EntityReference sourceEntityReference, EntityReference targetEntityReference, string[] sourceAttributes, string[] targetAttributes, Common common, IOrganizationService organizationService, CodeActivityContext executionContext)
        {
            if (sourceEntityReference == null || targetEntityReference == null || sourceAttributes == null || targetAttributes == null)
            {
                return null;
            }

            var numberSourceAttribute = sourceAttributes.Length;
            var numberTargetAttribute = targetAttributes.Length;
            if (numberSourceAttribute != numberTargetAttribute)
            {
                common.Trace("Number of source attributes ({0}) doesn't match the number of target attributes ({1}).", numberSourceAttribute, numberTargetAttribute);
                return null;
            }

            var sourceEntity = organizationService.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(sourceAttributes));
            common.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            var targetEntity = new Entity(targetEntityReference.LogicalName, targetEntityReference.Id);
            string targetAttribute = null;
            var attributeMappedCounter = 0;

            OptionSetValueCollection sourceNewValues = null;
            OptionSetValueCollection targetExistingValues = null;

            for (var i = 0; i < numberSourceAttribute; i++)
            {
                var sourceAttribute = sourceAttributes[i];
                if (sourceEntity.Contains(sourceAttribute))
                {
                    sourceNewValues = sourceEntity[sourceAttribute] as OptionSetValueCollection;
                    if (typeof(OptionSetValueCollection).Equals(sourceNewValues.GetType()))
                    {
                        targetAttribute = targetAttributes[i];
                        targetExistingValues = GetExistingAttributeValues(targetEntity.ToEntityReference(), targetAttribute, common, organizationService, executionContext);
                        targetEntity.Attributes.Add(targetAttribute, MergeOptionSetCollections(sourceNewValues, targetExistingValues,common));
                        attributeMappedCounter++;
                    }
                    else
                    {
                        common.Trace("Attribute '{0}' is not an Option Set", sourceAttribute);
                    }
                }
                else
                {
                    common.Trace("Attribute '{0}' wasn't found in source record", sourceAttribute);
                }
            }

            common.Trace("Target entity record has been built correctly. '{0}' of '{1}' attributes were mapped. ", attributeMappedCounter, numberSourceAttribute);

            return targetEntity;
        }
        private OptionSetValueCollection GetExistingAttributeValues(EntityReference targetEntityReference, string attributeName, Common common, IOrganizationService organizationService, CodeActivityContext executionContext)
        {
            common.Trace("Retrieving existing values");

            var attributeValues = KeepExistingValues.Get<bool>(executionContext);

            if (attributeValues == false)
            {
                return null;
            }

            var record = organizationService.Retrieve(targetEntityReference.LogicalName, targetEntityReference.Id, new ColumnSet(new string[] { attributeName }));

            common.Trace("Existing values have been retrieved correctly");

            if (record.Contains(attributeName))
            {
                return record[attributeName] as OptionSetValueCollection;
            }
            else
            {
                return null;
            }
        }

        private OptionSetValueCollection MergeOptionSetCollections(OptionSetValueCollection newValues, OptionSetValueCollection existingValues, Common common)
        {
            common.Trace("Merging new and exiting multi-select optionset values");

            if (existingValues == null && newValues == null)
            {
                return new OptionSetValueCollection();
            }

            if (existingValues == null)
            {
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

            common.Trace("New and exiting multi-select optionset values have been merged correctly. Total options: {0} ", existingValues.Count);
            return existingValues;
        }
    }
}
