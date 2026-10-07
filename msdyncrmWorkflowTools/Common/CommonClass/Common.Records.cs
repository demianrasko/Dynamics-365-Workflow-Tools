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
        /// <param name="copyToFieldName">Optional field on the same record to set to the new value, e.g. a plain field
        /// that a rollup on the next level up can use (a rollup can't use another rollup).</param>
        /// <returns>The recalculated value.</returns>
        public object CalculateRollupField(EntityReference record, string fieldName, string copyToFieldName = null)
        {
            // a stray space in the field name makes Dataverse report that the column doesn't exist
            fieldName = fieldName?.Trim();
            Trace($"Calculating rollup {fieldName} on {record.LogicalName} {record.Id}");
            var response = Service.Execute(new CalculateRollupFieldRequest { Target = record, FieldName = fieldName });
            var entity = response.Results.Contains("Entity") ? response.Results["Entity"] as Entity : null;
            var value = entity != null && entity.Contains(fieldName) ? entity[fieldName] : null;

            copyToFieldName = copyToFieldName?.Trim();
            if (!string.IsNullOrEmpty(copyToFieldName))
            {
                // the response has the new value; a later step reading the record may still get the old one
                Trace($"Copying it to {copyToFieldName}");
                Service.Update(new Entity(record.LogicalName, record.Id)
                {
                    [copyToFieldName] = value
                });
            }

            return value;
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
        /// Deletes a record given either by its record URL or by its table name and id, for the Delete Record activity.
        /// </summary>
        /// <param name="deleteUsingRecordUrl">Use <paramref name="recordUrl"/>; otherwise the table name and id.</param>
        /// <param name="recordUrl">The record's URL.</param>
        /// <param name="entityTypeName">The record's table (logical name).</param>
        /// <param name="entityGuid">The record's id as text.</param>
        /// <exception cref="InvalidPluginExecutionException">The inputs the chosen way needs are missing, or the id isn't a GUID.</exception>
        public void DeleteRecord(bool deleteUsingRecordUrl, string recordUrl, string entityTypeName, string entityGuid)
        {
            if (deleteUsingRecordUrl)
            {
                if (string.IsNullOrEmpty(recordUrl))
                {
                    throw new InvalidPluginExecutionException("ERROR: Delete Record URL to be deleted missing.");
                }

                DeleteRecord(GetRecordReference(recordUrl));
                return;
            }

            if (string.IsNullOrEmpty(entityTypeName) || string.IsNullOrEmpty(entityGuid))
            {
                throw new InvalidPluginExecutionException("ERROR: Entity Type name or GUID to be deleted missing.");
            }

            if (!Guid.TryParse(entityGuid, out var id))
            {
                throw new InvalidPluginExecutionException($"ERROR: Entity Guid '{entityGuid}' is not a valid GUID.");
            }

            DeleteRecord(new EntityReference(entityTypeName, id));
        }

        /// <summary>
        /// Creates a copy of the record a required record URL points at (see the overload below), for the Clone
        /// Record activity.
        /// </summary>
        /// <returns>The id of the copy.</returns>
        /// <exception cref="InvalidPluginExecutionException">The record URL is empty.</exception>
        public Guid CloneRecord(string recordUrl, string fieldstoIgnore, string prefix)
        {
            var record = GetRecordReference(recordUrl, "Cloning Record URL");

            return CloneRecord(record.LogicalName, record.Id, fieldstoIgnore, prefix);
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
        /// <param name="copyStatus">Give the copy the record's status and status reason. Without it the copy starts in
        /// its default (active) state.</param>
        /// <returns>The id of the copy.</returns>
        public Guid CloneRecord(string entityName, Guid objectId, string fieldstoIgnore, string prefix, IDictionary<string, object> fieldsToReplace = null, bool copyStatus = false)
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

                    if (!Utility.GetParticipation(attribute2, out var participationTypeMask))
                    {
                        throw new InvalidPluginExecutionException($"Unsupported party list attribute '{attribute2}'.");
                    }

                    var returnCollection = Service.RetrieveMultiple(ActivityPartiesQuery(objectId, participationTypeMask));

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

            // statecode and statuscode aren't copied above, so the copy starts in its default (active) state unless
            // copyStatus asks for the record's own
            var state = copyStatus ? retrievedObject.GetAttributeValue<OptionSetValue>(AttributeNames.StateCode) : null;
            var status = copyStatus ? retrievedObject.GetAttributeValue<OptionSetValue>(AttributeNames.StatusCode) : null;

            if (state?.Value == 0 && status != null)
            {
                // an active status reason can be set on create
                newEntity[AttributeNames.StatusCode] = status;
            }

            Trace("creating cloned object...");
            var id = Service.Create(newEntity);
            Trace("cloned object OK");

            if (state != null && state.Value != 0 && status != null)
            {
                // an inactive state can only be set once the record exists
                SetState(new EntityReference(entityName, id), state.Value, status.Value);
            }

            return id;
        }

        /// <summary>
        /// Deletes a record's audit history.
        /// </summary>
        public void DeleteRecordAuditHistory(EntityReference record)
        {
            Trace($"Deleting the audit history of {record.LogicalName} {record.Id}");
            Service.Execute(new DeleteRecordChangeHistoryRequest { Target = record });
        }

        public void DeleteRecordAuditHistory(string logicalName, Guid id)
        {
            DeleteRecordAuditHistory(new EntityReference(logicalName, id));
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
        /// The selected values of a multi-select option set field as comma-separated numbers, for the Get Multi Select
        /// Option Set activity; empty when none are selected.
        /// </summary>
        /// <param name="recordUrl">Record URL of the record to read.</param>
        /// <param name="attributeName">Logical name of the multi-select option set field.</param>
        /// <param name="retrieveNames">Also return the selected options' labels.</param>
        /// <param name="names">The labels, comma separated; null unless <paramref name="retrieveNames"/> is set and an
        /// option is selected.</param>
        /// <exception cref="InvalidPluginExecutionException">The record URL or the field name is empty.</exception>
        public string GetMultiSelectOptionSetText(string recordUrl, string attributeName, bool retrieveNames, out string names)
        {
            var source = GetRecordReference(recordUrl, "Source Record URL");
            Utility.Required(attributeName, "Attribute Name");
            names = null;

            var values = GetMultiSelectOptionSet(source, attributeName);

            if (values.Count == 0)
            {
                Trace("No selected options");
                return string.Empty;
            }

            var selectedValues = Utility.JoinOptionSetValues(values);
            Trace($"Selected values: {selectedValues}");

            if (retrieveNames)
            {
                names = GetOptionSetNames(source.LogicalName, attributeName, values);
                Trace($"Selected names: {names}");
            }

            return selectedValues;
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
        /// Sets a multi-select option set field from values typed as text (e.g. "1,2,3"), for the Set Multi Select
        /// Option Set activity. Values that aren't whole numbers are skipped and traced.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The record URL, the field name or the values are empty.</exception>
        public void SetMultiSelectOptionSet(string recordUrl, string attributeName, string values, bool keepExistingValues)
        {
            var target = GetRecordReference(recordUrl, "Target Record URL");
            Utility.Required(attributeName, "Attribute Name");
            Utility.Required(values, "Attribute Values");

            var invalidValues = new List<string>();
            var optionValues = Utility.ParseOptionSetValues(values, invalidValues);

            if (invalidValues.Count > 0)
            {
                Trace($"Skipped values that are not whole numbers: '{string.Join("', '", invalidValues)}'");
            }

            SetMultiSelectOptionSet(target, attributeName, optionValues, keepExistingValues);
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
        public void MapMultiSelectOptionSets(EntityReference source, IList<string> sourceAttributes, EntityReference target, IList<string> targetAttributes, bool keepExistingValues)
        {
            if (sourceAttributes.Count != targetAttributes.Count)
            {
                throw new InvalidPluginExecutionException(
                    $"The number of source attributes ({sourceAttributes.Count}) does not match the number of target attributes ({targetAttributes.Count}).");
            }

            var sourceRecord = Service.Retrieve(source.LogicalName, source.Id, new ColumnSet(sourceAttributes.ToArray()));
            var values = new Dictionary<string, OptionSetValueCollection>();

            for (var i = 0; i < sourceAttributes.Count; i++)
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

        /// <summary>
        /// Copies multi-select option set fields between the records two record URLs point at, with the field names
        /// given as comma- or semicolon-separated lists, for the Map Multi Select Option Set activity.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">A record URL or a field list is empty, or the lists have
        /// different lengths.</exception>
        public void MapMultiSelectOptionSets(string sourceRecordUrl, string sourceAttributes, string targetRecordUrl, string targetAttributes, bool keepExistingValues)
        {
            MapMultiSelectOptionSets(
                GetRecordReference(sourceRecordUrl, "Source Record URL"),
                Utility.SplitList(Utility.Required(sourceAttributes, "Source Attributes")),
                GetRecordReference(targetRecordUrl, "Target Record URL"),
                Utility.SplitList(Utility.Required(targetAttributes, "Target Attributes")),
                keepExistingValues);
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
