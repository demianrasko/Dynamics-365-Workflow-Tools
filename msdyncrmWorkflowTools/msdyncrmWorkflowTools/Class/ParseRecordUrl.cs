using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class ParseRecordUrl : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Record URL")]
        public InArgument<String> RecordUrl { get; set; }

        [Output("Entity Logical Name")]
        public OutArgument<String> EntityLogicalName { get; set; }

        [Output("Entity Type Code")]
        public OutArgument<String> EntityTypeCode { get; set; }

        [Output("Record Id")]
        public OutArgument<String> RecordId { get; set; }

        [Output("Record Url")]
        public OutArgument<String> ParsedRecordUrl { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String recordUrl = this.RecordUrl.Get(executionContext);
            #endregion

            this.ParsedRecordUrl.Set(executionContext, recordUrl ?? String.Empty);

            if (String.IsNullOrWhiteSpace(recordUrl))
            {
                this.EntityLogicalName.Set(executionContext, String.Empty);
                this.EntityTypeCode.Set(executionContext, String.Empty);
                this.RecordId.Set(executionContext, String.Empty);
                return;
            }

            try
            {
                DynamicUrlParser parser = new DynamicUrlParser(recordUrl);
                String logicalName = parser.GetEntityLogicalName(objCommon.service) ?? String.Empty;

                this.EntityLogicalName.Set(executionContext, logicalName);
                this.EntityTypeCode.Set(executionContext, parser.EntityTypeCode.ToString());
                this.RecordId.Set(executionContext, parser.Id.ToString());
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace(String.Format("ParseRecordUrl - Error: {0}", ex.ToString()));
                throw;
            }
        }
    }
}
