using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// The object type code (the "etc" in record URLs) of an entity, from its logical name.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">There is no entity with that name.</exception>
        public int GetEntityTypeCode(string entityName)
        {
            var filter = new MetadataFilterExpression(LogicalOperator.And);
            filter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityName));

            var request = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression { Criteria = filter },
                ClientVersionStamp = null
            };

            var response = (RetrieveMetadataChangesResponse)Service.Execute(request);
            var objectTypeCode = response.EntityMetadata.FirstOrDefault()?.ObjectTypeCode;

            return objectTypeCode ?? throw new InvalidPluginExecutionException($"Entity '{entityName}' was not found.");
        }

        /// <summary>
        /// Query the Metadata to get the Entity Schema Name from the Object Type Code
        /// </summary>
        /// <param name="objectTypeCode"></param>
        /// <returns>Entity Schema Name</returns>
        public string GetEntityNameFromCode(string objectTypeCode)
        {
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, Convert.ToInt32(objectTypeCode)));

            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var request = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression,
                ClientVersionStamp = null
            };

            var response = (RetrieveMetadataChangesResponse)Service.Execute(request);
            var entityMetadata = response.EntityMetadata.FirstOrDefault();

            if (entityMetadata == null)
            {
                throw new InvalidPluginExecutionException($"No entity has the object type code '{objectTypeCode}'.");
            }

            return entityMetadata.LogicalName ?? entityMetadata.SchemaName.ToLowerInvariant();
        }

        /// <summary>
        /// Intersect entity of an N:N relationship. A name that is not a relationship is returned unchanged,
        /// so workflows configured with the intersect entity name keep working.
        /// </summary>
        public string GetIntersectEntityName(string relationshipName)
        {
            RetrieveRelationshipResponse response;

            try
            {
                response = (RetrieveRelationshipResponse)Service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                Trace($"Relationship '{relationshipName}' not found; using it as the intersect entity name.");

                return relationshipName;
            }

            if (!(response.RelationshipMetadata is ManyToManyRelationshipMetadata relationship))
            {
                throw new InvalidPluginExecutionException($"Relationship '{relationshipName}' is not Many to Many.");
            }

            return relationship.IntersectEntityName;
        }

        public List<string> GetEntityAttributesToClone(string entityName, ref string primaryIdAttribute, ref string primaryNameAttribute)
        {
            var attributes = new List<string>();
            var request = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = entityName
            };

            var response = (RetrieveEntityResponse)Service.Execute(request);
            primaryIdAttribute = response.EntityMetadata.PrimaryIdAttribute;

            foreach (var attMetadata in response.EntityMetadata.Attributes)
            {
                if (attMetadata.IsPrimaryName != null && attMetadata.IsPrimaryName.Value)
                {
                    primaryNameAttribute = attMetadata.LogicalName;
                }

                if (attMetadata.IsValidForCreate != null &&
                    attMetadata.IsValidForUpdate != null &&
                    attMetadata.IsPrimaryId != null &&
                    ((!attMetadata.IsValidForCreate.Value && 
                      !attMetadata.IsValidForUpdate.Value)
                     || attMetadata.IsPrimaryId.Value))
                {
                    continue;
                }

                attributes.Add(attMetadata.AttributeTypeName.Value.ToLower() == "partylisttype"
                    ? $"partylist-{attMetadata.LogicalName}"
                    : attMetadata.LogicalName);
            }

            return attributes;
        }

        /// <summary>
        /// The value of an option set (choice) field on a record.
        /// </summary>
        /// <returns>The option value, or 0 when the field is empty.</returns>
        public int GetOptionSetValue(EntityReference record, string attributeName)
        {
            var entity = Service.Retrieve(record.LogicalName, record.Id, new ColumnSet(attributeName));
            var value = entity.GetAttributeValue<OptionSetValue>(attributeName);

            Trace($"{record.LogicalName}.{attributeName} = {(value == null ? "(empty)" : value.Value.ToString())}");

            return value?.Value ?? 0;
        }

        /// <summary>
        /// Labels of an option set (choice) or multi-select option set attribute, by value, in the user's language.
        /// </summary>
        public Dictionary<int, string> GetOptionSetLabels(string entityName, string attributeName)
        {
            var response = (RetrieveAttributeResponse)Service.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = false
            });

            if (!(response.AttributeMetadata is EnumAttributeMetadata metadata))
            {
                throw new InvalidPluginExecutionException($"Attribute {attributeName} is not an option set (choice) attribute.");
            }

            return metadata.OptionSet.Options
                .Where(o => o.Value.HasValue)
                .ToDictionary(
                    o => o.Value.Value,
                    o => o.Label?.UserLocalizedLabel?.Label ?? o.Label?.LocalizedLabels.FirstOrDefault()?.Label ?? o.Value.Value.ToString());
        }

        public bool InsertOptionValue(bool globalOptionSet, string attributeName, string entityName, string optionText, int optionValue, int languageCode)
        {
            var request = new InsertOptionValueRequest
            {
                Value = optionValue,
                Label = new Label(optionText, languageCode)
            };

            if (globalOptionSet)
            {
                request.OptionSetName = attributeName;
            }
            else
            {
                request.AttributeLogicalName = attributeName;
                request.EntityLogicalName = entityName;
            }

            Service.Execute(request);

            return true;
        }

        public void DeleteOptionValue(bool globalOptionSet, string attributeName, string entityName, int optionValue)
        {
            var request = new DeleteOptionValueRequest
            {
                Value = optionValue
            };

            if (globalOptionSet)
            {
                request.OptionSetName = attributeName;
            }
            else
            {
                request.AttributeLogicalName = attributeName;
                request.EntityLogicalName = entityName;
            }

            Service.Execute(request);
        }
    }
}
