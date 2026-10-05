using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Entity Mobile Deep Link")]
    public class EntityMobileDeepLink : WorkflowActivityBase
    {
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = RecordUrl.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }
            var parsedUrl = common.ParseRecordUrl(recordUrl);

            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

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
        }
    }
}
