using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class DeleteRecordAuditHistory : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"


            var _RecordURL = RecordURL.Get(executionContext);
            if (_RecordURL == null || _RecordURL == string.Empty)
            {
                return;
            }
            var parsedUrl = Utility.ParseRecordUrl(_RecordURL);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode);
            var objectId = parsedUrl.Id;
            objCommon.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);


            #endregion

            #region "DeleteRecordAuditHistory"


            objCommon.DeleteRecordAuditHistory(entityName,objectId);
            

            #endregion



        }
    }
}
