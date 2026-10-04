using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class ShareRecordWithUser : CodeActivity
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
            principals.Clear();

            if (user != null)
            {
                principals.Add(user);
            }

            #endregion
            
            #region "ApplyRoutingRuteamReferenceleRequest Execution"
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);

            var refObject = new EntityReference(entityName, new Guid(objectId));

            objCommon.tracingService.Trace("Grant Request--- Start");
            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);

            var grantRequest = new GrantAccessRequest
            {
                Target = refObject,
                PrincipalAccess = new PrincipalAccess
                {
                    AccessMask = msdyncrmWorkflowTools_Class.GetMask(
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

                objCommon.service.Execute(grantRequest);
            }

            objCommon.tracingService.Trace("Grant Request--- end");

            #endregion
        }
    }
}
