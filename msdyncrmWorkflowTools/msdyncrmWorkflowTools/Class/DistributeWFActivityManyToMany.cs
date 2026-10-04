using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class DistributeWFActivityManyToMany : WorkflowActivityBase
    {
        [Input("Relationship Name"), RequiredArgument]
        public InArgument<string> RelationshipName { get; set; }

        [ReferenceTarget("workflow"), Input("Distributed Workflow"), RequiredArgument]
        public InArgument<EntityReference> Workflow { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var relationshipName = RelationshipName.Get(executionContext);
            var workflow = Workflow.Get(executionContext);

            if (string.IsNullOrEmpty(relationshipName) || workflow == null)
            {
                throw new InvalidPluginExecutionException("Relationship Name and Distributed Workflow are required.");
            }

            // every record associated with the workflow's primary record through the N:N relationship
            var recordIds = common.GetManyToManyRelatedIds(relationshipName, common.Context.PrimaryEntityName, common.Context.PrimaryEntityId);
            common.Trace($"Running workflow {workflow.Id} for {recordIds.Count} records related through {relationshipName}");

            common.ExecuteWorkflow(workflow.Id, recordIds);
        }
    }
}
