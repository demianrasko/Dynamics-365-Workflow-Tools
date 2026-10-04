using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class AddUserToTeam : CodeActivity
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

            var req = new AddMembersTeamRequest();
            req.TeamId = teamReference.Id;
            req.MemberIds = new[] { userReference.Id};
            var res = (AddMembersTeamResponse)objCommon.service.Execute(req);

        }
    }
}
