using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class AddToMarketingList : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        [Input("Account")]
        [ReferenceTarget("account")]
        // ReSharper disable once InconsistentNaming
        public InArgument<EntityReference> account { get; set; }

        [Input("Contact")]
        [ReferenceTarget("contact")]
        // ReSharper disable once InconsistentNaming
        public InArgument<EntityReference> contact { get; set; }

        [Input("Lead")]
        [ReferenceTarget("lead")]
        // ReSharper disable once InconsistentNaming
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

            var idToAdd = Guid.Empty;

            if (accountReference != null)
            {
                idToAdd = accountReference.Id;
            }
            else if (contactReference != null)
            {
                idToAdd = contactReference.Id;
            }
            else if (leadReference != null)
            {
                idToAdd = leadReference.Id;
            }
            common.Trace($"idToAdd: {idToAdd.ToString()} ");

            var request = new AddMemberListRequest
            {
                ListId = marketingList.Id,
                EntityId = idToAdd
            };

            common.Service.Execute(request);
        }
    }
}
