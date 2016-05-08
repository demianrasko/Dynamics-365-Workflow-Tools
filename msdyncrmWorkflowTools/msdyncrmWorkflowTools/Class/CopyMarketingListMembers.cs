using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference sourceList = this.SourceList.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", sourceList.Id.ToString()));

            EntityReference targetList = this.TargetList.Get(executionContext);
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
