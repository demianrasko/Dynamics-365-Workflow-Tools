using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// The QueryExpressions used by Common and the activities. Values are passed as typed condition values,
    /// so nothing needs escaping, and each query can be unit tested without a Dataverse connection.
    /// </summary>
    public static class Queries
    {
        /// <summary>
        /// Records of <paramref name="primaryEntityName"/> with id <paramref name="primaryEntityId"/> that are associated
        /// with record <paramref name="relatedId"/> of <paramref name="relatedEntityName"/> through the N:N intersect entity.
        /// </summary>
        public static QueryExpression Associations(string primaryEntityName, Guid primaryEntityId, string intersectEntityName, string relatedEntityName, Guid relatedId)
        {
            var primaryKey = $"{primaryEntityName}id";
            var relatedKey = $"{relatedEntityName}id";

            var query = new QueryExpression(primaryEntityName)
            {
                ColumnSet = new ColumnSet(false),
                Distinct = true
            };

            var intersect = query.AddLink(intersectEntityName, primaryKey, primaryKey);
            intersect.LinkCriteria.AddCondition(primaryKey, ConditionOperator.Equal, primaryEntityId);

            var related = intersect.AddLink(relatedEntityName, relatedKey, relatedKey);
            related.EntityAlias = "ac";
            related.LinkCriteria.AddCondition(relatedKey, ConditionOperator.Equal, relatedId);

            return query;
        }

        /// <summary>The parties of an activity with one participation type (from, to, cc, ...).</summary>
        public static QueryExpression ActivityParties(Guid activityId, int participationTypeMask)
        {
            var query = new QueryExpression(EntityNames.ActivityParty)
            {
                ColumnSet = new ColumnSet(AttributeNames.PartyId),
                Distinct = true
            };
            query.Criteria.AddCondition(AttributeNames.ActivityId, ConditionOperator.Equal, activityId);
            query.Criteria.AddCondition(AttributeNames.ParticipationTypeMask, ConditionOperator.Equal, participationTypeMask);

            return query;
        }

        /// <summary>The default team of the business unit a user belongs to.</summary>
        public static QueryExpression DefaultTeamForUser(Guid systemUserId)
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

        /// <summary>Enabled, read-write users that hold a security role.</summary>
        public static QueryExpression UsersInRole(Guid roleId)
        {
            var query = new QueryExpression(EntityNames.SystemUser)
            {
                ColumnSet = new ColumnSet(AttributeNames.SystemUserId),
                Distinct = true
            };
            query.Criteria.AddCondition(AttributeNames.AccessMode, ConditionOperator.Equal, 0);

            var userRoles = query.AddLink(EntityNames.SystemUserRoles, AttributeNames.SystemUserId, AttributeNames.SystemUserId);
            var role = userRoles.AddLink(EntityNames.Role, AttributeNames.RoleId, AttributeNames.RoleId);
            role.EntityAlias = "aa";
            role.LinkCriteria.AddCondition(AttributeNames.RoleId, ConditionOperator.Equal, roleId);

            return query;
        }

        /// <summary>Sales literature items whose file name matches a LIKE pattern (% and _ wildcards).</summary>
        public static QueryExpression SalesLiteratureItems(string fileNamePattern, Guid salesLiteratureId)
        {
            var query = new QueryExpression(EntityNames.SalesLiteratureItem)
            {
                ColumnSet = new ColumnSet(AttributeNames.FileName, AttributeNames.SalesLiteratureItemId, AttributeNames.Title, AttributeNames.DocumentBody, AttributeNames.MimeType)
            };
            query.Criteria.AddCondition(AttributeNames.FileName, ConditionOperator.Like, fileNamePattern);
            query.Criteria.AddCondition(AttributeNames.SalesLiteratureId, ConditionOperator.Equal, salesLiteratureId);

            return query;
        }

        /// <summary>
        /// File attachments of a record: notes with a document (newest first), or the attachments of an email/activity.
        /// </summary>
        /// <param name="activityMimeAttachments">True for activitymimeattachment (email attachments), false for annotation (notes).</param>
        /// <param name="fileNamePattern">Optional LIKE pattern for the file name; null or empty means any file.</param>
        /// <param name="parentId">The record the notes belong to, or the activity the attachments belong to.</param>
        /// <param name="top">Maximum number of records; 0 or less means no limit.</param>
        public static QueryExpression EntityAttachments(bool activityMimeAttachments, string fileNamePattern, Guid parentId, int top)
        {
            QueryExpression query;

            if (activityMimeAttachments)
            {
                query = new QueryExpression(EntityNames.ActivityMimeAttachment)
                {
                    ColumnSet = new ColumnSet(AttributeNames.FileName, AttributeNames.AttachmentId, AttributeNames.Subject, AttributeNames.Body, AttributeNames.MimeType)
                };
                query.Criteria.AddCondition(AttributeNames.ActivityId, ConditionOperator.Equal, parentId);
            }
            else
            {
                query = new QueryExpression(EntityNames.Annotation)
                {
                    ColumnSet = new ColumnSet(AttributeNames.FileName, AttributeNames.AnnotationId, AttributeNames.Subject, AttributeNames.DocumentBody, AttributeNames.MimeType)
                };
                query.AddOrder(AttributeNames.CreatedOn, OrderType.Descending);
                query.Criteria.AddCondition(AttributeNames.IsDocument, ConditionOperator.Equal, true);
                query.Criteria.AddCondition(AttributeNames.ObjectId, ConditionOperator.Equal, parentId);
            }

            if (!string.IsNullOrEmpty(fileNamePattern))
            {
                query.Criteria.AddCondition(AttributeNames.FileName, ConditionOperator.Like, fileNamePattern);
            }

            if (top > 0)
            {
                query.TopCount = top;
            }

            return query;
        }

        /// <summary>The team, if the user is a member of it (one row) — otherwise no rows.</summary>
        public static QueryExpression TeamMembership(Guid teamId, Guid systemUserId)
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

        /// <summary>Every marketing list membership of a record (account, contact or lead).</summary>
        public static QueryExpression MarketingListMemberships(Guid memberId)
        {
            var query = new QueryExpression(EntityNames.ListMember)
            {
                ColumnSet = new ColumnSet(AttributeNames.ListId)
            };
            query.Criteria.AddCondition(AttributeNames.EntityId, ConditionOperator.Equal, memberId);

            return query;
        }

        /// <summary>The marketing list membership of a record (one row) — otherwise no rows.</summary>
        public static QueryExpression MarketingListMembership(Guid listId, Guid memberId)
        {
            var query = new QueryExpression(EntityNames.ListMember)
            {
                ColumnSet = new ColumnSet(AttributeNames.ListMemberId),
                TopCount = 1
            };
            query.Criteria.AddCondition(AttributeNames.ListId, ConditionOperator.Equal, listId);
            query.Criteria.AddCondition(AttributeNames.EntityId, ConditionOperator.Equal, memberId);

            return query;
        }

        /// <summary>
        /// The first record of <paramref name="entityName"/> where every filter attribute equals its value.
        /// Empty column names and filters with an empty attribute name are skipped.
        /// </summary>
        public static QueryExpression FirstMatch(string entityName, IEnumerable<string> columns, params KeyValuePair<string, object>[] equalFilters)
        {
            var query = new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet(columns.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray()),
                TopCount = 1
            };

            foreach (var filter in equalFilters.Where(f => !string.IsNullOrEmpty(f.Key)))
            {
                query.Criteria.AddCondition(filter.Key, ConditionOperator.Equal, filter.Value);
            }

            return query;
        }

        /// <summary>The field-sharing record (principalobjectattributeaccess) for one secured field, record and principal.</summary>
        public static QueryExpression FieldSharing(Guid attributeId, Guid objectId, Guid principalId)
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

        /// <summary>The stage of a business process flow with the given name.</summary>
        public static QueryExpression ProcessStage(Guid processId, string stageName)
        {
            var query = new QueryExpression(EntityNames.ProcessStage)
            {
                ColumnSet = new ColumnSet(AttributeNames.ProcessStageId),
                TopCount = 1
            };
            query.Criteria.AddCondition(AttributeNames.ProcessId, ConditionOperator.Equal, processId);
            query.Criteria.AddCondition(AttributeNames.StageName, ConditionOperator.Equal, stageName);

            return query;
        }

        /// <summary>The role assignment of a team or user (one row) — otherwise no rows.</summary>
        /// <param name="principal">A team or systemuser.</param>
        /// <param name="roleId">The role, in the principal's business unit.</param>
        public static QueryExpression PrincipalRole(EntityReference principal, Guid roleId)
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

        /// <summary>
        /// The user's copies of a role (one row per business-unit copy whose root is <paramref name="rootRoleId"/>) —
        /// no rows when the user does not have the role.
        /// </summary>
        public static QueryExpression UserRole(Guid systemUserId, Guid rootRoleId)
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

        /// <summary>The users who are members of a team.</summary>
        public static QueryExpression TeamMembers(Guid teamId)
        {
            var query = new QueryExpression(EntityNames.SystemUser)
            {
                ColumnSet = new ColumnSet(AttributeNames.SystemUserId)
            };

            var membership = query.AddLink(EntityNames.TeamMembership, AttributeNames.SystemUserId, AttributeNames.SystemUserId);
            membership.LinkCriteria.AddCondition(AttributeNames.TeamId, ConditionOperator.Equal, teamId);

            return query;
        }

        /// <summary>Active queue items in a queue, newest first; optionally only those not assigned to a worker.</summary>
        /// <param name="top">Maximum number of records; 0 or less means no limit.</param>
        public static QueryExpression QueueItems(Guid queueId, bool onlyUnassigned, int top = 0)
        {
            var query = new QueryExpression(EntityNames.QueueItem)
            {
                ColumnSet = new ColumnSet(AttributeNames.EnteredOn, AttributeNames.ObjectTypeCode, AttributeNames.ObjectId, AttributeNames.QueueId)
            };
            query.AddOrder(AttributeNames.EnteredOn, OrderType.Descending);
            query.Criteria.AddCondition(AttributeNames.StateCode, ConditionOperator.Equal, 0);

            if (onlyUnassigned)
            {
                query.Criteria.AddCondition(AttributeNames.WorkerId, ConditionOperator.Null);
            }

            query.Criteria.AddCondition(AttributeNames.QueueId, ConditionOperator.Equal, queueId);

            if (top > 0)
            {
                query.TopCount = top;
            }

            return query;
        }

        /// <summary>Records of a child entity whose lookup points at a parent record.</summary>
        public static QueryExpression ChildRecords(string childEntityName, string parentLookupName, Guid parentId)
        {
            var query = new QueryExpression(childEntityName)
            {
                ColumnSet = new ColumnSet(false)
            };
            query.Criteria.AddCondition(parentLookupName, ConditionOperator.Equal, parentId);

            return query;
        }

        /// <summary>One column of the organization record.</summary>
        public static QueryExpression OrganizationSetting(string attributeName)
        {
            var query = new QueryExpression(EntityNames.Organization)
            {
                ColumnSet = new ColumnSet(attributeName),
                TopCount = 1
            };
            query.AddOrder(AttributeNames.Name, OrderType.Ascending);

            return query;
        }

        /// <summary>
        /// SharePoint document locations whose regarding record is <paramref name="regardingObjectId"/>.
        /// </summary>
        public static QueryExpression SharepointDocumentLocations(Guid regardingObjectId)
        {
            var query = new QueryExpression(EntityNames.SharePointDocumentLocation)
            {
                ColumnSet = new ColumnSet(AttributeNames.AbsoluteUrl, AttributeNames.SharePointDocumentLocationId, AttributeNames.RelativeUrl)
            };

            query.Criteria.AddCondition(AttributeNames.RegardingObjectId, ConditionOperator.Equal, regardingObjectId);

            return query;
        }

        /// <summary>
        /// Records of <paramref name="relatedEntityName"/> associated with a record through an N:N intersect entity.
        /// The record is matched on the intersect entity itself, which also works for self-referencing relationships.
        /// </summary>
        /// <param name="relatedEntityName">Entity to return.</param>
        /// <param name="relatedPrimaryKey">Primary key of <paramref name="relatedEntityName"/>.</param>
        /// <param name="relatedIntersectAttribute">Intersect attribute that holds the related record's id.</param>
        /// <param name="intersectEntityName">The N:N intersect entity.</param>
        /// <param name="primaryIntersectAttribute">Intersect attribute that holds the primary record's id.</param>
        /// <param name="primaryId">Id of the primary record.</param>
        public static QueryExpression ManyToManyRelated(string relatedEntityName, string relatedPrimaryKey, string relatedIntersectAttribute, string intersectEntityName, string primaryIntersectAttribute, Guid primaryId)
        {
            var query = new QueryExpression(relatedEntityName)
            {
                ColumnSet = new ColumnSet(false)
            };

            var intersect = query.AddLink(intersectEntityName, relatedPrimaryKey, relatedIntersectAttribute);
            intersect.LinkCriteria.AddCondition(primaryIntersectAttribute, ConditionOperator.Equal, primaryId);

            return query;
        }

        /// <summary>
        /// FetchXML for the child records of a parent, with an optional extra FetchXML filter fragment supplied by the
        /// user (conditions and/or filter elements). Values are written by XElement, so they are escaped.
        /// </summary>
        public static string ChildRecordsFetchXml(string childEntityName, string parentLookupName, Guid parentId, string filterFragment)
        {
            var filter = new System.Xml.Linq.XElement("filter",
                new System.Xml.Linq.XAttribute("type", "and"),
                new System.Xml.Linq.XElement("condition",
                    new System.Xml.Linq.XAttribute("attribute", parentLookupName),
                    new System.Xml.Linq.XAttribute("operator", "eq"),
                    new System.Xml.Linq.XAttribute("value", parentId)));

            if (!string.IsNullOrWhiteSpace(filterFragment))
            {
                filter.Add(System.Xml.Linq.XElement.Parse($"<x>{filterFragment}</x>").Elements());
            }

            var fetch = new System.Xml.Linq.XElement("fetch",
                new System.Xml.Linq.XAttribute("mapping", "logical"),
                new System.Xml.Linq.XElement("entity",
                    new System.Xml.Linq.XAttribute("name", childEntityName),
                    filter));

            return fetch.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
        }
    }
}
