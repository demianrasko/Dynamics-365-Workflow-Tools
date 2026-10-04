using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
namespace msdyncrmWorkflowTools.Class
{
    public class CopyMarketingListMembers : CodeActivity
    {
        [RequiredArgument]
        [Input("Source List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> SourceList { get; set; }

        [RequiredArgument]
        [Input("Target List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> TargetList { get; set; }



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var sourceList = SourceList.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", sourceList.Id.ToString()));

            var targetList = TargetList.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("campaign: {0} ", targetList.Id.ToString()));


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
