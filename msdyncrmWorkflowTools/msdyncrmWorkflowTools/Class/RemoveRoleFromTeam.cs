using Microsoft.Crm.Sdk.Messages;
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
    public class RemoveRoleFromTeam : CodeActivity
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

            objCommon.tracingService.Trace(String.Format("RoleId: {0} - TeamID: {1} ", roleReference.Id.ToString(), teamReference.Id.ToString()));
            #endregion

            var systemUser = (Entity)objCommon.service.Retrieve(
                        "team",
                        teamReference.Id,
                        new ColumnSet("businessunitid"));
            var businessUnit = (EntityReference)systemUser.Attributes["businessunitid"];

            var query = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet( "parentrootroleid"),
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
            var givenRoles = objCommon.service.RetrieveMultiple(query);


            
            if (givenRoles.Entities.Count > 0)
            {
                var givenRole = givenRoles.Entities[0].ToEntity<Entity>();
                var entRootRole = (EntityReference)givenRole.Attributes["parentrootroleid"];

                Console.WriteLine("Role {0} is retrieved.", givenRole);


                var query2 = new QueryExpression
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
                var givenRoles2 = objCommon.service.RetrieveMultiple(query2);

                var givenRole2 = givenRoles2.Entities[0].ToEntity<Entity>();
                var entRoleId = (Guid)givenRole2.Attributes["roleid"];

                objCommon.service.Disassociate(
                      "team",
                      teamReference.Id,
                      new Relationship("teamroles_association"), 
                      new EntityReferenceCollection() { new EntityReference("role", entRoleId) });
                
            }

        }
    }
}
