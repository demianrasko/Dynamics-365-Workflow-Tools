using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var sourceList = SourceList.Get(executionContext);
            common.Trace($"marketingList: {sourceList.Id.ToString()} ");

            var targetList = TargetList.Get(executionContext);
            common.Trace($"campaign: {targetList.Id.ToString()} ");

            #endregion

            var request = new CopyMembersListRequest
            {
                SourceListId = sourceList.Id,
                TargetListId = targetList.Id
            };

            common.Service.Execute(request);
        }
    }
}
