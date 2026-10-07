using Microsoft.Xrm.Sdk;
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
            var parentRecordUrl = ParentRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(parentRecordUrl))
            {
                throw new InvalidPluginExecutionException("Parent Record URL is required.");
            }

            var parsedUrl = common.ParseRecordUrl(parentRecordUrl);

            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            var relationshipName = RelationshipName.Get(executionContext);
            var parentFieldNameToUpdate = ParentFieldNameToUpdate.Get(executionContext);
            var valueToSet = ValueToSet.Get(executionContext);
            var childFieldNameToUpdate = ChildFieldNameToUpdate.Get(executionContext);
            var updateOnlyActive = UpdateonlyActive.Get(executionContext);

            common.Trace($"{nameof(RelationshipName)}={relationshipName}--_ParentFieldNameToUpdate={parentFieldNameToUpdate}");
            common.Trace($"_ValueToSet={valueToSet}--_ChildFieldNameToUpdate={childFieldNameToUpdate}");

            common.UpdateChildRecords(relationshipName, parsedUrl.EntityName, parsedUrl.Id, parentFieldNameToUpdate, valueToSet, childFieldNameToUpdate, updateOnlyActive,
                ContinueIfARecordFails.Get(executionContext), out var failed);

            FailedRecords.Set(executionContext, failed);
        }
    }
}
