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
    public class AddRoleToUser : CodeActivity
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> Role { get; set; }

        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference roleReference = this.Role.Get(executionContext);
            EntityReference userReference = this.User.Get(executionContext);

            objCommon.tracingService.Trace(String.Format("RoleId: {0} - UserID: {1} ", roleReference.Id.ToString(), userReference.Id.ToString()));
            #endregion

            Entity systemUser = (Entity)objCommon.service.Retrieve(
                        "systemuser",
                        userReference.Id,
                        new ColumnSet("businessunitid"));
            EntityReference businessUnit = (EntityReference)systemUser.Attributes["businessunitid"];

            QueryExpression query = new QueryExpression
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
            EntityCollection givenRoles = objCommon.service.RetrieveMultiple(query);


            
            if (givenRoles.Entities.Count > 0)
            {
                Entity givenRole = givenRoles.Entities[0].ToEntity<Entity>();
                EntityReference entRootRole = (EntityReference)givenRole.Attributes["parentrootroleid"];

                Console.WriteLine("Role {0} is retrieved.", givenRole);


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

               
                    objCommon.service.Associate(
                          "systemuser",
                          userReference.Id,
                          new Relationship("systemuserroles_association"),
                          new EntityReferenceCollection() { new EntityReference("role", entRoleId) });
                

            }


        }
    }
}
