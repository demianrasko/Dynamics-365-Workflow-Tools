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
            var _relationshipName = RelationshipName.Get(executionContext);
            var _recordURL = RecordURL.Get(executionContext);
            if (_recordURL == null || _recordURL == string.Empty)
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }
            var parsedUrl = Utility.ParseRecordUrl(_recordURL);
            var ParentObjectTypeCode=parsedUrl.ObjectTypeCode;
            var entityName = common.GetEntityNameFromCode(ParentObjectTypeCode);
            var ParentId = parsedUrl.Id;
            common.Trace($"ParentObjectTypeCode={ParentObjectTypeCode}--ParentId={ParentId}");
            #endregion

            #region "Associate Execution"

            var relations = common.GetAssociations(common.context.PrimaryEntityName, common.context.PrimaryEntityId,_relationshipName, entityName, ParentId);

            if (relations.Entities.Count > 0)
            {
                Result.Set(executionContext, true);
            }
            else
            {
                Result.Set(executionContext, false);
            }
            #endregion
        }
    }
}
