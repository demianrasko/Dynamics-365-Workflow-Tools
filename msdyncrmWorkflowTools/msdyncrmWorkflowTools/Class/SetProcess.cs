using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace msdyncrmWorkflowTools.Class
{
    public class SetProcess : CodeActivity
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> ClonningRecordURL { get; set; }

        [Input("Process")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Process { get; set; }

 

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _ClonningRecordURL = this.ClonningRecordURL.Get(executionContext);
            if (_ClonningRecordURL == null || _ClonningRecordURL == "")
            {
                return;
            }
            string[] urlParts = _ClonningRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string objectTypeCode = urlParams[0].Replace("etc=", "");
            string entityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
            string objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            EntityReference process = this.Process.Get(executionContext);
            

            #endregion

            #region "SetProcess Execution"

            SetProcessRequest req = new SetProcessRequest();
            req.Target = new EntityReference(entityName, new Guid(objectId));
            req.NewProcess = process;
            SetProcessResponse res = (SetProcessResponse)objCommon.service.Execute(req);

            #endregion

        }
    }
}
