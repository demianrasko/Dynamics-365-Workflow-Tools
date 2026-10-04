using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class GetRecordID : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Record URL")]
        [Default("")]
        public InArgument<string> RecordURL { get; set; }


        [Output("Record ID")]
        public OutArgument<string> RecordID { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var recordURL = RecordURL.Get(executionContext);


            #endregion

            var recordID=objCommon.GetRecordID(recordURL);
                
           
            RecordID.Set(executionContext, recordID);

        }
        

    }
}
