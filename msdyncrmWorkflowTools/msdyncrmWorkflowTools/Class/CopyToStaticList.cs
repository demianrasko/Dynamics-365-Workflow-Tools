using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
namespace msdyncrmWorkflowTools.Class
{
    public class CopyToStaticList : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        



        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            objCommon.Trace(string.Format("marketingList: {0} ", marketingList.Id.ToString()));

            

            #endregion


            objCommon.service.Execute(new CopyDynamicListToStaticRequest { ListId = marketingList.Id });


        }
        
    }
}
