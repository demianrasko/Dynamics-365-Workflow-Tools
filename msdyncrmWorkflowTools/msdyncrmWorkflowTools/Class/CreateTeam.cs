using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CreateTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Team Name")]
        [Default("")]
        public InArgument<string> TeamName { get; set; }

        [RequiredArgument]
        [Input("Team Type")]
        public InArgument<int> TeamType { get; set; }

        [RequiredArgument]
        [Input("Administrator")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public InArgument<EntityReference> Administrator { get; set; }

        [RequiredArgument]
        [Input("Business Unit")]
        [ReferenceTarget(EntityNames.BusinessUnit)]
        public InArgument<EntityReference> BusinessUnit { get; set; }

        [Output("Team")]
        [ReferenceTarget(EntityNames.Team)]
        public OutArgument<EntityReference> createdTeam { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var teamName = TeamName.Get(executionContext);
            var teamType = TeamType.Get(executionContext);
            var administrator = Administrator.Get(executionContext);
            var businessUnit = BusinessUnit.Get(executionContext);

            common.Trace($"teamName={teamName}");

            var createdTeamId = common.CreateTeam(teamName, teamType, administrator, businessUnit);

            createdTeam.Set(executionContext, new EntityReference(EntityNames.Team, createdTeamId));
        }
    }
}
