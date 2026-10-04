using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Text;

namespace msdyncrmWorkflowTools.Class
{
    public class QueueItemCount : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var sourceQueue = SourceQueue.Get(executionContext);

            objCommon.Trace($"sourceQueue: {sourceQueue.Id.ToString()} ");

            var countOnlyUnassigned = CountOnlyUnassigned.Get(executionContext);
            objCommon.Trace("countOnlyUnassigned: {0}", countOnlyUnassigned);


            #endregion

            //query for retrieving all the queueitems from one queue
            var sFetchXml = new StringBuilder(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' aggregate='true'>
                      <entity name='queueitem'>
                        <attribute name='objectid' alias='queueitem_count' aggregate='count'/>
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />");
            
            if (countOnlyUnassigned)
            {
                sFetchXml.Append("<condition attribute='workerid' operator='null' />");
            }

            sFetchXml.Append(@"
                            <condition attribute='queueid' operator='eq' uitype='queue' value='" + sourceQueue.Id + @"' />
                        </filter>
                      </entity>
                    </fetch>");

            objCommon.Trace($"FetchXML: {sFetchXml} ");
            var queueItemsCount = objCommon.service.RetrieveMultiple(new FetchExpression(sFetchXml.ToString()));

            if (queueItemsCount.Entities.Count == 0)
            {
                //no pending queue items
                ItemsCount.Set(executionContext, 0);
                return;
            }

            foreach (var c in queueItemsCount.Entities)
            {
                var aggregate2 = (int)((AliasedValue)c["queueitem_count"]).Value;
                objCommon.Trace("{0}", "Count of all queueItemsCount: " + aggregate2);
                ItemsCount.Set(executionContext, aggregate2);
            }
        }
    }
}
