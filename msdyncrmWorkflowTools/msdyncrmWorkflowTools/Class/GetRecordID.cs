using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class GetRecordID : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Record URL")]
        [Default("")]
        public InArgument<string> RecordURL { get; set; }


        [Output("Record ID")]
        public OutArgument<string> RecordID { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var recordURL = RecordURL.Get(executionContext);


            #endregion

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            var recordID=commonClass.GetRecordID(recordURL);
                
           
            RecordID.Set(executionContext, recordID);

        }
        

    }
}
