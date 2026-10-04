using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class JsonParser : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("JSON")]
        [Default("")]
        public InArgument<string> JSON { get; set; }

        [RequiredArgument]
        [Input("JSON Path")]
        [Default("")]
        public InArgument<string> JSONPath { get; set; }


      
        [Output("JSON Result")]
        public OutArgument<string> JSONResult { get; set; }
        
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var json = JSON.Get(executionContext);
            var jsonPath = JSONPath.Get(executionContext);

            #endregion

           
            var res=objCommon.JsonParser(json, jsonPath);

            if (res == null) res = string.Empty;

            JSONResult.Set(executionContext, res);
            
        }
    }
}
