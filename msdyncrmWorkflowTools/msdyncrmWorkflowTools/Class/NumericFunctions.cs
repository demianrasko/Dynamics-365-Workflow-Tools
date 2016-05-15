using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
namespace msdyncrmWorkflowTools.Class
{
    public class NumericFunctions : CodeActivity
    {
        [RequiredArgument]
        [Input("Number 1")]
        public InArgument<decimal> Number1 { get; set; }

        [RequiredArgument]
        [Input("Number 2")]
        public InArgument<decimal> Number2 { get; set; }


        [Output("Add")]
        public OutArgument<decimal> Add { get; set; }

        [Output("Subtract")]
        public OutArgument<decimal> Subtract { get; set; }

        [Output("Multiply")]
        public OutArgument<decimal> Multiply { get; set; }

        [Output("Divide")]
        public OutArgument<decimal> Divide { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            decimal number1= this.Number1.Get(executionContext);
            decimal number2 = this.Number2.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("number 1 / number 2: {0} / {1}", number1.ToString(), number2.ToString()));

            #endregion

            this.Add.Set(executionContext, number1+number2);
            this.Subtract.Set(executionContext, number1 - number2);
            this.Multiply.Set(executionContext, number1 * number2);
            if (number2 != 0)
            {
                this.Divide.Set(executionContext, number1 / number2);
            }
            else
            {
                this.Divide.Set(executionContext, 0);
            }

        }
        
    }
}
