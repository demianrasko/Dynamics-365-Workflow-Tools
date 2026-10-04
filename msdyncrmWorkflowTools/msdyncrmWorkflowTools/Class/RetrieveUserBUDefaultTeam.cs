using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class RetrieveUserBUDefaultTeam : CodeActivity
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

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var user = User.Get(executionContext);
            

            #endregion

           
            var team = objCommon.retrieveUserBUDefaultTeam(user.Id.ToString());
            
            DefaultTeam.Set(executionContext, team);
            
        }
    }
}
