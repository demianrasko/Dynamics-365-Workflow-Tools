using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class JsonParser : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("JSON")]
        [Default("")]
        public InArgument<String> JSON { get; set; }

        [RequiredArgument]
        [Input("JSON Path")]
        [Default("")]
        public InArgument<string> JSONPath { get; set; }


      
        [Output("JSON Result")]
        public OutArgument<String> JSONResult { get; set; }
        
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String json = this.JSON.Get(executionContext);
            String jsonPath = this.JSONPath.Get(executionContext);

            #endregion

           
            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            string res=commonClass.JsonParser(json, jsonPath);

            if (res == null) res = "";

            this.JSONResult.Set(executionContext, res);
            
        }
    }
}
