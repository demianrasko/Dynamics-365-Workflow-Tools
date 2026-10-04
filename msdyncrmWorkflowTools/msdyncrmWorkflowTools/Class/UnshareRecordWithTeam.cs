using System;
using System.Activities;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class UnshareRecordWithTeam : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Sharing Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> SharingRecordURL { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

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

            var parsedUrl = Utility.ParseRecordUrl(sharingRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var objectId = parsedUrl.Id;
            
            objCommon.tracingService.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var teamReference = Team.Get(executionContext);

            if (teamReference != null) principals.Add(teamReference);
            #endregion
            
            #region "ApplyRoutingRuteamReferenceleRequest Execution"
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode);

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
