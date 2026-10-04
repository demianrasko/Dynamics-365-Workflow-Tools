// Not in the Power Platform build: it needs Dynamics 365 tables (lead, list or salesliterature).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class QualifyLead : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Lead")]
        [ReferenceTarget(EntityNames.Lead)]
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
        [ReferenceTarget(EntityNames.Account)]
        public InArgument<EntityReference> ExistingAccount { get; set; }

        [Input("Existing Contact")]
        [ReferenceTarget(EntityNames.Contact)]
        public InArgument<EntityReference> ExistingContact { get; set; }

        [RequiredArgument]
        [Input("LeadStatus")]
        public InArgument<int> LeadStatus { get; set; }

        #endregion
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            // [RequiredArgument] is only enforced in the designer; a dynamic value can still be empty at runtime.
            var lead = Lead.Get(executionContext) ?? throw new InvalidPluginExecutionException("Lead is required.");

            common.QualifyLead(
                lead,
                CreateAccount.Get(executionContext),
                CreateContact.Get(executionContext),
                CreateOpportunity.Get(executionContext),
                ExistingAccount.Get(executionContext),
                ExistingContact.Get(executionContext),
                LeadStatus.Get(executionContext));
        }
    }
}
#endif
