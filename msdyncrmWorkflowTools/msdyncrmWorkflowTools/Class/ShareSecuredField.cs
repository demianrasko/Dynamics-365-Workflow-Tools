using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class ShareSecuredField : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordUrl { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Input("Share With User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> UserToShare { get; set; }

        [Input("Share With Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> TeamToShare { get; set; }

        [RequiredArgument]
        [Input("Allow Read")]
        [Default("true")]
        public InArgument<bool> AllowRead { get; set; }

        [RequiredArgument]
        [Input("Allow Update")]
        [Default("true")]
        public InArgument<bool> AllowUpdate { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var parsedUrl = common.ParseRecordUrl(RecordUrl.Get(executionContext));
            common.Trace($"ObjectTypeCode={parsedUrl.ObjectTypeCode}--ParentId={parsedUrl.Id}");

            common.ShareSecuredField(
                new EntityReference(parsedUrl.EntityName, new Guid(parsedUrl.Id)),
                AttributeName.Get(executionContext),
                AllowRead.Get(executionContext),
                AllowUpdate.Get(executionContext),
                UserToShare.Get(executionContext),
                TeamToShare.Get(executionContext));
        }
    }
}
