using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Create Team")]
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
            var createdTeamId = common.CreateTeam(TeamName.Get(executionContext), TeamType.Get(executionContext), Administrator.Get(executionContext), BusinessUnit.Get(executionContext));

            createdTeam.Set(executionContext, new EntityReference(EntityNames.Team, createdTeamId));
        }
    }
}
