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
    public class GetOptionSetValue : CodeActivity
    {
        [RequiredArgument]
        [Input("Source Record URL")]
        public InArgument<string> SourceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Output("Value")]
        public OutArgument<int> SelectedValue { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            EntityReference sourceEntityReference = GetSourceEntityReference(objCommon.tracingService, executionContext, objCommon.service);
            string attributeName = GetAttributeName(objCommon.tracingService, executionContext);

            int value= GetValue(sourceEntityReference, attributeName, objCommon.tracingService, objCommon.service);

            this.SelectedValue.Set(executionContext, value);
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

        

        private int GetValue(EntityReference sourceEntityReference, string attributeName, ITracingService tracingService, IOrganizationService organizationService)
        {
            if (sourceEntityReference == null || attributeName == null)
            {
                tracingService.Trace("Null parameters have been passed, so string will be empty");
                return 0;
            }

            Entity sourceEntity = organizationService.Retrieve(sourceEntityReference.LogicalName, sourceEntityReference.Id, new ColumnSet(attributeName));
            tracingService.Trace("Source record has been retrieved correctly. Id:{0}", sourceEntity.Id);

            if (!sourceEntity.Contains(attributeName))
            {
                tracingService.Trace("Attribues {0} was not found", attributeName);
                return 0;
            }
            int value = 0;
            if (sourceEntity.Attributes.Contains(attributeName))
            {
                value = ((OptionSetValue)sourceEntity.Attributes[attributeName]).Value;
            }
            
            return value;
        }
    }
}