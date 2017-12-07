using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class AzureTextAnalyticsSentiment : CodeActivity
    {

        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Subscription Key")]
        [Default("")]
        public InArgument<String> SubscriptionKey { get; set; }

        [RequiredArgument]
        [Input("Text to Analyze")]
        public InArgument<String> TexttoAnalyze { get; set; }

        [RequiredArgument]
        [Input("Language")]
        public InArgument<String> Language { get; set; }

        [Output("Result")]
        public OutArgument<string> Result { get; set; }
        #endregion

        //string relationshipName, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate
        //string parentEntityId, string parentEntityType, 

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"

            string _subscriptionKey = this.SubscriptionKey.Get(executionContext);
            string _texttoAnalyze = this.TexttoAnalyze.Get(executionContext);
            string _language = this.Language.Get(executionContext);

            objCommon.tracingService.Trace("_subscriptionKey:" + _subscriptionKey + " -- TexttoAnalyze:" + TexttoAnalyze + " -- _language:"+ _language);

            #endregion

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service);
            string result=commonClass.AzureTextAnalyticsSentiment(_subscriptionKey, _texttoAnalyze, _language);

            this.Result.Set(executionContext, result);
        }
    }
}
