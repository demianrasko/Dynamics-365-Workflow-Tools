using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using msdyncrmWorkflowTools;
using System.ServiceModel;


namespace msdyncrmWorkflowTools
{
  
    public class CreateTeam : CodeActivity
    {
        [RequiredArgument]
        [Input("Team Name")]
        [Default("")]
        public InArgument<String> TeamName{ get; set; }


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

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _teamName = this.TeamName.Get(executionContext);
            int _teamType = this.TeamType.Get(executionContext);
            EntityReference _administrator= this.Administrator.Get(executionContext);
            EntityReference _businessUnit= this.BusinessUnit.Get(executionContext);

            objCommon.tracingService.Trace("_teamName=" + _teamName );
            #endregion


            #region "Associate Execution"

            try
            {
                msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service);
                Guid createdTeamId= commonClass.CreateTeam(_teamName,_teamType, _administrator, _businessUnit);
                this.createdTeam.Set(executionContext, new EntityReference("team", createdTeamId));

            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
                // if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                //{
                // throw ex;
                //}
            }
            catch (System.Exception ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
            }
            #endregion

        }
    }
}
