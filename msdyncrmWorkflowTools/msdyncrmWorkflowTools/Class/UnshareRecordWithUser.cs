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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _SharingRecordURL = this.SharingRecordURL.Get(executionContext);
            if (_SharingRecordURL == null || _SharingRecordURL == "")
            {
                return;
            }
            string[] urlParts = _SharingRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string objectTypeCode = urlParams[0].Replace("etc=", "");
            string objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            EntityReference systemuserReference = this.User.Get(executionContext);

            if (systemuserReference != null) principals.Add(systemuserReference);

            #endregion


            #region "ApplyRoutingRuteamReferenceleRequest Execution"
            string EntityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);

            EntityReference refObject = new EntityReference(EntityName, new Guid(objectId));

            RevokeAccessRequest revoqueRequest = new RevokeAccessRequest();
            revoqueRequest.Target = refObject;

            foreach (EntityReference principalObject in principals)
            {
                revoqueRequest.Revokee = principalObject;
                RevokeAccessResponse revoqueResponse = (RevokeAccessResponse)objCommon.service.Execute(revoqueRequest);
            }

            objCommon.tracingService.Trace("Revoqued Permissions--- OK");
            #endregion

        }
    }
}
