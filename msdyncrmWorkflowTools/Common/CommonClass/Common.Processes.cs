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
        /// Joins one value from each record a FetchXML query returns, e.g. "Contoso, Fabrikam, Litware".
        /// </summary>
        /// <param name="fetchXml">The fetch query; {PARENT_GUID} must already be replaced.</param>
        /// <param name="attributeName">The attribute to join; empty uses each record's first attribute.</param>
        /// <param name="separator">Text between the values; \n, \r and \t stand for a new line, carriage return and tab.</param>
        /// <param name="format">.NET format string for each value, e.g. "N2" or "yyyy-MM-dd"; empty for none.</param>
        /// <param name="top">Maximum number of values; 0 or less for all.</param>
        /// <returns>The joined values, or null when no record has a value.</returns>
        public string ConcatenateFromQuery(string fetchXml, string attributeName, string separator, string format, int top)
        {
            var values = RetrieveAllWithFetchXml(fetchXml)
                .Select(record => Utility.FormatConcatenationValue(record, attributeName, format))
                .Where(value => value != null);

            var list = (top > 0 ? values.Take(top) : values).ToList();

            Trace($"{list.Count} value(s) to concatenate.");

            return list.Count == 0 ? null : string.Join(Utility.ExpandEscapes(separator), list);
        }

        /// <summary>
        /// Applies the active routing rule to a record (usually a case).
        /// </summary>
        public void ApplyRoutingRule(EntityReference record)
        {
            Trace($"Applying the routing rule to {record.LogicalName} {record.Id}");
            Service.Execute(new ApplyRoutingRuleRequest { Target = record });
        }

        /// <summary>
        /// Switches a record to another business process flow.
        /// </summary>
        public void SetProcess(EntityReference record, EntityReference process)
        {
            Trace($"Setting process {process?.Id} on {record.LogicalName} {record.Id}");
            Service.Execute(new SetProcessRequest { Target = record, NewProcess = process });
        }

        /// <summary>
        /// Starts an on-demand workflow for each record.
        /// </summary>
        public void ExecuteWorkflow(Guid workflowId, IEnumerable<Guid> recordIds)
        {
            foreach (var recordId in recordIds)
            {
                Service.Execute(new ExecuteWorkflowRequest { EntityId = recordId, WorkflowId = workflowId });
            }
        }

        /// <summary>
        /// Moves a record's business process flow instance to the named stage.
        /// </summary>
        /// <param name="record">The record the process runs on.</param>
        /// <param name="process">The business process flow (workflow) the stage belongs to.</param>
        /// <param name="stageName">The stage name, as shown in the process.</param>
        public void SetProcessStage(EntityReference record, EntityReference process, string stageName)
        {
            var stageId = GetProcessStageId(process.Id, stageName);
            var instance = GetProcessInstance(record, process.Id);
            var instanceEntityName = GetProcessEntityName(process.Id);

            Trace($"Moving {instanceEntityName} {instance.Id} to stage '{stageName}' ({stageId}).");

            Service.Update(new Entity(instanceEntityName, instance.Id)
            {
                [AttributeNames.ActiveStageId] = new EntityReference(EntityNames.ProcessStage, stageId)
            });
        }

        /// <summary>
        /// The id of the stage named <paramref name="stageName"/> in a business process flow.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The process has no stage with that name.</exception>
        public Guid GetProcessStageId(Guid processId, string stageName)
        {
            // stage names are easily typed with a stray leading or trailing space
            stageName = stageName?.Trim();
            var stage = Service.RetrieveMultiple(ProcessStageQuery(processId, stageName)).Entities.FirstOrDefault();

            return stage?.Id ?? throw new InvalidPluginExecutionException($"Process stage '{stageName}' was not found in process {processId}.");
        }

        /// <summary>
        /// The record's instance of a business process flow. When the instances do not say which process they
        /// belong to, the record's active instance (the first one returned) is used.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The record has no instance of the process.</exception>
        public Entity GetProcessInstance(EntityReference record, Guid processId)
        {
            var request = new RetrieveProcessInstancesRequest
            {
                EntityId = record.Id,
                EntityLogicalName = record.LogicalName
            };

            var response = (RetrieveProcessInstancesResponse)Service.Execute(request);
            var instances = response.Processes.Entities;

            var instance = instances.Any(i => i.Contains(AttributeNames.ProcessId))
                ? instances.FirstOrDefault(i => i.GetAttributeValue<EntityReference>(AttributeNames.ProcessId)?.Id == processId)
                : instances.FirstOrDefault();

            if (instance == null)
            {
                throw new InvalidPluginExecutionException($"No instance of process {processId} was found for {record.LogicalName} {record.Id}.");
            }

            Trace($"Process instance: '{instance.GetAttributeValue<string>("name")}' ({instance.Id})");

            return instance;
        }

        /// <summary>
        /// The logical name of the entity that stores a business process flow's instances (the process unique name).
        /// </summary>
        public string GetProcessEntityName(Guid processId)
        {
            return Service.Retrieve(EntityNames.Workflow, processId, new ColumnSet(AttributeNames.UniqueName)).GetAttributeValue<string>(AttributeNames.UniqueName);
        }

        /// <summary>The stage of a business process flow with the given name.</summary>
        public static QueryExpression ProcessStageQuery(Guid processId, string stageName)
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
    }
}
