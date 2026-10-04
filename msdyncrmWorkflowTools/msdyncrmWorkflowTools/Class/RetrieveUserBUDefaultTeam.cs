using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class RetrieveUserBUDefaultTeam : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        
        [Output("DefaultTeam")]
        [ReferenceTarget("team")]
        public OutArgument<EntityReference> DefaultTeam { get; set; }
        
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var user = User.Get(executionContext);
            

            #endregion

           
            var team = objCommon.retrieveUserBUDefaultTeam(user.Id.ToString());
            
            DefaultTeam.Set(executionContext, team);
            
        }
    }
}
