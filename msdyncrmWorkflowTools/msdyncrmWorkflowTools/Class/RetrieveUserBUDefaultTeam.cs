using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference user = this.User.Get(executionContext);
            

            #endregion

           
            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            EntityReference team = commonClass.retrieveUserBUDefaultTeam(user.Id.ToString());
            
            this.DefaultTeam.Set(executionContext, team);
            
        }
    }
}
