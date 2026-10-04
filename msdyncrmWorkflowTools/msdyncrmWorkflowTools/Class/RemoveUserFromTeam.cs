using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class RemoveUserFromTeam : CodeActivity
    {
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var userReference = User.Get(executionContext);
            var teamReference = Team.Get(executionContext);

            objCommon.tracingService.Trace(String.Format("UserID: {0} - TeamID: {1} ", userReference.Id.ToString(), teamReference.Id.ToString()));
            #endregion

            var req = new RemoveMembersTeamRequest();
            req.TeamId = teamReference.Id;
            req.MemberIds = new[] { userReference.Id};
            var res = (RemoveMembersTeamResponse)objCommon.service.Execute(req);

        }
    }
}
