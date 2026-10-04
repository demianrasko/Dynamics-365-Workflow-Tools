using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CheckUserInTeam : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }

        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [Output("isUserInTeam")]
        public OutArgument<bool> isUserInTeam { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var teamReference = Team.Get(executionContext);
            var userReference = User.Get(executionContext);

            common.Trace($"TeamId: {teamReference.Id.ToString()} ");
            #endregion

            var userId = common.context.InitiatingUserId.ToString();
            if (userReference != null)
            {
                userId = userReference.Id.ToString();
            }

            var fetchXml = @"<fetch version=""1.0"" output-format=""xml - platform"" mapping=""logical"" distinct=""true""><entity name=""team"">
                         <attribute name=""teamid""/>
                         <filter type=""and"">
                          <condition attribute=""teamid"" operator=""eq"" value="""+ teamReference.Id.ToString() + @"""/>
                                </filter>
                                <link-entity name=""teammembership"" from=""teamid"" to=""teamid"" visible=""false"" intersect=""true"">
                                             <link-entity name=""systemuser"" from=""systemuserid"" to=""systemuserid"" alias=""ag"">
                                                        <filter type=""and"">
                                                           <condition attribute=""systemuserid"" operator=""eq""  uitype=""systemuser"" value= """+ userId + @"""/>
                                                                 </filter>
                                                               </link-entity>
                                                             </link-entity>
                                                           </entity></fetch> ";

            common.Trace($"FetchXML: {fetchXml} ");
            var givenTeams = common.service.RetrieveMultiple(new FetchExpression (fetchXml));

            var userInTeam = (givenTeams.Entities.Count > 0);

            common.Trace("{0}", userInTeam ? "User belongs to the team." : "User does not belong to the team.");

            isUserInTeam.Set(executionContext, userInTeam);
        }
    }
}
