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
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> Administrator { get; set; }

        [RequiredArgument]
        [Input("Business Unit")]
        [ReferenceTarget("businessunit")]
        public InArgument<EntityReference> BusinessUnit { get; set; }

        [Output("Team")]
        [ReferenceTarget("team")]
        public OutArgument<EntityReference> createdTeam { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var teamName = TeamName.Get(executionContext);
            var teamType = TeamType.Get(executionContext);
            var administrator = Administrator.Get(executionContext);
            var businessUnit = BusinessUnit.Get(executionContext);

            common.Trace($"teamName={teamName}");
            #endregion

            #region "Create the Team"

            var createdTeamId = common.CreateTeam(teamName, teamType, administrator, businessUnit);

            createdTeam.Set(executionContext, new EntityReference("team", createdTeamId));

            #endregion
        }
    }
}
