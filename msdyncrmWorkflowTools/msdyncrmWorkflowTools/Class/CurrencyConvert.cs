using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class CurrencyConvert : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Amount")]
        [Default("0")]
        public InArgument<Decimal> Amount{ get; set; }

        [RequiredArgument]
        [Input("From Currency")]
        [Default("")]
        public InArgument<string> FromCurrency { get; set; }

        [RequiredArgument]
        [Input("To Currency")]
        [Default("")]
        public InArgument<string> ToCurrency { get; set; }

        
        [Output("Result")]
        public OutArgument<Decimal> Result { get; set; }

       

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            

            var amount = this.Amount.Get(executionContext);
            var fromCurrency= this.FromCurrency.Get(executionContext);
            var toCurrency = this.ToCurrency.Get(executionContext);

            #endregion
            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            var result=commonClass.CurrencyConvert(amount,fromCurrency, toCurrency);


            this.Result.Set(executionContext, result);
                    

        }
     

        




    }

    


}
