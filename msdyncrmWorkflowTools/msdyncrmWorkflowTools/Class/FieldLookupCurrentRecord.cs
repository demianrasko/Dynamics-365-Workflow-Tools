using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools.Class
{
    class FieldLookupCurrentRecord : CodeActivity
    {
  
  
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Field name")]
        [ReferenceTarget("")]
        public InArgument<String> FieldName { get; set; }

        [Output("ReturnValue")]
        public OutArgument<String> ReturnValue { get; set; }


        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _FieldName = this.FieldName.Get(executionContext);
            if (_FieldName == null || _FieldName == "")
            {
                return;
            }
          
            #endregion


            #region "FieldLookupCurrentRecord Execution"
           

            
            #endregion

        }
    }
}
