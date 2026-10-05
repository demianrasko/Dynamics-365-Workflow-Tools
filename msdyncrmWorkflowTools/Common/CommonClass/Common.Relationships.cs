using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public void AssociateEntity(string primaryEntityName, Guid primaryEntityId, string relationshipName, string relationshipEntityName, string entityName, Guid parentId)
        {
            try
            {
                var relations = GetAssociations(primaryEntityName, primaryEntityId, relationshipEntityName, entityName, parentId);

                if (relations.Entities.Count != 0)
                {
                    return;
                }

                var relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(entityName, parentId)
                };

                var relationship = new Relationship(relationshipName);

                if (primaryEntityName == entityName)
                {
                    relationship.PrimaryEntityRole = EntityRole.Referencing;
                }

                Service.Associate(primaryEntityName, primaryEntityId, relationship, relatedEntities);
            }
            catch (Exception ex)
            {
                Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
            }
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
        /// Retrieves related records using a relationship metadata
        /// </summary>
        /// <param name="relationshipName">relationship to navigate</param>
        /// <param name="parentEntityId">Parent Id</param>
        /// <returns></returns>
        public EntityCollection GetChildRecords(string relationshipName, Guid parentEntityId)
        {
            var request = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };

            var response = (RetrieveRelationshipResponse)Service.Execute(request);
            var rel = (OneToManyRelationshipMetadata)response.RelationshipMetadata;
            var childEntityType = rel.ReferencingEntity;
            var childEntityFieldName = rel.ReferencingAttribute;

            var query = new QueryByAttribute(childEntityType)
            {
                ColumnSet = new ColumnSet(childEntityFieldName),
                Attributes = { childEntityFieldName },
                Values = { parentEntityId }
            };

            return Service.RetrieveMultiple(query);
        }

        public void UpdateChildRecords(string relationshipName, string parentEntityType, Guid parentEntityId, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate, bool updateonlyActive)
        {
            //1) Get child lookup field name
            var req = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };

            var res = (RetrieveRelationshipResponse)Service.Execute(req);
            var rel = (OneToManyRelationshipMetadata)res.RelationshipMetadata;
            var childEntityType = rel.ReferencingEntity;
            var childEntityFieldName = rel.ReferencingAttribute;

            //2) retrieve all child records
            var query = new QueryByAttribute(childEntityType)
            {
                ColumnSet = new ColumnSet(childEntityFieldName),
                Attributes = { childEntityFieldName },
                Values = { parentEntityId }
            };

            if (updateonlyActive)
            {
                query.AddAttributeValue(AttributeNames.StateCode, 0);
            }

            var retrieved = Service.RetrieveMultiple(query);

            //2') retrieve parent field value
            object valueToUpdate;

            if (!string.IsNullOrEmpty(parentFieldNameToUpdate))
            {
                var retrievedEntity = Service.Retrieve(parentEntityType, parentEntityId, new ColumnSet(parentFieldNameToUpdate));

                valueToUpdate = retrievedEntity.Attributes.Contains(parentFieldNameToUpdate) ? retrievedEntity.Attributes[parentFieldNameToUpdate] : null;
            }
            else
            {
                valueToUpdate = setValueToUpdate;
            }

            //3) update each child record

            foreach (var child in retrieved.Entities)
            {
                if (childEntityType.ToLower() == "dynamicpropertyinstance")
                {
                    //pending...
                    var request = new UpdateProductPropertiesRequest();
                    // req2.
                    break;
                }

                var attributeRequest = new RetrieveAttributeRequest
                {
                    EntityLogicalName = childEntityType,
                    LogicalName = childFieldNameToUpdate
                };

                var attributeResponse = (RetrieveAttributeResponse)Service.Execute(attributeRequest);

                var metadata = attributeResponse.AttributeMetadata;

                var entity = new Entity(childEntityType)
                {
                    Id = child.Id
                };

                if (metadata.AttributeType != null)
                {
                    switch (metadata.AttributeType.Value.ToString())
                    {
                        case "Boolean":
                        {
                            // valueToUpdate is an object, so compare its text (== "1" compared references and missed "1" read from a field)
                            var text = Convert.ToString(valueToUpdate, CultureInfo.InvariantCulture);
                            var isTrue = valueToUpdate is bool flag
                                ? flag
                                : text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);

                            entity.Attributes.Add(childFieldNameToUpdate, isTrue);
                            break;
                        }
                        case "Picklist":
                        case "Status":
                        {
                            if (valueToUpdate == null)
                            {
                                entity.Attributes.Add(childFieldNameToUpdate, null);
                            }
                            else
                            {
                                if (valueToUpdate is OptionSetValue value)
                                {
                                    valueToUpdate = value.Value;
                                }

                                var opt = new OptionSetValue(Convert.ToInt32(valueToUpdate));
                                entity.Attributes.Add(childFieldNameToUpdate, opt);
                            }

                            break;
                        }
                        default:
                        {
                            entity.Attributes.Add(childFieldNameToUpdate, valueToUpdate);
                            break;
                        }
                    }
                }

                Service.Update(entity);
            }
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
        /// <param name="filterXml">A FetchXML &lt;filter&gt; fragment for the child, or empty for none.</param>
        public int CountChildRecords(string childEntityName, string parentLookupName, Guid parentId, string filterXml)
        {
            var query = string.IsNullOrWhiteSpace(filterXml)
                ? ChildRecordsQuery(childEntityName, parentLookupName, parentId)
                : FetchXmlToQueryExpression(ChildRecordsFetchXmlQuery(childEntityName, parentLookupName, parentId, filterXml));

            return CountRecords(query);
        }
    }
}
