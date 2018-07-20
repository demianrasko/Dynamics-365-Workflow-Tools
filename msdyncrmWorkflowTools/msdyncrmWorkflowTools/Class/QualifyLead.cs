using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace msdyncrmWorkflowTools
{
    public class QualifyLead : CodeActivity
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
        protected override void Execute(CodeActivityContext executionContext)
        {
            

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference lead = this.Lead.Get(executionContext);
            if (lead == null)
            {
                return;
            }

            bool createAccount = this.CreateAccount.Get(executionContext);
            bool createContact = this.CreateContact.Get(executionContext);
            bool createOpportunity = this.CreateOpportunity.Get(executionContext);
            EntityReference existingAccount = this.ExistingAccount.Get(executionContext);
            EntityReference existingContact = this.ExistingContact.Get(executionContext);
            int leadStatus = this.LeadStatus.Get(executionContext);

            objCommon.tracingService.Trace("LeadID=" + lead.Id);
            #endregion


            #region "QualifyLead Execution"
            var query = new QueryExpression("organization");
            query.ColumnSet = new ColumnSet("basecurrencyid");
            var result = objCommon.service.RetrieveMultiple(query);
            var currencyId = (EntityReference)result.Entities[0]["basecurrencyid"];


            var qualifyIntoOpportunityReq = new QualifyLeadRequest();

            qualifyIntoOpportunityReq.CreateOpportunity = createOpportunity;
            qualifyIntoOpportunityReq.CreateAccount = createAccount;
            qualifyIntoOpportunityReq.CreateContact = createContact;
            qualifyIntoOpportunityReq.OpportunityCurrencyId = currencyId;

            if (existingAccount != null)
            {
                qualifyIntoOpportunityReq.OpportunityCustomerId = new EntityReference(
                        "account", existingAccount.Id);
            }
            else if (existingContact != null)
            {
                qualifyIntoOpportunityReq.OpportunityCustomerId = new EntityReference(
                        "contact", existingContact.Id);
            }
            qualifyIntoOpportunityReq.Status = new OptionSetValue(leadStatus);
            qualifyIntoOpportunityReq.LeadId = new EntityReference("lead", lead.Id);
            

            var qualifyIntoOpportunityRes =
                (QualifyLeadResponse)objCommon.service.Execute(qualifyIntoOpportunityReq);
            Console.WriteLine("  Executed OK.");


            #endregion

        }
    }
}
