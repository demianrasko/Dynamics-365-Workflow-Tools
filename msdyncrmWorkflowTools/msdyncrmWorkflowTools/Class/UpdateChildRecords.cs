using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Update Child Records")]
    public class UpdateChildRecords : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Parent Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ParentRecordURL { get; set; }

        [RequiredArgument]
        [Input("Relationship Name")]
        [ReferenceTarget("")]
        public InArgument<string> RelationshipName { get; set; }

        [Input("Parent Field Name")]
        [ReferenceTarget("")]
        public InArgument<string> ParentFieldNameToUpdate { get; set; }

        [Input("Value to Set")]
        [ReferenceTarget("")]
        public InArgument<string> ValueToSet{ get; set; }

        [RequiredArgument]
        [Input("Child Field Name to Update")]
        [ReferenceTarget("")]
        public InArgument<string> ChildFieldNameToUpdate { get; set; }

        [RequiredArgument]
        [Input("Update only Active")]
        public InArgument<bool> UpdateonlyActive { get; set; }

        /// <summary>
        /// Skip a child that can't be updated (e.g. a plugin locks it) and update the rest; Failed Records counts the
        /// skipped ones. Steps saved before this input existed get No: one failure stops the step.
        /// </summary>
        [Input("Continue If A Record Fails")]
        [Default("false")]
        public InArgument<bool> ContinueIfARecordFails { get; set; }

        [Output("Failed Records")]
        public OutArgument<int> FailedRecords { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.UpdateChildRecords(ParentRecordURL.Get(executionContext), RelationshipName.Get(executionContext), ParentFieldNameToUpdate.Get(executionContext),
                ValueToSet.Get(executionContext), ChildFieldNameToUpdate.Get(executionContext), UpdateonlyActive.Get(executionContext),
                ContinueIfARecordFails.Get(executionContext), out var failed);

            FailedRecords.Set(executionContext, failed);
        }
    }
}
