using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class AddRoleToTeam : CodeActivity
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> Role { get; set; }

        [RequiredArgument]
        [Input("Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> Team { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);
            var teamReference = Team.Get(executionContext);

            objCommon.tracingService.Trace("RoleId: {0} - TeamID: {1} ", roleReference.Id, teamReference.Id);
            #endregion

            try
            {
                var roleId = Utility.GetRoleIdInBusinessUnit(objCommon.service, objCommon.tracingService, new EntityReference("team", teamReference.Id), roleReference.Id);

                if (roleId == null)
                {
                    return;
                }

                var entRoleId = roleId.Value;

                if (IsAssociate(objCommon.service, teamReference.Id, entRoleId))
                {
                    return;
                }
                
                objCommon.tracingService.Trace("Associate | RoleId: {0} - TeamID: {1} ", entRoleId, teamReference.Id);

                objCommon.service.Associate(
                    "team",
                    teamReference.Id,
                    new Relationship("teamroles_association"),
                    new EntityReferenceCollection { new EntityReference("role", entRoleId) });
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace("Message: {0} \nStackTrace: {1}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        private static bool IsAssociate(IOrganizationService organizationService, Guid teamId, Guid rolesId)
        {
            var query = new QueryExpression("teamroles")
            {
                TopCount = 1
            };

            query.ColumnSet.AddColumns("teamroleid");

            query.Criteria.AddCondition("roleid", ConditionOperator.Equal, rolesId);
            query.Criteria.AddCondition("teamid", ConditionOperator.Equal, teamId);


            var entityCollection = organizationService.RetrieveMultiple(query);

            return entityCollection.Entities.Count > 0;
        }
    }
}