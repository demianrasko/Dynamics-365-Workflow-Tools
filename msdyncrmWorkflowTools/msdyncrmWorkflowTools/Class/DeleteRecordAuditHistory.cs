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
            if (_RecordURL == null || _RecordURL == "")
            {
                return;
            }
            var urlParts = _RecordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", "");
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);
            var objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);


            #endregion

            #region "DeleteRecordAuditHistory"

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);

            commonClass.DeleteRecordAuditHistory(entityName,objectId);
            

            #endregion



        }
    }
}
