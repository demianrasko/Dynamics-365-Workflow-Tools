using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class PickFromQueue : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Source Queue")]
        [ReferenceTarget(EntityNames.Queue)]
        public InArgument<EntityReference> SourceQueue { get; set; }

        [RequiredArgument]
        [Input("Remove Items From Source Queue")]
        public InArgument<bool> RemoveItems { get; set; }

        [RequiredArgument]
        [Input("Quantity Items")]
        public InArgument<int> Quantity { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.PickFromQueue(
                SourceQueue.Get(executionContext).Id,
                common.Context.InitiatingUserId,
                RemoveItems.Get(executionContext),
                Quantity.Get(executionContext));
        }
    }
}
