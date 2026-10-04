using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class RemoveFromMarketingList : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        [Input("Account")]
        [ReferenceTarget("account")]
        public InArgument<EntityReference> account { get; set; }

        [Input("Contact")]
        [ReferenceTarget("contact")]
        public InArgument<EntityReference> contact { get; set; }

        [Input("Lead")]
        [ReferenceTarget("lead")]
        public InArgument<EntityReference> lead { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            common.Trace($"marketingList: {marketingList.Id.ToString()} ");

            var accountReference = account.Get(executionContext);

            var contactReference = contact.Get(executionContext);

            var leadReference = lead.Get(executionContext);

            #endregion

            var idToRemove = Guid.Empty;

            if (accountReference != null)
            {
                idToRemove = accountReference.Id;
            }
            else if (contactReference != null)
            {
                idToRemove = contactReference.Id;
            }
            else if (leadReference != null)
            {
                idToRemove = leadReference.Id;
            }

            common.Trace($"idToRemove: {idToRemove.ToString()} ");

            var removeRequest = new RemoveMemberListRequest
            {
                ListId = marketingList.Id,
                EntityId = idToRemove
            };

            common.service.Execute(removeRequest);
        }
    }
}
