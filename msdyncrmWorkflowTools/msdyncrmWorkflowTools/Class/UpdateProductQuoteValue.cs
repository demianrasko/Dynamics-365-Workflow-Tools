using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class UpdateProductQuoteValue : CodeActivity
    {
        [RequiredArgument]
        [Input("Quote Product")]
        [ReferenceTarget("quotedetail")]
        public InArgument<EntityReference> Quote { get; set; }

        [RequiredArgument]
        [Input("Discount Amount")]
        public InArgument<decimal> Discountamount { get; set; }
        
        //"manualdiscountamount"
        [RequiredArgument]
        [Input("Field name to update (manualdiscountamount)")]
        public InArgument<string> Fieldname { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference quote = this.Quote.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("quotedetail: {0} ", quote.Id.ToString()));

            decimal discountamount = this.Discountamount.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("Discountamount: {0} ", discountamount.ToString()));

            string fieldname = this.Fieldname.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("Fieldname: {0} ", fieldname.ToString()));

            #endregion  

            Entity quoteEnt = new Entity(quote.LogicalName, quote.Id);
            if (quoteEnt.Attributes.Contains(fieldname))
            {
                quoteEnt.Attributes.Add(fieldname, new Money(discountamount));
            }
            else
            {
                quoteEnt.Attributes[fieldname] = new Money(discountamount);
            }

            objCommon.service.Update(quoteEnt);
        }
    }
}
