using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class PickFromQueue : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Source Queue")]
        [ReferenceTarget("queue")]
        public InArgument<EntityReference> SourceQueue { get; set; }

        [RequiredArgument]
        [Input("Remove Items From Source Queue")]
        public InArgument<bool> RemoveItems { get; set; }

        [RequiredArgument]
        [Input("Quantity Items")]
        public InArgument<int> Quantity { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var sourceQueue = SourceQueue.Get(executionContext);

            common.Trace($"sourceQueue: {sourceQueue.Id.ToString()} ");

            var removeItems = RemoveItems.Get(executionContext);
            common.Trace($"removeItems: {removeItems.ToString()} ");

            var quantity = Quantity.Get(executionContext);
            common.Trace($"quantity: {quantity.ToString()} ");

            #endregion

            // active, unassigned queue items, newest first; only the requested quantity is used
            var queueItems = common.service.RetrieveMultiple(Queries.QueueItems(sourceQueue.Id, onlyUnassigned: true, top: Math.Max(quantity, 1)));

            //no pending queue items
            if (queueItems.Entities.Count == 0)
            {
                return;
            }

            var count = 0;
            foreach (var queItem in queueItems.Entities)
            {
                //pick from Queue
                var pickFromQueueRequest = new PickFromQueueRequest
                {
                    QueueItemId = queItem.Id,
                    WorkerId = common.context.InitiatingUserId, 
                    RemoveQueueItem = removeItems
                };

                common.service.Execute(pickFromQueueRequest);
                count++;

                //only pick the defined Quantity
                if (count >= quantity)
                {
                    break;
                }
            }
        }
    }
}
