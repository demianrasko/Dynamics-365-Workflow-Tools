using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Text;

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

            //query for retrieving all the queueitems from one queue
            var sFetchXml = new StringBuilder(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='queueitem'>
                        <attribute name='enteredon' />
                        <attribute name='objecttypecode' />
                        <attribute name='objectid' />
                        <attribute name='queueid' />
                        <order attribute='enteredon' descending='true' />
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
                          <condition attribute='workerid' operator='null' />
                          <condition attribute='queueid' operator='eq' uitype='queue' value='"+ sourceQueue.Id.ToString() + @"' />
                        </filter>
                      </entity>
                    </fetch>");

            common.Trace($"FetchXML: {sFetchXml} ");
            var queueItems = common.service.RetrieveMultiple(new FetchExpression(sFetchXml.ToString()));

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
