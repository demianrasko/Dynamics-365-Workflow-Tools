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
    public class GetAppModuleID : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Application Unique Name")]
        [Default("")]
        public InArgument<String> AppModuleUniqueName { get; set; }

        [Output("App Module ID")]
        public OutArgument<string> AppModuleId { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"
            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String appModuleUniqueName = this.AppModuleUniqueName.Get(executionContext);
            #endregion

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            
            string appModuleId = commonClass.GetAppModuleId(appModuleUniqueName);
                
            this.AppModuleId.Set(executionContext, appModuleId);

        }
        

    }
}
