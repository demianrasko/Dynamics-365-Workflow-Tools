using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class WinQuote : CodeActivity
    {
        [RequiredArgument]
        [Input("Quote")]
        [ReferenceTarget("quote")]
        public InArgument<EntityReference> Quote { get; set; }

        [Input("Message")]
        public InArgument<string> Message { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference quote = this.Quote.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("quote: {0} ", quote.Id.ToString()));

            string message = this.Message.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("Discountamount: {0} ", message.ToString()));
            #endregion

            Entity quoteclose = new Entity("quoteclose");
            WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
            quoteclose.Attributes.Add("subject", message);
            quoteclose.Attributes.Add("quoteid", quote);
            winQuoteRequest.QuoteClose = quoteclose;
            winQuoteRequest.Status = new OptionSetValue(-1);
            objCommon.service.Execute(winQuoteRequest);
        }
    }
}
