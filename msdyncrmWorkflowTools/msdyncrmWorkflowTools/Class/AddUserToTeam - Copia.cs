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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference userReference = this.User.Get(executionContext);
            EntityReference teamReference = this.Team.Get(executionContext);

            objCommon.tracingService.Trace(String.Format("UserID: {0} - TeamID: {1} ", userReference.Id.ToString(), teamReference.Id.ToString()));
            #endregion

            AddMembersTeamRequest req = new AddMembersTeamRequest();
            req.TeamId = teamReference.Id;
            req.MemberIds = new[] { userReference.Id};
            AddMembersTeamResponse res = (AddMembersTeamResponse)objCommon.service.Execute(req);

        }
    }
}
