using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools
{
    public class UnshareRecordWithUser : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Sharing Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> SharingRecordURL { get; set; }

        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

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

            var user = User.Get(executionContext);

            if (user != null)
            {
                principals.Add(user);
            }

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
