using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class UnshareRecordWithUser : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Sharing Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> SharingRecordURL { get; set; }

        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        List<EntityReference> principals = new List<EntityReference>();
        #endregion


        protected override void Execute(CodeActivityContext executionContext)
        {


            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _SharingRecordURL = SharingRecordURL.Get(executionContext);
            if (_SharingRecordURL == null || _SharingRecordURL == "")
            {
                return;
            }
            var urlParts = _SharingRecordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", "");
            var objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            var systemuserReference = User.Get(executionContext);

            if (systemuserReference != null) principals.Add(systemuserReference);

            #endregion


            #region "ApplyRoutingRuteamReferenceleRequest Execution"
            var EntityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);

            var refObject = new EntityReference(EntityName, new Guid(objectId));

            var revoqueRequest = new RevokeAccessRequest();
            revoqueRequest.Target = refObject;

            foreach (var principalObject in principals)
            {
                revoqueRequest.Revokee = principalObject;
                var revoqueResponse = (RevokeAccessResponse)objCommon.service.Execute(revoqueRequest);
            }

            objCommon.tracingService.Trace("Revoqued Permissions--- OK");
            #endregion

        }
    }
}
