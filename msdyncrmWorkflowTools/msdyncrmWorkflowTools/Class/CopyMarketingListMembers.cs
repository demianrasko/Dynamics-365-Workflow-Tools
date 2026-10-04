using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
namespace msdyncrmWorkflowTools.Class
{
    public class CopyMarketingListMembers : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Source List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> SourceList { get; set; }

        [RequiredArgument]
        [Input("Target List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> TargetList { get; set; }



        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var sourceList = SourceList.Get(executionContext);
            objCommon.Trace(string.Format("marketingList: {0} ", sourceList.Id.ToString()));

            var targetList = TargetList.Get(executionContext);
            objCommon.Trace(string.Format("campaign: {0} ", targetList.Id.ToString()));


            #endregion

            var request = new CopyMembersListRequest
            {
                SourceListId = sourceList.Id,
                TargetListId = targetList.Id
            };

            objCommon.service.Execute(request);


        }

    }
}
