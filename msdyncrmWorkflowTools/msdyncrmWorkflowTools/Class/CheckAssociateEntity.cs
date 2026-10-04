using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CheckAssociateEntity : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Relationship Name")]
        [Default("")]        
        public InArgument<string> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        [Output("Result")]
        public OutArgument<bool> Result { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var relationshipName = RelationshipName.Get(executionContext);
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            var parsedUrl = Utility.ParseRecordUrl(recordUrl);
            var parentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var entityName = common.GetEntityNameFromCode(parentObjectTypeCode);
            var parentId = parsedUrl.Id;

            common.Trace($"ParentObjectTypeCode={parentObjectTypeCode}--ParentId={parentId}");
            #endregion

            #region "Associate Execution"

            var intersectEntityName = common.GetIntersectEntityName(relationshipName);
            var relations = common.GetAssociations(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId, intersectEntityName, entityName, parentId);

            Result.Set(executionContext, relations.Entities.Count > 0);

            #endregion
        }
    }
}
