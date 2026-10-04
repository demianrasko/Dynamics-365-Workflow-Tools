using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class UpdateChildRecords : CodeActivity
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

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var parentRecordUrl = ParentRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(parentRecordUrl))
            {
                return;
            }
            
            var urlParts = parentRecordUrl.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", "");
            var parentEntityId = urlParams[1].Replace("id=", "");
            var parentEntityType = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);

            objCommon.tracingService.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={parentEntityId}");

            var relationshipName = RelationshipName.Get(executionContext);
            var parentFieldNameToUpdate = ParentFieldNameToUpdate.Get(executionContext);
            var valueToSet = ValueToSet.Get(executionContext);
            var childFieldNameToUpdate = ChildFieldNameToUpdate.Get(executionContext);
            var updateOnlyActive = UpdateonlyActive.Get(executionContext);

            objCommon.tracingService.Trace($"{nameof(RelationshipName)}={relationshipName}--_ParentFieldNameToUpdate={parentFieldNameToUpdate}");
            objCommon.tracingService.Trace($"_ValueToSet={valueToSet}--_ChildFieldNameToUpdate={childFieldNameToUpdate}");
            #endregion

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service);

            commonClass.UpdateChildRecords(relationshipName, parentEntityType, parentEntityId, parentFieldNameToUpdate, valueToSet, childFieldNameToUpdate, updateOnlyActive);
        }
    }
}
