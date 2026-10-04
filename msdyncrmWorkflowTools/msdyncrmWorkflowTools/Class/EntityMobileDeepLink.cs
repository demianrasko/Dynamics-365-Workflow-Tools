using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class EntityMobileDeepLink : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }


        [Output("Mobile Deep Link Edit")]
        public OutArgument<string> MobileDeepLinkEdit { get; set; }

        [Output("Mobile Deep Link New")]
        public OutArgument<string> MobileDeepLinkNew { get; set; }

        [Output("Mobile Deep Link Default View")]
        public OutArgument<string> MobileDeepLinkDefaultView { get; set; }

        #endregion



        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var _recordURL = RecordURL.Get(executionContext);
            if (_recordURL == null || _recordURL == string.Empty)
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }
            var parsedUrl = Utility.ParseRecordUrl(_recordURL);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var entityName = common.GetEntityNameFromCode(objectTypeCode);
            var objectId = parsedUrl.Id;
            common.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);


            #endregion

            #region "Generating Mobile Deep Links Execution"

            var recordURLEdit = string.Format("ms-dynamicsxrm://?pagetype=entity&etn={0}&id={1}", entityName, objectId);
            var recordURLNew = string.Format("ms-dynamicsxrm://?pagetype=create&etn={0}", entityName);
            var recordURLDefaultView = string.Format("ms-dynamicsxrm://?pagetype=view&etn={0}", entityName);

            common.Trace("MobileDeepLinkEdit: "+ recordURLEdit);
            common.Trace("MobileDeepLinkNew: "+ recordURLNew);
            common.Trace("MobileDeepLinkDefaultView: "+ recordURLDefaultView);

            MobileDeepLinkEdit.Set(executionContext, recordURLEdit);
            MobileDeepLinkNew.Set(executionContext, recordURLNew);
            MobileDeepLinkDefaultView.Set(executionContext, recordURLDefaultView);

            common.Trace("returned object links OK");

            #endregion

        }

        
    }

}
