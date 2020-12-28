using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

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

        
        public void CodeActivity()
        {
            DisplayName = "Add Role To Team";
        }

        protected override void Execute(CodeActivityContext executionContext)
        {
            
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference roleReference = Role.Get(executionContext);
            EntityReference teamReference = Team.Get(executionContext);

            objCommon.tracingService.Trace("RoleId: {0} - TeamID: {1} ", roleReference.Id, teamReference.Id);
            #endregion

            try
            {
                Entity systemUser = objCommon.service.Retrieve(
                            "team",
                            teamReference.Id,
                            new ColumnSet("businessunitid"));
                EntityReference businessUnit = (EntityReference)systemUser.Attributes["businessunitid"];

                QueryExpression query = new QueryExpression
                {
                    EntityName = "role",
                    ColumnSet = new ColumnSet("parentrootroleid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {

                            new ConditionExpression
                            {
                                AttributeName = "roleid",
                                Operator = ConditionOperator.Equal,
                                Values = {roleReference.Id}
                            }
                        }
                    }
                };
                EntityCollection givenRoles = objCommon.service.RetrieveMultiple(query);



                if (givenRoles.Entities.Count > 0)
                {
                    Entity givenRole = givenRoles.Entities[0].ToEntity<Entity>();
                    EntityReference entRootRole = (EntityReference)givenRole.Attributes["parentrootroleid"];

                    objCommon.tracingService.Trace("Role {0} is retrieved.", givenRole.Id);


                    QueryExpression query2 = new QueryExpression
                    {
                        EntityName = "role",
                        ColumnSet = new ColumnSet("roleid"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                        {

                            new ConditionExpression
                            {
                                AttributeName = "parentrootroleid",
                                Operator = ConditionOperator.Equal,
                                Values = { entRootRole.Id}
                            },
                            new ConditionExpression
                            {
                                AttributeName = "businessunitid",
                                Operator = ConditionOperator.Equal,
                                Values = { businessUnit.Id}
                            }
                        }
                        }
                    };
                    EntityCollection givenRoles2 = objCommon.service.RetrieveMultiple(query2);

                    Entity givenRole2 = givenRoles2.Entities[0].ToEntity<Entity>();
                    Guid entRoleId = (Guid)givenRole2.Attributes["roleid"];

                    if (!IsAssociate(objCommon.service, teamReference.Id, entRoleId))
                    {
                        objCommon.tracingService.Trace("Associate | RoleId: {0} - TeamID: {1} ", entRoleId, teamReference.Id);

                        objCommon.service.Associate(
                              "team",
                              teamReference.Id,
                              new Relationship("teamroles_association"),
                              new EntityReferenceCollection() { new EntityReference("role", entRoleId) });
                    }
                }
            }
            catch (Exception ex)
            {
                objCommon.tracingService.Trace("Message: {0} \nStackTrace: {1}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        private bool IsAssociate(IOrganizationService organizationService, Guid teamId, Guid rolesId)
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