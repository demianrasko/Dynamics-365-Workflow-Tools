using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class QualifyLead : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Lead")]
        [ReferenceTarget("lead")]
        public InArgument<EntityReference> Lead { get; set; }

        [RequiredArgument]
        [Input("Create Account")]
        public InArgument<bool> CreateAccount { get; set; }

        [RequiredArgument]
        [Input("Create Contact")]
        public InArgument<bool> CreateContact { get; set; }

        [RequiredArgument]
        [Input("Create Opportunity")]
        public InArgument<bool> CreateOpportunity { get; set; }

        [Input("Existing Account")]
        [ReferenceTarget("account")]
        public InArgument<EntityReference> ExistingAccount { get; set; }

        [Input("Existing Contact")]
        [ReferenceTarget("contact")]
        public InArgument<EntityReference> ExistingContact { get; set; }

        [RequiredArgument]
        [Input("LeadStatus")]
        public InArgument<int> LeadStatus { get; set; }

        #endregion
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var lead = Lead.Get(executionContext);
            // [RequiredArgument] is only enforced in the designer; a dynamic value can still be empty at runtime.
            if (lead == null)
            {
                throw new InvalidPluginExecutionException("Lead is required.");
            }

            var createAccount = CreateAccount.Get(executionContext);
            var createContact = CreateContact.Get(executionContext);
            var createOpportunity = CreateOpportunity.Get(executionContext);
            var existingAccount = ExistingAccount.Get(executionContext);
            var existingContact = ExistingContact.Get(executionContext);
            var leadStatus = LeadStatus.Get(executionContext);

            common.Trace($"LeadID={lead.Id}");
            #endregion

            #region "QualifyLead Execution"
            var query = new QueryExpression("organization")
            {
                ColumnSet = new ColumnSet("basecurrencyid")
            };

            var result = common.service.RetrieveMultiple(query);
            var currencyId = (EntityReference)result.Entities[0]["basecurrencyid"];

            var request = new QualifyLeadRequest
            {
                CreateOpportunity = createOpportunity,
                CreateAccount = createAccount,
                CreateContact = createContact,
                OpportunityCurrencyId = currencyId
            };

            if (existingAccount != null)
            {
                request.OpportunityCustomerId = new EntityReference("account", existingAccount.Id);
            }
            else if (existingContact != null)
            {
                request.OpportunityCustomerId = new EntityReference("contact", existingContact.Id);
            }

            request.Status = new OptionSetValue(leadStatus);
            request.LeadId = new EntityReference("lead", lead.Id);

            common.service.Execute(request);
            common.Trace("  Executed OK.");

            #endregion
        }
    }
}
