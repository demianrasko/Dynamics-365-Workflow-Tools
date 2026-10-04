using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

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
            var sharingRecordUrl = SharingRecordURL.Get(executionContext);
            
            if (string.IsNullOrEmpty(sharingRecordUrl))
            {
                return;
            }

            var urlParts = sharingRecordUrl.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", "");
            var objectId = urlParams[1].Replace("id=", "");
            
            objCommon.tracingService.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var user = User.Get(executionContext);

            if (user != null)
            {
                principals.Add(user);
            }

            #endregion

            #region "ApplyRoutingRuteamReferenceleRequest Execution"
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);

            var refObject = new EntityReference(entityName, new Guid(objectId));

            var request = new RevokeAccessRequest
            {
                Target = refObject
            };

            foreach (var principalObject in principals)
            {
                request.Revokee = principalObject;
                objCommon.service.Execute(request);
            }

            objCommon.tracingService.Trace("Revoked Permissions--- OK");
            #endregion
        }
    }
}
