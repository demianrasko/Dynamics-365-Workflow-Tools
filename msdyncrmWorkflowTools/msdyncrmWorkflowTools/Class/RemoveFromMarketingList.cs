using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools.Class
{
    public class RemoveFromMarketingList : CodeActivity
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        [Input("Account")]
        [ReferenceTarget("account")]
        public InArgument<EntityReference> Account { get; set; }

        [Input("Contact")]
        [ReferenceTarget("contact")]
        public InArgument<EntityReference> Contact { get; set; }

        [Input("Lead")]
        [ReferenceTarget("lead")]
        public InArgument<EntityReference> Lead { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            objCommon.tracingService.Trace($"marketingList: {marketingList.Id.ToString()} ");

            var account = Account.Get(executionContext);

            var contact = Contact.Get(executionContext);

            var lead = Lead.Get(executionContext);

            #endregion

            var idToRemove = Guid.Empty;

            if (account != null)
            {
                idToRemove = account.Id;
            }
            else if (contact != null)
            {
                idToRemove = contact.Id;
            }
            else if (lead != null)
            {
                idToRemove = lead.Id;
            }

            objCommon.tracingService.Trace($"idToRemove: {idToRemove.ToString()} ");

            var removeRequest = new RemoveMemberListRequest
            {
                ListId = marketingList.Id,
                EntityId = idToRemove
            };
            
            objCommon.service.Execute(removeRequest);
        }
    }
}
