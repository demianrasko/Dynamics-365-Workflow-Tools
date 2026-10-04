using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
namespace msdyncrmWorkflowTools.Class
{
    public class CopyToStaticList : CodeActivity
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            objCommon.tracingService.Trace(string.Format("marketingList: {0} ", marketingList.Id.ToString()));

            

            #endregion


            objCommon.service.Execute(new CopyDynamicListToStaticRequest { ListId = marketingList.Id });


        }
        
    }
}
