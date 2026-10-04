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
        public InArgument<EntityReference> Account { get; set; }

        [Input("Contact")]
        [ReferenceTarget("contact")]
        public InArgument<EntityReference> Contact { get; set; }

        [Input("Lead")]
        [ReferenceTarget("lead")]
        public InArgument<EntityReference> Lead { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            objCommon.Trace(string.Format("marketingList: {0} ", marketingList.Id.ToString()));

            var account = Account.Get(executionContext);
            
            var contact = Contact.Get(executionContext);
           
            var lead = Lead.Get(executionContext);

            #endregion

            var idToAdd = Guid.Empty;

            if (account != null)
            {
                idToAdd = account.Id;
            }
            else if (contact != null)
            {
                idToAdd = contact.Id;
            }
            else if (lead != null)
            {
                idToAdd = lead.Id;
            }
            objCommon.Trace(string.Format("idToAdd: {0} ", idToAdd.ToString()));

            var addRequest = new AddMemberListRequest
            {
                ListId = marketingList.Id,
                EntityId = idToAdd
            };

            objCommon.service.Execute(addRequest);
        }
    }
}
