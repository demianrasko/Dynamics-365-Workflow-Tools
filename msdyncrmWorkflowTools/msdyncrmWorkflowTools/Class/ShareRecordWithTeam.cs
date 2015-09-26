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
    public class ShareRecordWithTeam : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Sharing Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> SharingRecordURL { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

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

            EntityReference teamReference = this.Team.Get(executionContext);
            principals.Clear();

            if (teamReference != null) principals.Add(teamReference);

            #endregion

            #region "ApplyRoutingRuteamReferenceleRequest Execution"
            string EntityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);

            EntityReference refObject = new EntityReference(EntityName, new Guid(objectId));

            objCommon.tracingService.Trace("Grant Request--- Start");

            GrantAccessRequest grantRequest = new GrantAccessRequest();
            grantRequest.Target = refObject;
            grantRequest.PrincipalAccess = new PrincipalAccess();
            grantRequest.PrincipalAccess.AccessMask = (AccessRights)getMask(executionContext);
            foreach (EntityReference principalObject2 in principals)
            {
                grantRequest.PrincipalAccess.Principal = principalObject2;
                GrantAccessResponse grantResponse = (GrantAccessResponse)objCommon.service.Execute(grantRequest);
            }

            objCommon.tracingService.Trace("Grant Request--- end");

            #endregion

        }

        UInt32 getMask(CodeActivityContext executionContext)
        {
            bool ShareAppend = this.ShareAppend.Get(executionContext);
            bool ShareAppendTo = this.ShareAppendTo.Get(executionContext);
            bool ShareAssign = this.ShareAssign.Get(executionContext);
            bool ShareDelete = this.ShareDelete.Get(executionContext);
            bool ShareRead = this.ShareRead.Get(executionContext);
            bool ShareShare = this.ShareShare.Get(executionContext);
            bool ShareWrite = this.ShareWrite.Get(executionContext);

            UInt32 mask = 0;
            if (ShareAppend)
            {
                mask |= (UInt32)AccessRights.AppendAccess;
            }
            if (ShareAppendTo)
            {
                mask |= (UInt32)AccessRights.AppendToAccess;
            }
            if (ShareAssign)
            {
                mask |= (UInt32)AccessRights.AssignAccess;
            }

            if (ShareDelete)
            {
                mask |= (UInt32)AccessRights.DeleteAccess;
            }
            if (ShareRead)
            {
                mask |= (UInt32)AccessRights.ReadAccess;
            }
            if (ShareShare)
            {
                mask |= (UInt32)AccessRights.ShareAccess;
            }
            if (ShareWrite)
            {
                mask |= (UInt32)AccessRights.WriteAccess;
            }



            return mask;

        }
    }

}
