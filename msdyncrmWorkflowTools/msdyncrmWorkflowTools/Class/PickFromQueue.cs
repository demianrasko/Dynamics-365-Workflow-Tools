using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using msdyncrmWorkflowTools;


namespace msdyncrmWorkflowTools.Class
{
    public class PickFromQueue : CodeActivity
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

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference sourceQueue = this.SourceQueue.Get(executionContext);

            objCommon.tracingService.Trace(String.Format("sourceQueue: {0} ", sourceQueue.Id.ToString()));

            bool removeItems = this.RemoveItems.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("removeItems: {0} ", removeItems.ToString()));

            int quantity = this.Quantity.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("quantity: {0} ", quantity.ToString()));

            #endregion

            //query for retrieving all the queueitems from one queue
            StringBuilder sFetchXML = new StringBuilder(@"
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

            objCommon.tracingService.Trace(String.Format("FetchXML: {0} ", sFetchXML.ToString()));
            EntityCollection queueItems = objCommon.service.RetrieveMultiple(new FetchExpression(sFetchXML.ToString()));

            if (queueItems.Entities.Count == 0)
            {
                //no pending queuitems
                return;
            }

            int count = 0;
            foreach (Entity queItem in queueItems.Entities)
            {
                //pick from Queue
                PickFromQueueRequest pickFromQueueRequest = new PickFromQueueRequest
                {
                    QueueItemId = queItem.Id,
                    WorkerId = objCommon.context.InitiatingUserId, 
                    RemoveQueueItem = removeItems
                };
                objCommon.service.Execute(pickFromQueueRequest);
                count++;
                if (count >= quantity)
                {
                    //only pick the defined Quantity
                    break;
                }
            }

        }

    }
}
