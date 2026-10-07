using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Queue Item Count")]
    public class QueueItemCount : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Source Queue")]
        [ReferenceTarget(EntityNames.Queue)]
        public InArgument<EntityReference> SourceQueue { get; set; }

        [RequiredArgument]
        [Input("Count Only Unassigned Items")]
        public InArgument<bool> CountOnlyUnassigned { get; set; }

        [Output("ItemsCount")]
        public OutArgument<int> ItemsCount { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            ItemsCount.Set(executionContext, common.CountQueueItems(SourceQueue.Get(executionContext).Id, CountOnlyUnassigned.Get(executionContext)));
        }
    }
}
