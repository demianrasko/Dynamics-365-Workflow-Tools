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
    public class AzureFunctionCall : CodeActivity
    {

        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Function URL")]
        [Default("")]
        public InArgument<String> FunctionURL { get; set; }

        [RequiredArgument]
        [Input("Json Data")]
        public InArgument<String> JsonData { get; set; }


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

            String _functionURL= this.FunctionURL.Get(executionContext);
            String _jsonData = this.JsonData.Get(executionContext);
            objCommon.tracingService.Trace("FunctionURL:"+ _functionURL + " -- JsonData:"+ _jsonData);

            #endregion

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service);
            string result=commonClass.AzureFunctionCall(_jsonData,_functionURL);

            this.Result.Set(executionContext, result);
        }
    }
}
