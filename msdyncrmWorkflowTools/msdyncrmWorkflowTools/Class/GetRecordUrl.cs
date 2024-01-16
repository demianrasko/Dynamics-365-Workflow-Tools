using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Gets the URL for a record 
    /// </summary>
    public class GetRecordUrl : CodeActivity
    {
        /// <summary>
        /// A reference Record URL (use the URL for the record on which the flow is running)
        /// </summary>
        [RequiredArgument]
        [Input("Reference Record URL")]
        public InArgument<string> ReferenceRecordUrl { get; set; }

        /// <summary>
        /// ID of the record
        /// </summary>
        [RequiredArgument]
        [Input("Record ID")]
        public InArgument<string> RecordId { get; set; }

        /// <summary>
        /// Logical name of the entity 
        /// </summary>
        [RequiredArgument]
        [Input("Entity Logical Name")]
        public InArgument<string> EntityName { get; set; }

        /// <summary>
        /// URL of the record
        /// </summary>
        [Output("Record URL")]
        public OutArgument<string> RecordUrl { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            Common objCommon = new Common(context);
            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            string entityName = this.EntityName.Get(context);
            Guid recordId = Guid.Parse(this.RecordId.Get(context));
            string referenceRecordUrl = this.ReferenceRecordUrl.Get(context);

            var entityCode = objCommon.GetEntityCodeFromName(entityName, objCommon.service);
            var recordUrl = commonClass.GetRecordUrl(referenceRecordUrl, entityCode, recordId);
            this.RecordUrl.Set(context, recordUrl);
        }
    }
}
