using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk.Workflow;


namespace msdyncrmWorkflowTools.Class
{
    public class DeleteRecordAuditHistory : CodeActivity
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"


            var _RecordURL = RecordURL.Get(executionContext);
            if (_RecordURL == null || _RecordURL == string.Empty)
            {
                return;
            }
            var parsedUrl = Utility.ParseRecordUrl(_RecordURL);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);
            var objectId = parsedUrl.Id;
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);


            #endregion

            #region "DeleteRecordAuditHistory"


            objCommon.DeleteRecordAuditHistory(entityName,objectId);
            

            #endregion



        }
    }
}
