using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CheckUserInRole : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Role")]
        [ReferenceTarget("role")]
        public InArgument<EntityReference> Role { get; set; }

        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [Output("isUserInRole")]
        public OutArgument<bool> isUserInRole { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);

            common.Trace($"RoleId: {roleReference.Id.ToString()} ");
            #endregion

            var userId = User.Get(executionContext)?.Id ?? common.context.InitiatingUserId;
            common.Trace($"Checking association between user {userId} and role.");

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
                        "systemuserid", ConditionOperator.Equal, userId)
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
            var matchEntities = common.service.RetrieveMultiple(linkQuery);

            // if an entity is returned then the user is a member
            // of the role
            var userInRole = (matchEntities.Entities.Count > 0);

            common.Trace("{0}", userInRole ? "User belongs to the role." : "User does not belong to the role.");

            isUserInRole.Set(executionContext, userInRole);
        }
    }
}
