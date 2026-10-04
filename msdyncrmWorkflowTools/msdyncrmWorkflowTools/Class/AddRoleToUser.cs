using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

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

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var roleReference = Role.Get(executionContext);
            var userReference = User.Get(executionContext);

            objCommon.tracingService.Trace(
                $"RoleId: {roleReference.Id.ToString()} - UserID: {userReference.Id.ToString()} ");
            #endregion

            var systemUser = objCommon.service.Retrieve("systemuser", userReference.Id, new ColumnSet("businessunitid"));

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

            if (givenRoles.Entities.Count <= 0)
            {
                return;
            }

            var givenRole = givenRoles.Entities[0].ToEntity<Entity>();
            var entRootRole = (EntityReference)givenRole.Attributes["parentrootroleid"];

            objCommon.tracingService.Trace("Role {0} is retrieved.", givenRole);

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
            
            objCommon.service.Associate(
                "systemuser",
                userReference.Id,
                new Relationship("systemuserroles_association"),
                new EntityReferenceCollection() { new EntityReference("role", entRoleId) });
        }
    }
}
