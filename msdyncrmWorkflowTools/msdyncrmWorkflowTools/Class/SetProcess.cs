using System;
using System.Activities;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools.Class
{
    public class SetProcess : CodeActivity
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ClonningRecordURL { get; set; }

        [Input("Process")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Process { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var cloningRecordUrl = ClonningRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(cloningRecordUrl))
            {
                return;
            }

            var urlParts = cloningRecordUrl.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", string.Empty);
            var objectId = urlParams[1].Replace("id=", string.Empty);
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);

            objCommon.tracingService.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var process = Process.Get(executionContext);
            
            #endregion

            #region "SetProcess Execution"

            var req = new SetProcessRequest
            {
                Target = new EntityReference(entityName, new Guid(objectId)),
                NewProcess = process
            };

            objCommon.service.Execute(req);

            #endregion
        }
    }
}
