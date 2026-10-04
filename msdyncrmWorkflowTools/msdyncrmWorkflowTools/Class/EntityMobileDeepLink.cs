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
        public InArgument<string> RecordUrl { get; set; }

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
            var recordUrl = RecordUrl.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }
            var parsedUrl = common.ParseRecordUrl(recordUrl);
            //var objectTypeCode = parsedUrl.ObjectTypeCode;
            //var entityName = parsedUrl.EntityName;
            //var objectId = parsedUrl.Id;
            
            common.Trace($"ObjectTypeCode={parsedUrl.EntityName}--ParentId={parsedUrl.Id}");

            #endregion

            #region "Generating Mobile Deep Links Execution"

            var recordUrlEdit = $"ms-dynamicsxrm://?pagetype=entity&etn={parsedUrl.EntityName}&id={parsedUrl.Id}";
            var recordUrlNew = $"ms-dynamicsxrm://?pagetype=create&etn={parsedUrl.EntityName}";
            var recordUrlDefaultView = $"ms-dynamicsxrm://?pagetype=view&etn={parsedUrl.EntityName}";

            common.Trace($"MobileDeepLinkEdit: {recordUrlEdit}");
            common.Trace($"MobileDeepLinkNew: {recordUrlNew}");
            common.Trace($"MobileDeepLinkDefaultView: {recordUrlDefaultView}");

            MobileDeepLinkEdit.Set(executionContext, recordUrlEdit);
            MobileDeepLinkNew.Set(executionContext, recordUrlNew);
            MobileDeepLinkDefaultView.Set(executionContext, recordUrlDefaultView);

            common.Trace("returned object links OK");

            #endregion
        }
    }
}
