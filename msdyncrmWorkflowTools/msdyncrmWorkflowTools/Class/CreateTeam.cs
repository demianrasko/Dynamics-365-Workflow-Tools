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
        public InArgument<string> TeamName{ get; set; }


        [RequiredArgument]
        [Input("Team Type")]
        public InArgument<int> TeamType{ get; set; }

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var _teamName = TeamName.Get(executionContext);
            var _teamType = TeamType.Get(executionContext);
            var _administrator= Administrator.Get(executionContext);
            var _businessUnit= BusinessUnit.Get(executionContext);

            objCommon.Trace("_teamName=" + _teamName );
            #endregion


            #region "Associate Execution"

            var createdTeamId= objCommon.CreateTeam(_teamName,_teamType, _administrator, _businessUnit);
            createdTeam.Set(executionContext, new EntityReference("team", createdTeamId));

            #endregion

        }
    }
}
