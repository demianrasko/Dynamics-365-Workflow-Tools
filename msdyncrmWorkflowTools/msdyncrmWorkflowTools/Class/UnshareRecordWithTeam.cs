using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools
{
    public class UnshareRecordWithTeam : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var sharingRecordUrl = SharingRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(sharingRecordUrl))
            {
                throw new InvalidPluginExecutionException("Sharing Record URL is required.");
            }

            var parsedUrl = Utility.ParseRecordUrl(sharingRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var objectId = parsedUrl.Id;
            
            common.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var teamReference = Team.Get(executionContext);

            if (teamReference != null) principals.Add(teamReference);
            #endregion
            
            #region "ApplyRoutingRuteamReferenceleRequest Execution"
            var entityName = common.GetEntityNameFromCode(objectTypeCode);

            var refObject = new EntityReference(entityName, new Guid(objectId));

            var request = new RevokeAccessRequest
            {
                Target = refObject
            };

            foreach (var principalObject in principals)
            {
                request.Revokee = principalObject;
                
                common.service.Execute(request);
            }

            common.Trace("Revoked Permissions--- OK");

            #endregion
        }
    }
}
