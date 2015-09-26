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
    public class CheckUserInRole : CodeActivity
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> Role { get; set; }

        [Output("isUserInRole")]
        public OutArgument<bool> isUserInRole { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference roleReference = this.Role.Get(executionContext);

            objCommon.tracingService.Trace(String.Format("RoleId: {0} ", roleReference.Id.ToString()));
            #endregion

            QueryExpression query = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("roleid"),
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

                Console.WriteLine("Role {0} is retrieved.", givenRole);

                Console.WriteLine("Checking association between user and role.");
                // Establish a SystemUser link for a query.
                LinkEntity systemUserLink = new LinkEntity()
                {
                    LinkFromEntityName = "systemuserroles",
                    LinkFromAttributeName = "systemuserid",
                    LinkToEntityName = "systemuser",
                    LinkToAttributeName = "systemuserid",
                    LinkCriteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            "systemuserid", ConditionOperator.Equal, objCommon.context.InitiatingUserId)
                    }
                }
                };

                // Build the query.
                QueryExpression linkQuery = new QueryExpression()
                {
                    EntityName = "role",
                    ColumnSet = new ColumnSet("roleid"),
                    LinkEntities =
                {
                    new LinkEntity()
                    {
                        LinkFromEntityName = "role",
                        LinkFromAttributeName = "roleid",
                        LinkToEntityName = "systemuserroles",
                        LinkToAttributeName = "roleid",
                        LinkEntities = {systemUserLink}
                    }
                },
                    Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("roleid", ConditionOperator.Equal, givenRole.Id)
                    }
                }
                };

                // Retrieve matching roles.
                EntityCollection matchEntities = objCommon.service.RetrieveMultiple(linkQuery);

                // if an entity is returned then the user is a member
                // of the role
                Boolean UserInRole = (matchEntities.Entities.Count > 0);

                if (UserInRole)
                    Console.WriteLine("User do not belong to the role.");
                else
                    Console.WriteLine("User belong to this role.");

                this.isUserInRole.Set(executionContext, UserInRole);

            }

        }
    }
}
