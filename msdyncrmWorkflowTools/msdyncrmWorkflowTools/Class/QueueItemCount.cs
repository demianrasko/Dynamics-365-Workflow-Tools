using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
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
            var sourceQueue = SourceQueue.Get(executionContext);

            common.Trace($"sourceQueue: {sourceQueue.Id.ToString()} ");

            var countOnlyUnassigned = CountOnlyUnassigned.Get(executionContext);
            common.Trace("countOnlyUnassigned: {0}", countOnlyUnassigned);

            var count = common.CountRecords(Queries.QueueItems(sourceQueue.Id, countOnlyUnassigned));
            common.Trace($"Count of all queueItemsCount: {count}");

            ItemsCount.Set(executionContext, count);
        }
    }
}
