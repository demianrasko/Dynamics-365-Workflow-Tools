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
    public class MapMultiSelectOptionSet : CodeActivity
    {
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [Input("Source Attributes")]
        public InArgument<string> SourceAttributes { get; set; }

        [Input("Target Record URL")]
        public InArgument<string> TargetRecordUrl { get; set; }

        [Input("Target Attributes")]
        public InArgument<string> TargetAttributes { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            string[] sourceAttributes = GetSourceAttributes(executionContext, objCommon.tracingService);
            string[] targetAttributes = GetTargetAttributes(executionContext, objCommon.tracingService);
            EntityReference sourceEntityReference = GetSourceEntityReference(executionContext, objCommon.service);
            EntityReference targetEntityReference = GetTargetEntityReference(executionContext, objCommon.service);
            Entity targetEntity = BuildTargetEntity(sourceEntityReference, targetEntityReference, sourceAttributes, targetAttributes,objCommon.tracingService,objCommon.service);

            if (targetEntity != null)
            {
                objCommon.service.Update(targetEntity);
                objCommon.tracingService.Trace("Target entity record updated correctly.");

            }
            else
                objCommon.tracingService.Trace("Target entity record was NOT updated. ");

        }

        private EntityReference GetSourceEntityReference(CodeActivityContext executionContext, IOrganizationService organizationService)
        {
            string sourceRecordUrl = SourceRecordUrl.Get<string>(executionContext) ?? throw new ArgumentNullException("Source URL is empty");
            return new DynamicUrlParser(sourceRecordUrl).ToEntityReference(organizationService);
        }

        private EntityReference GetTargetEntityReference(CodeActivityContext executionContext, IOrganizationService organizationService)
        {
            string targetRecordUrl = TargetRecordUrl.Get<string>(executionContext) ?? throw new ArgumentNullException("Target URL is empty");
            return new DynamicUrlParser(targetRecordUrl).ToEntityReference(organizationService);
        }

        private string[] GetSourceAttributes(CodeActivityContext executionContext, ITracingService tracingService)
        {
            string sourceAttributes = SourceAttributes.Get<string>(executionContext) ?? throw new ArgumentNullException("Source Attributes is empty");
            string[] sourceAttributesArray = sourceAttributes.Split(',');

            if (sourceAttributesArray == null || sourceAttributesArray.Length == 0)
            {
                tracingService.Trace("No source attributes could be found");
                return null;
            }
            else
                return sourceAttributesArray;
        }

        private string[] GetTargetAttributes(CodeActivityContext executionContext, ITracingService tracingService)
        {
            string targetAttributes = TargetAttributes.Get<string>(executionContext) ?? throw new ArgumentNullException("Target Attributes is empty");
            string[] targetAttributesArray = targetAttributes.Split(',');

            if (targetAttributesArray == null || targetAttributesArray.Length == 0)
            {
                tracingService.Trace("No target attributes could be found");
                return null;
            }
            else
                return targetAttributesArray;
        }

        private Entity BuildTargetEntity(EntityReference sourceEntityReference, EntityReference targetEntityReference, string[] sourceAttributes, string[] targetAttributes, ITracingService tracingService, IOrganizationService organizationService)
        {
            if (sourceEntityReference == null || targetEntityReference == null || sourceAttributes == null || targetAttributes == null)
                return null;

            int numberSourceAttribute = sourceAttributes.Length;
            int numberTargetAttribute = targetAttributes.Length;
            if (numberSourceAttribute != numberTargetAttribute)
            {
                tracingService.Trace("Number of source attributes ({0}) doesn't match the number of target attributes ({1}).", numberSourceAttribute, numberTargetAttribute);
                return null;
            }

            Entity sourceEntity = organizationService.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(sourceAttributes));
            tracingService.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            Entity targetEntity = new Entity(targetEntityReference.LogicalName, targetEntityReference.Id);
            string targetAttribute = null;
            int attributeMappedCounter = 0;
            for (int i = 0; i < numberSourceAttribute; i++)
            {
                string sourceAttribute = sourceAttributes[i];
                if (sourceEntity.Contains(sourceAttribute))
                {
                    var optionSetValueCollection = sourceEntity[sourceAttribute] as OptionSetValueCollection;
                    if (typeof(OptionSetValueCollection).Equals(optionSetValueCollection.GetType()))
                    {
                        targetAttribute = targetAttributes[i];
                        targetEntity.Attributes.Add(targetAttribute, optionSetValueCollection);
                        tracingService.Trace("Source attribute '{0}' has been mapped to target attribute {1}. ", sourceAttribute, targetAttribute);
                        attributeMappedCounter++;
                    }
                    else
                        tracingService.Trace("Attribute '{0}' is not an Option Set", sourceAttribute);
                }
                else
                    tracingService.Trace("Attribute '{0}' wasn't found in source record", sourceAttribute);

            }

            tracingService.Trace("Target entity record has been built correctly. '{0}' of '{1}' attributes were mapped. ", attributeMappedCounter, numberSourceAttribute);

            return targetEntity;
        }
    }
}
