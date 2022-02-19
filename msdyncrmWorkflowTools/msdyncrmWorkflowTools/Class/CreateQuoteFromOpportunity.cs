using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CreateQuoteFromOpportunity : CodeActivity
    {
        [RequiredArgument]
        [Input("Opportunity")]
        [ReferenceTarget("opportunity")]
        public InArgument<EntityReference> Opportunity { get; set; }

        [Output("Quote")]
        [ReferenceTarget("quote")]
        public OutArgument<EntityReference> Quote { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference opportunity = this.Opportunity.Get(executionContext);
            #endregion
            // Convert the opportunity to a quote

            var genQuoteFromOppRequest = new GenerateQuoteFromOpportunityRequest
            {
                OpportunityId = opportunity.Id,
                ColumnSet = new ColumnSet("quoteid", "name")
            };
            var genQuoteFromOppResponse = (GenerateQuoteFromOpportunityResponse)objCommon.service.Execute(genQuoteFromOppRequest);
            Entity quote = genQuoteFromOppResponse.Entity;
            EntityReference _quote = new EntityReference(quote.LogicalName, quote.Id);

            this.Quote.Set(executionContext, _quote);
        }
    }
}
