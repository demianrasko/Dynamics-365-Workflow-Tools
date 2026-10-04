using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

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

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);

            objCommon.tracingService.Trace($"RoleId: {roleReference.Id.ToString()} ");
            #endregion

            objCommon.tracingService.Trace("Checking association between user and role.");

            var systemUserLink = new LinkEntity
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

            var linkQuery = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("parentrootroleid"),
                LinkEntities =
            {
                new LinkEntity
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
                    new ConditionExpression("parentrootroleid", ConditionOperator.Equal, roleReference.Id.ToString())
                }
            }
            };

            // Retrieve matching roles.
            var matchEntities = objCommon.service.RetrieveMultiple(linkQuery);

            // if an entity is returned then the user is a member
            // of the role
            var userInRole = (matchEntities.Entities.Count > 0);

            Console.WriteLine(userInRole ? "User do not belong to the role." : "User belong to this role.");

            isUserInRole.Set(executionContext, userInRole);
        }
    }
}
