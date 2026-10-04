// Not in the Power Platform build: it needs Dynamics 365 tables (quote).
#if !POWERPLATFORM
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class WinQuote : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Quote")]
        [ReferenceTarget("quote")]
        public InArgument<EntityReference> Quote { get; set; }

        [Input("Message")]
        public InArgument<string> Message { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var quote = Quote.Get(executionContext);
            if (quote == null)
            {
                throw new InvalidPluginExecutionException("Quote is required.");
            }

            var message = Message.Get(executionContext);
            common.Trace($"quote: {quote.Id} message: {message}");
            #endregion

            var quoteClose = new Entity("quoteclose")
            {
                ["subject"] = message,
                ["quoteid"] = quote
            };

            common.Service.Execute(new WinQuoteRequest
            {
                QuoteClose = quoteClose,
                Status = new OptionSetValue(-1)
            });
        }
    }
}
#endif
