using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools
{
    public class ShareRecordWithUser : WorkflowActivityBase
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


        /// <summary>
        /// Share Read privilege.
        /// </summary>
        [Input("Read Permission")]
        [Default("True")]
        public InArgument<bool> ShareRead { get; set; }

        /// <summary>
        /// Share Write privilege.
        /// </summary>
        [Input("Write Permission")]
        [Default("False")]
        public InArgument<bool> ShareWrite { get; set; }

        /// <summary>
        /// Share Delete privilege.
        /// </summary>
        [Input("Delete Permission")]
        [Default("False")]
        public InArgument<bool> ShareDelete { get; set; }

        /// <summary>
        /// Share Append privilege.
        /// </summary>
        [Input("Append Permission")]
        [Default("False")]
        public InArgument<bool> ShareAppend { get; set; }

        /// <summary>
        /// Share AppendTo privilege.
        /// </summary>
        [Input("Append To Permission")]
        [Default("False")]
        public InArgument<bool> ShareAppendTo { get; set; }

        /// <summary>
        /// Share Assign privilege.
        /// </summary>
        [Input("Assign Permission")]
        [Default("False")]
        public InArgument<bool> ShareAssign { get; set; }

        /// <summary>
        /// Share Share privilege.
        /// </summary>
        [Input("Share Permission")]
        [Default("False")]
        public InArgument<bool> ShareShare { get; set; }


        List<EntityReference> principals = new List<EntityReference>();
        #endregion


        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var sharingRecordUrl = SharingRecordURL.Get(executionContext);
            if (string.IsNullOrEmpty(sharingRecordUrl))
            {
                return;
            }

            var parsedUrl = Utility.ParseRecordUrl(sharingRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var objectId = parsedUrl.Id;

            common.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var user = User.Get(executionContext);
            principals.Clear();

            if (user != null)
            {
                principals.Add(user);
            }

            #endregion
            
            #region "ApplyRoutingRuteamReferenceleRequest Execution"
            var entityName = common.GetEntityNameFromCode(objectTypeCode);

            var refObject = new EntityReference(entityName, new Guid(objectId));

            common.Trace("Grant Request--- Start");

            var grantRequest = new GrantAccessRequest
            {
                Target = refObject,
                PrincipalAccess = new PrincipalAccess
                {
                    AccessMask = Utility.GetMask(
                        read: ShareRead.Get(executionContext),
                        write: ShareWrite.Get(executionContext),
                        append: ShareAppend.Get(executionContext),
                        appendTo: ShareAppendTo.Get(executionContext),
                        delete: ShareDelete.Get(executionContext),
                        share: ShareShare.Get(executionContext),
                        assign: ShareAssign.Get(executionContext))
                }
            };

            foreach (var principalObject2 in principals)
            {
                grantRequest.PrincipalAccess.Principal = principalObject2;

                common.service.Execute(grantRequest);
            }

            common.Trace("Grant Request--- end");

            #endregion
        }
    }
}
