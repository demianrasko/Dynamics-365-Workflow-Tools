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
    public class CheckUserInTeam : CodeActivity
    {
        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        [Output("isUserInTeam")]
        public OutArgument<bool> isUserInTeam { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference teamReference = this.Team.Get(executionContext);

            objCommon.tracingService.Trace(String.Format("TeamId: {0} ", teamReference.Id.ToString()));
            #endregion

            string fetchXML = @"<fetch version=""1.0"" output-format=""xml - platform"" mapping=""logical"" distinct=""true""><entity name=""team"">
                         <attribute name=""teamid""/>
                         <filter type=""and"">
                          <condition attribute=""teamid"" operator=""eq"" value="""+ teamReference.Id.ToString() + @"""/>
                                </filter>
                                <link-entity name=""teammembership"" from=""teamid"" to=""teamid"" visible=""false"" intersect=""true"">
                                             <link-entity name=""systemuser"" from=""systemuserid"" to=""systemuserid"" alias=""ag"">
                                                        <filter type=""and"">
                                                           <condition attribute=""systemuserid"" operator=""eq""  uitype=""systemuser"" value= """+ objCommon.context.InitiatingUserId.ToString() + @"""/>
                                                                 </filter>
                                                               </link-entity>
                                                             </link-entity>
                                                           </entity></fetch> ";

            objCommon.tracingService.Trace(String.Format("FetchXML: {0} ", fetchXML));
            EntityCollection givenTeams = objCommon.service.RetrieveMultiple(new FetchExpression (fetchXML));

            Boolean UserInTeam = (givenTeams.Entities.Count > 0);

            if (UserInTeam)
                Console.WriteLine("User do not belong to the team.");
            else
                Console.WriteLine("User belong to this team.");

            this.isUserInTeam.Set(executionContext, UserInTeam);

        }
    }
}
