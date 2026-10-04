using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools.Class
{
    public class IsMemberOfTeam : CodeActivity
    {
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        [Output("Result")]
        public OutArgument<bool> Result { get; set; }
        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var user = User.Get(executionContext);
            var team = Team.Get(executionContext);
            #endregion

            #region "Is user member of team"

            var isMember = objCommon.IsMemberOfTeam(team.Id, user.Id);

            Result.Set(executionContext, isMember);
            
            #endregion
        }
    }
}
