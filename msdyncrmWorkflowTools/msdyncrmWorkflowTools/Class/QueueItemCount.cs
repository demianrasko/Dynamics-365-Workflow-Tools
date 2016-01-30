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
    public class QueueItemCount : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Source Queue")]
        [ReferenceTarget("queue")]
        public InArgument<EntityReference> SourceQueue { get; set; }


        [RequiredArgument]
        [Input("Count Only Unassigned Items")]
        public InArgument<bool> CountOnlyUnassigned { get; set; }

        [Output("ItemsCount")]
        public OutArgument<int> ItemsCount { get; set; }

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

            bool countOnlyUnassigned = this.CountOnlyUnassigned.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("countOnlyUnassigned: {0} ", countOnlyUnassigned.ToString()));


            #endregion

            //query for retrieving all the queueitems from one queue
            StringBuilder sFetchXML = new StringBuilder(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' aggregate='true'>
                      <entity name='queueitem'>
                        <attribute name='objectid' alias='queueitem_count' aggregate='count'/>
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />");
            if (countOnlyUnassigned)
            {
                sFetchXML.Append("<condition attribute='workerid' operator='null' />");
            }
            sFetchXML.Append(@"
                            <condition attribute='queueid' operator='eq' uitype='queue' value='"+ sourceQueue.Id.ToString() + @"' />
                        </filter>
                      </entity>
                    </fetch>");

            objCommon.tracingService.Trace(String.Format("FetchXML: {0} ", sFetchXML.ToString()));
            EntityCollection queueItemsCount = objCommon.service.RetrieveMultiple(new FetchExpression(sFetchXML.ToString()));

            if (queueItemsCount.Entities.Count == 0)
            {
                //no pending queuitems
                this.ItemsCount.Set(executionContext, 0);
                return;
            }


            foreach (var c in queueItemsCount.Entities)
            {
                Int32 aggregate2 = (Int32)((AliasedValue)c["queueitem_count"]).Value;
                System.Console.WriteLine("Count of all queueItemsCount: " + aggregate2);
                this.ItemsCount.Set(executionContext, aggregate2);

            }
            
        }

    }
}
