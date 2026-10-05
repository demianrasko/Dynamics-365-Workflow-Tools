using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// Sets a record's status (statecode) and status reason (statuscode).
        /// </summary>
        public void SetState(EntityReference record, int state, int status)
        {
            Trace($"Setting {record.LogicalName} {record.Id} to state {state}, status {status}");

            var request = new SetStateRequest
            {
                EntityMoniker = record,
                State = new OptionSetValue(state),
                Status = new OptionSetValue(status)
            };

            Service.Execute(request);
        }

        /// <summary>
        /// Sets a lookup field on a record.
        /// </summary>
        public void SetLookup(EntityReference record, string lookupFieldName, EntityReference value)
        {
            Trace($"{record.LogicalName} {record.Id}: {lookupFieldName} = {value.LogicalName} {value.Id}");

            Service.Update(new Entity(record.LogicalName, record.Id)
            {
                [lookupFieldName] = value
            });
        }

        /// <summary>
        /// Sets a currency (Money) field on a record.
        /// </summary>
        public void SetMoney(EntityReference record, string fieldName, decimal amount)
        {
            Trace($"{record.LogicalName} {record.Id}: {fieldName} = {amount}");

            Service.Update(new Entity(record.LogicalName, record.Id)
            {
                [fieldName] = new Money(amount)
            });
        }

        /// <summary>
        /// Recalculates a rollup field now instead of waiting for the scheduled job.
        /// </summary>
        public void CalculateRollupField(EntityReference record, string fieldName)
        {
            // a stray space in the field name makes Dataverse report that the column doesn't exist
            fieldName = fieldName?.Trim();
            Trace($"Calculating rollup {fieldName} on {record.LogicalName} {record.Id}");
            Service.Execute(new CalculateRollupFieldRequest { Target = record, FieldName = fieldName });
        }

        /// <summary>
        /// A record as JSON (see <see cref="Utility.SerializeEntity"/>), with every attribute that can be cloned.
        /// </summary>
        public string SerializeRecord(EntityReference record)
        {
            var entity = Service.Retrieve(record.LogicalName, record.Id, new ColumnSet(allColumns: true));
            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;
            var attributes = GetEntityAttributesToClone(record.LogicalName, ref primaryIdAttribute, ref primaryNameAttribute);

            return Utility.SerializeEntity(record.LogicalName, primaryIdAttribute, record.Id, entity, attributes);
        }

        /// <summary>
        /// Deletes a record.
        /// </summary>
        public void DeleteRecord(EntityReference record)
        {
            Trace($"Deleting {record.LogicalName} {record.Id}");
            Service.Delete(record.LogicalName, record.Id);
        }

        /// <summary>
        /// Creates a copy of a record, copying every attribute that can be set on create.
        /// </summary>
        /// <param name="entityName">Logical name of the record.</param>
        /// <param name="objectId">The record to copy.</param>
        /// <param name="fieldstoIgnore">Attributes not to copy, separated by ";" or ",".</param>
        /// <param name="prefix">Text put in front of the copy's primary name; null for none.</param>
        /// <param name="fieldsToReplace">Attribute values to set on the copy instead of the copied ones (null removes
        /// the value), e.g. the new parent lookup for CloneChildren. They are part of the create, so the copy never
        /// points at the original parent.</param>
        /// <returns>The id of the copy.</returns>
        public Guid CloneRecord(string entityName, Guid objectId, string fieldstoIgnore, string prefix, IDictionary<string, object> fieldsToReplace = null)
        {
            Trace("entering CloneRecord");
            if (fieldstoIgnore == null)
            {
                fieldstoIgnore = string.Empty;
            }

            fieldstoIgnore = fieldstoIgnore.ToLower();
            Trace($"{nameof(fieldstoIgnore)}={fieldstoIgnore}");

            var retrievedObject = Service.Retrieve(entityName, objectId, new ColumnSet(allColumns: true));
            Trace("retrieved object OK");

            var newEntity = new Entity(entityName);
            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;

            var attributesToClone = GetEntityAttributesToClone(entityName, ref primaryIdAttribute, ref primaryNameAttribute);

            foreach (var attribute in attributesToClone)
            {
                if (!string.IsNullOrEmpty(fieldstoIgnore))
                {
                    if (Array.IndexOf(fieldstoIgnore.Split(';'), attribute) >= 0 || Array.IndexOf(fieldstoIgnore.Split(','), attribute) >= 0)
                    {
                        continue;
                    }
                }

                if ((!retrievedObject.Attributes.Contains(attribute) || attribute == AttributeNames.StatusCode || attribute == AttributeNames.StateCode)
                    && !attribute.StartsWith("partylist-"))
                {
                    continue;
                }

                var newPartyList = new EntityCollection();

                if (attribute.StartsWith("partylist-"))
                {
                    var attribute2 = attribute.Replace("partylist-", string.Empty);

                    var participationTypeMask = Utility.GetParticipation(attribute2);
                    if (string.IsNullOrEmpty(participationTypeMask))
                    {
                        throw new InvalidPluginExecutionException($"Unsupported party list attribute '{attribute2}'.");
                    }

                    var returnCollection = Service.RetrieveMultiple(
                        ActivityPartiesQuery(objectId, int.Parse(participationTypeMask)));

                    Trace($"attribute:{attribute2}");

                    foreach (var ent in returnCollection.Entities)
                    {
                        var partyid = (EntityReference)ent.Attributes[AttributeNames.PartyId];

                        // one activityparty per party (re-using one entity threw "same key" for a second party)
                        var party = new Entity(EntityNames.ActivityParty)
                        {
                            [AttributeNames.PartyId] = new EntityReference(partyid.LogicalName, partyid.Id)
                        };

                        Trace($"attribute:{attribute2}:{partyid.LogicalName}:{partyid.Id}");

                        newPartyList.Entities.Add(party);
                    }

                    newEntity.Attributes.Add(attribute2, newPartyList);
                    continue;
                }

                Trace($"attribute:{attribute}");

                if (attribute == primaryNameAttribute && prefix != null)
                {
                    retrievedObject.Attributes[attribute] = prefix + retrievedObject.Attributes[attribute];
                }

                newEntity.Attributes.Add(attribute, retrievedObject.Attributes[attribute]);
            }

            if (fieldsToReplace != null)
            {
                foreach (var field in fieldsToReplace)
                {
                    Trace($"replacing attribute: {field.Key}");
                    newEntity[field.Key] = field.Value;
                }
            }

            Trace("creating cloned object...");
            var id = Service.Create(newEntity);
            Trace("created cloned object OK");

            if (newEntity.Attributes.Contains(AttributeNames.StatusCode) && newEntity.Attributes.Contains(AttributeNames.StateCode))
            {
                var record = Service.Retrieve(entityName, id, new ColumnSet(AttributeNames.StatusCode, AttributeNames.StateCode));

                if (retrievedObject.Attributes[AttributeNames.StatusCode] != record.Attributes[AttributeNames.StatusCode] ||
                    retrievedObject.Attributes[AttributeNames.StateCode] != record.Attributes[AttributeNames.StateCode])
                {
                    var setStatusEnt = new Entity(entityName, id);
                    setStatusEnt.Attributes.Add(AttributeNames.StatusCode, retrievedObject.Attributes[AttributeNames.StatusCode]);
                    setStatusEnt.Attributes.Add(AttributeNames.StateCode, retrievedObject.Attributes[AttributeNames.StateCode]);

                    Service.Update(setStatusEnt);
                }
            }

            Trace("cloned object OK");

            return id;
        }

        public void DeleteRecordAuditHistory(string logicalName, Guid id)
        {
            var request = new DeleteRecordChangeHistoryRequest();

            var entityReference = new EntityReference(logicalName, id);

            request.Target = entityReference;
            Service.Execute(request);
        }

        /// <summary>
        /// The selected values of a multi-select option set field on a record; empty when none are selected.
        /// </summary>
        public OptionSetValueCollection GetMultiSelectOptionSet(EntityReference record, string attributeName)
        {
            var entity = Service.Retrieve(record.LogicalName, record.Id, new ColumnSet(attributeName));

            return entity.GetAttributeValue<OptionSetValueCollection>(attributeName) ?? new OptionSetValueCollection();
        }

        /// <summary>
        /// The labels of the given option set values, comma separated, in the user's language (the number when an
        /// option has no label).
        /// </summary>
        public string GetOptionSetNames(string entityName, string attributeName, IEnumerable<OptionSetValue> values)
        {
            return Utility.JoinOptionSetLabels(values, GetOptionSetLabels(entityName, attributeName));
        }

        /// <summary>
        /// Sets a multi-select option set field on a record, optionally keeping the values it already has.
        /// </summary>
        /// <param name="target">The record to update.</param>
        /// <param name="attributeName">Logical name of the multi-select option set field.</param>
        /// <param name="values">The values to set; an empty collection clears the field unless existing values are kept.</param>
        /// <param name="keepExistingValues">Add <paramref name="values"/> to the current values instead of replacing them.</param>
        public void SetMultiSelectOptionSet(EntityReference target, string attributeName, OptionSetValueCollection values, bool keepExistingValues)
        {
            SetMultiSelectOptionSets(target, new Dictionary<string, OptionSetValueCollection> { [attributeName] = values }, keepExistingValues);
        }

        /// <summary>
        /// Sets several multi-select option set fields on a record in one update, optionally keeping the values
        /// they already have (read in one retrieve).
        /// </summary>
        /// <param name="target">The record to update.</param>
        /// <param name="values">The values to set, by field logical name.</param>
        /// <param name="keepExistingValues">Add the values to the current values instead of replacing them.</param>
        public void SetMultiSelectOptionSets(EntityReference target, IDictionary<string, OptionSetValueCollection> values, bool keepExistingValues)
        {
            if (values.Count == 0)
            {
                Trace("No multi-select option set values to set.");
                return;
            }

            var existing = keepExistingValues
                ? Service.Retrieve(target.LogicalName, target.Id, new ColumnSet(values.Keys.ToArray()))
                : null;

            var update = new Entity(target.LogicalName, target.Id);

            foreach (var field in values)
            {
                update[field.Key] = existing == null
                    ? field.Value
                    : Utility.MergeOptionSetValues(field.Value, existing.GetAttributeValue<OptionSetValueCollection>(field.Key));

                Trace($"Multi-select option set '{field.Key}' on {target.LogicalName} {target.Id}: {((OptionSetValueCollection)update[field.Key]).Count} value(s).");
            }

            Service.Update(update);
        }

        /// <summary>
        /// Copies multi-select option set fields from one record to another: the n-th source field to the n-th
        /// target field. Source fields that are empty or are not multi-select option sets are skipped.
        /// </summary>
        /// <param name="source">The record to copy from.</param>
        /// <param name="sourceAttributes">Source field logical names.</param>
        /// <param name="target">The record to copy to.</param>
        /// <param name="targetAttributes">Target field logical names, in the same order as <paramref name="sourceAttributes"/>.</param>
        /// <param name="keepExistingValues">Add the copied values to the target's current values instead of replacing them.</param>
        public void MapMultiSelectOptionSets(EntityReference source, string[] sourceAttributes, EntityReference target, string[] targetAttributes, bool keepExistingValues)
        {
            if (sourceAttributes.Length != targetAttributes.Length)
            {
                throw new InvalidPluginExecutionException(
                    $"The number of source attributes ({sourceAttributes.Length}) does not match the number of target attributes ({targetAttributes.Length}).");
            }

            var sourceRecord = Service.Retrieve(source.LogicalName, source.Id, new ColumnSet(sourceAttributes));
            var values = new Dictionary<string, OptionSetValueCollection>();

            for (var i = 0; i < sourceAttributes.Length; i++)
            {
                if (sourceRecord.GetAttributeValue<object>(sourceAttributes[i]) is OptionSetValueCollection sourceValues)
                {
                    values[targetAttributes[i]] = sourceValues;
                }
                else
                {
                    Trace($"Source attribute '{sourceAttributes[i]}' is empty or not a multi-select option set; skipped.");
                }
            }

            SetMultiSelectOptionSets(target, values, keepExistingValues);
        }

        /// <summary>The parties of an activity with one participation type (from, to, cc, ...).</summary>
        public static QueryExpression ActivityPartiesQuery(Guid activityId, int participationTypeMask)
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
    }
}
