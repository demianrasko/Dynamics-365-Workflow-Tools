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
        public InArgument<string> RecordURL { get; set; }

        [Output("Mobile Deep Link Edit")]
        public OutArgument<string> MobileDeepLinkEdit { get; set; }

        [Output("Mobile Deep Link New")]
        public OutArgument<string> MobileDeepLinkNew { get; set; }

        [Output("Mobile Deep Link Default View")]
        public OutArgument<string> MobileDeepLinkDefaultView { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var links = common.GetMobileDeepLinks(RecordURL.Get(executionContext));

            MobileDeepLinkEdit.Set(executionContext, links.Edit);
            MobileDeepLinkNew.Set(executionContext, links.New);
            MobileDeepLinkDefaultView.Set(executionContext, links.DefaultView);
        }
    }
}
