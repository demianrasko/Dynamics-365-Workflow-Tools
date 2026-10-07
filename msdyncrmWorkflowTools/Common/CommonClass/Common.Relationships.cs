using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// Returns the primary record if it is associated with the given record through an N:N relationship.
        /// </summary>
        /// <param name="primaryEntityName">Logical name of the primary record.</param>
        /// <param name="primaryEntityId">Id of the primary record.</param>
        /// <param name="intersectEntityName">Name of the N:N intersect entity.</param>
        /// <param name="entityName">Logical name of the related record.</param>
        /// <param name="parentId">Id of the related record.</param>
        public EntityCollection GetAssociations(string primaryEntityName, Guid primaryEntityId, string intersectEntityName, string entityName, Guid parentId)
        {
            Trace($"Associations: {primaryEntityName} {primaryEntityId} via {intersectEntityName} to {entityName} {parentId}");

            return Service.RetrieveMultiple(AssociationsQuery(primaryEntityName, primaryEntityId, intersectEntityName, entityName, parentId));
        }

        /// <summary>
        /// Removes the N:N association between two records.
        /// </summary>
        public void DisassociateEntity(EntityReference record, string relationshipName, EntityReference related)
        {
            Trace($"Disassociating {record.LogicalName} {record.Id} and {related.LogicalName} {related.Id} ({relationshipName})");
            Service.Disassociate(record.LogicalName, record.Id, new Relationship(relationshipName), new EntityReferenceCollection { related });
        }

        /// <summary>
        /// Associates two records through an N:N relationship, unless they are already associated. Errors (an unknown
        /// relationship, a missing record, no permission) reach the caller instead of being traced and ignored.
        /// </summary>
        /// <param name="primaryEntityName">Logical name of the first record.</param>
        /// <param name="primaryEntityId">Id of the first record.</param>
        /// <param name="relationshipName">Schema name of the N:N relationship.</param>
        /// <param name="relationshipEntityName">The relationship's intersect entity.</param>
        /// <param name="entityName">Logical name of the second record.</param>
        /// <param name="parentId">Id of the second record.</param>
        public void AssociateEntity(string primaryEntityName, Guid primaryEntityId, string relationshipName, string relationshipEntityName, string entityName, Guid parentId)
        {
            if (GetAssociations(primaryEntityName, primaryEntityId, relationshipEntityName, entityName, parentId).Entities.Count != 0)
            {
                Trace($"{primaryEntityName} {primaryEntityId} and {entityName} {parentId} are already associated.");

                return;
            }

            var relationship = new Relationship(relationshipName);

            if (primaryEntityName == entityName)
            {
                relationship.PrimaryEntityRole = EntityRole.Referencing;
            }

            Service.Associate(primaryEntityName, primaryEntityId, relationship, new EntityReferenceCollection { new EntityReference(entityName, parentId) });
        }

        /// <summary>
        /// Ids of the records on the "many" side of a 1:N relationship whose lookup points at <paramref name="parentId"/>.
        /// </summary>
        public List<Guid> GetOneToManyRelatedIds(string relationshipName, Guid parentId)
        {
            var response = (RetrieveRelationshipResponse)Service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });

            if (!(response.RelationshipMetadata is OneToManyRelationshipMetadata relationship))
            {
                throw new InvalidPluginExecutionException($"Relationship '{relationshipName}' is not One to Many.");
            }

            return RetrieveAllIds(ChildRecordsQuery(relationship.ReferencingEntity, relationship.ReferencingAttribute, parentId));
        }

        /// <summary>
        /// Ids of the records associated with a record through an N:N relationship. For a self-referencing
        /// relationship both directions are included and the record itself is left out.
        /// </summary>
        public List<Guid> GetManyToManyRelatedIds(string relationshipName, string primaryEntityName, Guid primaryEntityId)
        {
            var response = (RetrieveRelationshipResponse)Service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });

            if (!(response.RelationshipMetadata is ManyToManyRelationshipMetadata relationship))
            {
                throw new InvalidPluginExecutionException($"Relationship '{relationshipName}' is not Many to Many.");
            }

            var intersect = relationship.IntersectEntityName;

            if (relationship.Entity1LogicalName == primaryEntityName && relationship.Entity2LogicalName == primaryEntityName)
            {
                var ids = new HashSet<Guid>(RetrieveAllIds(ManyToManyRelatedQuery(primaryEntityName, $"{primaryEntityName}id", relationship.Entity2IntersectAttribute, intersect, relationship.Entity1IntersectAttribute, primaryEntityId)));
                ids.UnionWith(RetrieveAllIds(ManyToManyRelatedQuery(primaryEntityName, $"{primaryEntityName}id", relationship.Entity1IntersectAttribute, intersect, relationship.Entity2IntersectAttribute, primaryEntityId)));
                ids.Remove(primaryEntityId);

                return ids.ToList();
            }

            return RetrieveAllIds(relationship.Entity1LogicalName == primaryEntityName 
                ? ManyToManyRelatedQuery(relationship.Entity2LogicalName, relationship.Entity2IntersectAttribute, relationship.Entity2IntersectAttribute, intersect, relationship.Entity1IntersectAttribute, primaryEntityId) 
                : ManyToManyRelatedQuery(relationship.Entity1LogicalName, relationship.Entity1IntersectAttribute, relationship.Entity1IntersectAttribute, intersect, relationship.Entity2IntersectAttribute, primaryEntityId));
        }

        /// <summary>
        /// The child records of a parent through a 1:N relationship, with only their lookup to the parent.
        /// </summary>
        /// <param name="relationshipName">Schema name of the 1:N relationship.</param>
        /// <param name="parentEntityId">Id of the parent record.</param>
        public EntityCollection GetChildRecords(string relationshipName, Guid parentEntityId)
        {
            var relationship = GetOneToManyRelationship(relationshipName);

            var query = new QueryByAttribute(relationship.ReferencingEntity)
            {
                ColumnSet = new ColumnSet(relationship.ReferencingAttribute),
                Attributes = { relationship.ReferencingAttribute },
                Values = { parentEntityId }
            };

            return Service.RetrieveMultiple(query);
        }

        /// <summary>
        /// Sets a field on every child record of a parent, through a 1:N relationship. The value is either copied from
        /// a field of the parent or given as text, and is converted to the child field's type
        /// (see <see cref="Utility.ConvertToAttributeType"/>). Every child is updated, however many there are.
        /// </summary>
        /// <param name="relationshipName">Schema name of the 1:N relationship from the parent to the children.</param>
        /// <param name="parentEntityType">Logical name of the parent record.</param>
        /// <param name="parentEntityId">Id of the parent record.</param>
        /// <param name="parentFieldNameToUpdate">Parent field to copy, or empty to use <paramref name="setValueToUpdate"/>.</param>
        /// <param name="setValueToUpdate">The value as text, used when no parent field is given.</param>
        /// <param name="childFieldNameToUpdate">The child field to set.</param>
        /// <param name="updateonlyActive">Only update active children (statecode 0).</param>
        /// <returns>The number of child records updated.</returns>
        /// <exception cref="InvalidPluginExecutionException">The children are product properties (dynamicpropertyinstance),
        /// which can't be updated this way, or the value can't be stored in the child field.</exception>
        public int UpdateChildRecords(string relationshipName, string parentEntityType, Guid parentEntityId, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate, bool updateonlyActive)
        {
            var relationship = GetOneToManyRelationship(relationshipName);
            var childEntityType = relationship.ReferencingEntity;

            if (string.Equals(childEntityType, EntityNames.DynamicPropertyInstance, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidPluginExecutionException(
                    $"Update Child Records can't update product properties ({relationshipName}). Use the product's property editor instead.");
            }

            var query = ChildRecordsQuery(childEntityType, relationship.ReferencingAttribute, parentEntityId);

            if (updateonlyActive)
            {
                query.Criteria.AddCondition(AttributeNames.StateCode, ConditionOperator.Equal, 0);
            }

            var childIds = RetrieveAllIds(query);

            var value = string.IsNullOrEmpty(parentFieldNameToUpdate)
                ? setValueToUpdate
                : Service.Retrieve(parentEntityType, parentEntityId, new ColumnSet(parentFieldNameToUpdate)).GetAttributeValue<object>(parentFieldNameToUpdate);

            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = childEntityType,
                LogicalName = childFieldNameToUpdate
            };

            var response = (RetrieveAttributeResponse)Service.Execute(request);
            var convertedValue = Utility.ConvertToAttributeType(value, response.AttributeMetadata);

            foreach (var childId in childIds)
            {
                Service.Update(new Entity(childEntityType, childId)
                {
                    [childFieldNameToUpdate] = convertedValue
                });
            }

            Trace($"Set {childFieldNameToUpdate} on {childIds.Count} {childEntityType} record(s).");

            return childIds.Count;
        }

        /// <summary>
        /// The metadata of a 1:N relationship: its child table (ReferencingEntity) and lookup (ReferencingAttribute).
        /// </summary>
        private OneToManyRelationshipMetadata GetOneToManyRelationship(string relationshipName)
        {
            var request = new RetrieveRelationshipRequest
            {
                Name = relationshipName
            };

            var response = (RetrieveRelationshipResponse)Service.Execute(request);

            return response.RelationshipMetadata as OneToManyRelationshipMetadata
                ?? throw new InvalidPluginExecutionException($"'{relationshipName}' is not a one-to-many relationship.");
        }

        /// <summary>
        /// Records of <paramref name="primaryEntityName"/> with id <paramref name="primaryEntityId"/> that are associated
        /// with record <paramref name="relatedId"/> of <paramref name="relatedEntityName"/> through the N:N intersect entity.
        /// </summary>
        public static QueryExpression AssociationsQuery(string primaryEntityName, Guid primaryEntityId, string intersectEntityName, string relatedEntityName, Guid relatedId)
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

        /// <summary>Records of a child entity whose lookup points at a parent record.</summary>
        public static QueryExpression ChildRecordsQuery(string childEntityName, string parentLookupName, Guid parentId)
        {
            var query = new QueryExpression(childEntityName)
            {
                ColumnSet = new ColumnSet(false)
            };
            query.Criteria.AddCondition(parentLookupName, ConditionOperator.Equal, parentId);

            return query;
        }

        /// <summary>
        /// FetchXML for the child records of a parent, with an optional extra FetchXML filter fragment supplied by the
        /// user (conditions and/or filter elements). Values are written by XElement, so they are escaped.
        /// </summary>
        public static string ChildRecordsFetchXmlQuery(string childEntityName, string parentLookupName, Guid parentId, string filterFragment)
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
        public static QueryExpression ManyToManyRelatedQuery(string relatedEntityName, string relatedPrimaryKey, string relatedIntersectAttribute, string intersectEntityName, string primaryIntersectAttribute, Guid primaryId)
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
        /// The number of <paramref name="childEntityName"/> records whose <paramref name="parentLookupName"/> is
        /// <paramref name="parentId"/>, optionally narrowed by a FetchXML filter on the child.
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="filterXml">A FetchXML &lt;filter&gt; fragment for the child, or empty for none.</param>
        /// <param name="childEntityName"></param>
        /// <param name="parentLookupName"></param>
        public int CountChildRecords(string childEntityName, string parentLookupName, Guid parentId, string filterXml)
        {
            var query = string.IsNullOrWhiteSpace(filterXml)
                ? ChildRecordsQuery(childEntityName, parentLookupName, parentId)
                : FetchXmlToQueryExpression(ChildRecordsFetchXmlQuery(childEntityName, parentLookupName, parentId, filterXml));

            return CountRecords(query);
        }
    }
}
