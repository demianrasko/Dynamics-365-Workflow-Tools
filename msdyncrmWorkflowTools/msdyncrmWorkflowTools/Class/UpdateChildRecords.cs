using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var parentRecordUrl = ParentRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(parentRecordUrl))
            {
                return;
            }
            
            var parsedUrl = Utility.ParseRecordUrl(parentRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var parentEntityId = parsedUrl.Id;
            var parentEntityType = objCommon.GetEntityNameFromCode(objectTypeCode);

            objCommon.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={parentEntityId}");

            var relationshipName = RelationshipName.Get(executionContext);
            var parentFieldNameToUpdate = ParentFieldNameToUpdate.Get(executionContext);
            var valueToSet = ValueToSet.Get(executionContext);
            var childFieldNameToUpdate = ChildFieldNameToUpdate.Get(executionContext);
            var updateOnlyActive = UpdateonlyActive.Get(executionContext);

            objCommon.Trace($"{nameof(RelationshipName)}={relationshipName}--_ParentFieldNameToUpdate={parentFieldNameToUpdate}");
            objCommon.Trace($"_ValueToSet={valueToSet}--_ChildFieldNameToUpdate={childFieldNameToUpdate}");
            #endregion


            objCommon.UpdateChildRecords(relationshipName, parentEntityType, parentEntityId, parentFieldNameToUpdate, valueToSet, childFieldNameToUpdate, updateOnlyActive);
        }
    }
}
