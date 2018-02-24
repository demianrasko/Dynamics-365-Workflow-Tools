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
    public class GetRecordID : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Record URL")]
        [Default("")]
        public InArgument<String> RecordURL { get; set; }


        [Output("Record ID")]
        public OutArgument<string> RecordID { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String recordURL = this.RecordURL.Get(executionContext);


            #endregion

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            string recordID=commonClass.GetRecordID(recordURL);
                
           
            this.RecordID.Set(executionContext, recordID);

        }
        

    }
}
