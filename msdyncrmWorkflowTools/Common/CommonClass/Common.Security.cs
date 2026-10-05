using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// Grants a user or team access to the record a record URL points at. A null principal grants nothing.
        /// </summary>
        public void ShareRecord(string recordUrl, EntityReference principal, AccessRights accessMask)
        {
            var target = GetRecordReference(recordUrl);

            Trace("Grant Request--- Start");

            if (principal != null)
            {
                Service.Execute(new GrantAccessRequest
                {
                    Target = target,
                    PrincipalAccess = new PrincipalAccess { Principal = principal, AccessMask = accessMask }
                });
            }

            Trace("Grant Request--- end");
        }

        /// <summary>
        /// Removes a user's or team's shared access to the record a record URL points at. A null principal revokes nothing.
        /// </summary>
        public void UnshareRecord(string recordUrl, EntityReference principal)
        {
            var target = GetRecordReference(recordUrl);

            if (principal != null)
            {
                Service.Execute(new RevokeAccessRequest { Target = target, Revokee = principal });
            }

            Trace("Revoked Permissions--- OK");
        }

        /// <summary>
        /// Shares a secured (field security) field of a record with users and teams, updates their existing access,
        /// or removes it when both <paramref name="allowRead"/> and <paramref name="allowUpdate"/> are false.
        /// Does nothing when the field is not secured.
        /// </summary>
        /// <param name="record">The record whose field is shared.</param>
        /// <param name="attributeName">Logical name of the secured field.</param>
        /// <param name="allowRead">Grant read access.</param>
        /// <param name="allowUpdate">Grant update access.</param>
        /// <param name="principals">The systemuser and team references to share with; nulls are skipped.</param>
        public void ShareSecuredField(EntityReference record, string attributeName, bool allowRead, bool allowUpdate, params EntityReference[] principals)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = record.LogicalName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };

            var response = (RetrieveAttributeResponse)Service.Execute(request);
            var attribute = response.AttributeMetadata;

            if (attribute?.IsSecured != true || attribute.MetadataId == null)
            {
                Trace($"{record.LogicalName}.{attributeName} is not a secured field; nothing to share.");
                return;
            }

            foreach (var principal in principals.Where(p => p != null))
            {
                var existing = Service.RetrieveMultiple(FieldSharingQuery(attribute.MetadataId.Value, record.Id, principal.Id)).Entities.FirstOrDefault();

                if (existing != null)
                {
                    if (allowRead || allowUpdate)
                    {
                        existing[AttributeNames.ReadAccess] = allowRead;
                        existing[AttributeNames.UpdateAccess] = allowUpdate;

                        Service.Update(existing);
                    }
                    else
                    {
                        Service.Delete(existing.LogicalName, existing.Id);
                    }

                    continue;
                }

                if (!allowRead && !allowUpdate)
                {
                    continue;
                }

                Service.Create(new Entity(EntityNames.PrincipalObjectAttributeAccess)
                {
                    [AttributeNames.AttributeId] = attribute.MetadataId.Value,
                    [AttributeNames.ObjectId] = record,
                    [AttributeNames.PrincipalId] = principal,
                    [AttributeNames.ReadAccess] = allowRead,
                    [AttributeNames.UpdateAccess] = allowUpdate
                });
            }
        }

        public Guid CreateTeam(string teamName, int teamType, EntityReference administrator, EntityReference businessUnit)
        {
            var team = new Entity(EntityNames.Team)
            {
                [AttributeNames.AdministratorId] = administrator,
                [AttributeNames.Name] = teamName,
                [AttributeNames.TeamType] = new OptionSetValue(teamType),
                [AttributeNames.BusinessUnitId] = businessUnit
            };

            return Service.Create(team);
        }

        /// <summary>
        /// Whether a user has a security role, in any business unit copy of it.
        /// </summary>
        /// <param name="userId">The user.</param>
        /// <param name="roleId">The role picked in the workflow (the root role).</param>
        public bool UserHasRole(Guid userId, Guid roleId)
        {
            var hasRole = Service.RetrieveMultiple(UserRoleQuery(userId, roleId)).Entities.Count > 0;
            Trace($"User {userId} {(hasRole ? "has" : "does not have")} role {roleId}.");

            return hasRole;
        }

        /// <summary>
        /// Adds a user to a team.
        /// </summary>
        public void AddTeamMember(Guid teamId, Guid userId)
        {
            Trace($"Adding user {userId} to team {teamId}");
            Service.Execute(new AddMembersTeamRequest { TeamId = teamId, MemberIds = new[] { userId } });
        }

        /// <summary>
        /// Removes a user from a team.
        /// </summary>
        public void RemoveTeamMember(Guid teamId, Guid userId)
        {
            Trace($"Removing user {userId} from team {teamId}");
            Service.Execute(new RemoveMembersTeamRequest { TeamId = teamId, MemberIds = new[] { userId } });
        }

        /// <summary>
        /// Gives a team or user a security role (the copy of the role in their business unit). Does nothing when
        /// the role does not exist or the principal already has it.
        /// </summary>
        /// <param name="principal">A team or systemuser.</param>
        /// <param name="roleId">Any copy of the role (usually the one picked in the workflow).</param>
        public void AddRole(EntityReference principal, Guid roleId)
        {
            var businessUnitRoleId = GetRoleIdInBusinessUnit(principal, roleId);

            if (businessUnitRoleId == null)
            {
                Trace($"Role {roleId} was not found.");
                return;
            }

            if (Service.RetrieveMultiple(PrincipalRoleQuery(principal, businessUnitRoleId.Value)).Entities.Count > 0)
            {
                Trace($"{principal.LogicalName} {principal.Id} already has role {businessUnitRoleId}.");
                return;
            }

            Trace($"Adding role {businessUnitRoleId} to {principal.LogicalName} {principal.Id}");
            Service.Associate(principal.LogicalName, principal.Id, RoleRelationship(principal),
                new EntityReferenceCollection { new EntityReference(EntityNames.Role, businessUnitRoleId.Value) });
        }

        /// <summary>
        /// Removes a security role (the copy of the role in their business unit) from a team or user. Does nothing
        /// when the role does not exist.
        /// </summary>
        /// <param name="principal">A team or systemuser.</param>
        /// <param name="roleId">Any copy of the role (usually the one picked in the workflow).</param>
        public void RemoveRole(EntityReference principal, Guid roleId)
        {
            var businessUnitRoleId = GetRoleIdInBusinessUnit(principal, roleId);

            if (businessUnitRoleId == null)
            {
                Trace($"Role {roleId} was not found.");
                return;
            }

            Trace($"Removing role {businessUnitRoleId} from {principal.LogicalName} {principal.Id}");
            Service.Disassociate(principal.LogicalName, principal.Id, RoleRelationship(principal),
                new EntityReferenceCollection { new EntityReference(EntityNames.Role, businessUnitRoleId.Value) });
        }

        private static Relationship RoleRelationship(EntityReference principal)
        {
            switch (principal.LogicalName)
            {
                case EntityNames.Team:
                    return new Relationship("teamroles_association");
                case EntityNames.SystemUser:
                    return new Relationship("systemuserroles_association");
                default:
                    throw new InvalidPluginExecutionException($"Roles can only be given to teams and users, not {principal.LogicalName}.");
            }
        }

        public bool IsMemberOfTeam(Guid teamId, Guid userId)
        {
            var isMember = Service.RetrieveMultiple(TeamMembershipQuery(teamId, userId)).Entities.Count > 0;
            Trace($"User {userId} {(isMember ? "is" : "is not")} a member of team {teamId}.");

            return isMember;
        }

        /// <summary>
        /// The default team of a user's business unit, or null when there is none.
        /// </summary>
        public EntityReference RetrieveUserBuDefaultTeam(Guid systemUserId)
        {
            var teams = Service.RetrieveMultiple(DefaultTeamForUserQuery(systemUserId)).Entities;

            return teams.Count > 0 ? teams[0].ToEntityReference() : null;
        }

        /// <summary>
        /// Finds the copy of a security role that belongs to a team's or user's business unit.
        /// Roles are copied into every business unit and a principal can only hold the copy from its own
        /// business unit, so this reads the role's root role and returns the role with that root in the
        /// principal's business unit.
        /// </summary>
        /// <param name="principal">The team or systemuser whose business unit is used.</param>
        /// <param name="roleId">Any copy of the role (usually the one picked in the workflow).</param>
        /// <returns>The role id in the principal's business unit, or null if <paramref name="roleId"/> does not exist.</returns>
        public Guid? GetRoleIdInBusinessUnit(EntityReference principal, Guid roleId)
        {
            var principalRecord = Service.Retrieve(principal.LogicalName, principal.Id, new ColumnSet(AttributeNames.BusinessUnitId));
            var businessUnit = (EntityReference)principalRecord.Attributes[AttributeNames.BusinessUnitId];

            var roleQuery = new QueryExpression
            {
                EntityName = EntityNames.Role,
                ColumnSet = new ColumnSet(AttributeNames.ParentRootRoleId),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = AttributeNames.RoleId,
                            Operator = ConditionOperator.Equal,
                            Values = { roleId }
                        }
                    }
                }
            };

            var givenRoles = Service.RetrieveMultiple(roleQuery);

            if (givenRoles.Entities.Count <= 0)
            {
                return null;
            }

            var givenRole = givenRoles.Entities[0];
            var rootRole = (EntityReference)givenRole.Attributes[AttributeNames.ParentRootRoleId];

            Trace($"Role {givenRole.Id} is retrieved.");

            var businessUnitRoleQuery = new QueryExpression
            {
                EntityName = EntityNames.Role,
                ColumnSet = new ColumnSet(AttributeNames.RoleId),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = AttributeNames.ParentRootRoleId,
                            Operator = ConditionOperator.Equal,
                            Values = { rootRole.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = AttributeNames.BusinessUnitId,
                            Operator = ConditionOperator.Equal,
                            Values = { businessUnit.Id }
                        }
                    }
                }
            };

            var businessUnitRoles = Service.RetrieveMultiple(businessUnitRoleQuery);

            return (Guid)businessUnitRoles.Entities[0].Attributes[AttributeNames.RoleId];
        }

        /// <summary>The default team of the business unit a user belongs to.</summary>
        public static QueryExpression DefaultTeamForUserQuery(Guid systemUserId)
        {
            var query = new QueryExpression(EntityNames.Team)
            {
                ColumnSet = new ColumnSet(AttributeNames.Name, AttributeNames.BusinessUnitId, AttributeNames.TeamId, AttributeNames.TeamType),
                Distinct = true
            };
            query.AddOrder(AttributeNames.Name, OrderType.Ascending);
            query.Criteria.AddCondition(AttributeNames.TeamType, ConditionOperator.Equal, 0);
            query.Criteria.AddCondition(AttributeNames.IsDefault, ConditionOperator.Equal, true);

            var businessUnit = query.AddLink(EntityNames.BusinessUnit, AttributeNames.BusinessUnitId, AttributeNames.BusinessUnitId, JoinOperator.Inner);
            businessUnit.EntityAlias = "ae";

            var user = businessUnit.AddLink(EntityNames.SystemUser, AttributeNames.BusinessUnitId, AttributeNames.BusinessUnitId, JoinOperator.Inner);
            user.EntityAlias = "af";
            user.LinkCriteria.AddCondition(AttributeNames.SystemUserId, ConditionOperator.Equal, systemUserId);

            return query;
        }

        /// <summary>The field-sharing record (principalobjectattributeaccess) for one secured field, record and principal.</summary>
        public static QueryExpression FieldSharingQuery(Guid attributeId, Guid objectId, Guid principalId)
        {
            var query = new QueryExpression(EntityNames.PrincipalObjectAttributeAccess)
            {
                ColumnSet = new ColumnSet(AttributeNames.ReadAccess, AttributeNames.UpdateAccess),
                TopCount = 1
            };
            query.Criteria.AddCondition(AttributeNames.AttributeId, ConditionOperator.Equal, attributeId);
            query.Criteria.AddCondition(AttributeNames.ObjectId, ConditionOperator.Equal, objectId);
            query.Criteria.AddCondition(AttributeNames.PrincipalId, ConditionOperator.Equal, principalId);

            return query;
        }

        /// <summary>The role assignment of a team or user (one row) — otherwise no rows.</summary>
        /// <param name="principal">A team or systemuser.</param>
        /// <param name="roleId">The role, in the principal's business unit.</param>
        public static QueryExpression PrincipalRoleQuery(EntityReference principal, Guid roleId)
        {
            var isTeam = principal.LogicalName == EntityNames.Team;
            var query = new QueryExpression(isTeam ? EntityNames.TeamRoles : EntityNames.SystemUserRoles)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1
            };
            query.Criteria.AddCondition(isTeam ? AttributeNames.TeamId : AttributeNames.SystemUserId, ConditionOperator.Equal, principal.Id);
            query.Criteria.AddCondition(AttributeNames.RoleId, ConditionOperator.Equal, roleId);

            return query;
        }

        /// <summary>The team, if the user is a member of it (one row) — otherwise no rows.</summary>
        public static QueryExpression TeamMembershipQuery(Guid teamId, Guid systemUserId)
        {
            var query = new QueryExpression(EntityNames.Team)
            {
                ColumnSet = new ColumnSet(AttributeNames.TeamId),
                Distinct = true
            };
            query.Criteria.AddCondition(AttributeNames.TeamId, ConditionOperator.Equal, teamId);

            var membership = query.AddLink(EntityNames.TeamMembership, AttributeNames.TeamId, AttributeNames.TeamId);
            var user = membership.AddLink(EntityNames.SystemUser, AttributeNames.SystemUserId, AttributeNames.SystemUserId);
            user.EntityAlias = "ag";
            user.LinkCriteria.AddCondition(AttributeNames.SystemUserId, ConditionOperator.Equal, systemUserId);

            return query;
        }

        /// <summary>
        /// The user's copies of a role (one row per business-unit copy whose root is <paramref name="rootRoleId"/>) —
        /// no rows when the user does not have the role.
        /// </summary>
        public static QueryExpression UserRoleQuery(Guid systemUserId, Guid rootRoleId)
        {
            var query = new QueryExpression(EntityNames.Role)
            {
                ColumnSet = new ColumnSet(AttributeNames.RoleId),
                TopCount = 1
            };
            query.Criteria.AddCondition(AttributeNames.ParentRootRoleId, ConditionOperator.Equal, rootRoleId);

            var userRoles = query.AddLink(EntityNames.SystemUserRoles, AttributeNames.RoleId, AttributeNames.RoleId);
            userRoles.LinkCriteria.AddCondition(AttributeNames.SystemUserId, ConditionOperator.Equal, systemUserId);

            return query;
        }
    }
}
