using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.ServiceModel;

namespace msdyncrmWorkflowTools
{
    public class AssociateEntity : WorkflowActivityBase
    {
        /// <summary>Dataverse "Cannot insert duplicate key" (0x80040237): the association already exists.</summary>
        private const int DuplicateRecordErrorCode = -2147220937;

        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Relationship Name")]
        [Default("")]        
        public InArgument<string> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("Relationship Entity Name")]
        [Default("")]
        public InArgument<string> RelationshipEntityName { get; set; }

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var relationshipName = RelationshipName.Get(executionContext);
            var relationshipEntityName = RelationshipEntityName.Get(executionContext);
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            var parsedUrl = Utility.ParseRecordUrl(recordUrl);
            var parentObjectTypeCode=parsedUrl.ObjectTypeCode;
            var entityName = common.GetEntityNameFromCode(parentObjectTypeCode);
            var parentId = parsedUrl.Id;

            common.Trace($"ParentObjectTypeCode={parentObjectTypeCode}--ParentId={parentId}");
            #endregion

            #region "Associate Execution"

            try
            {
                common.AssociateEntity(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId, relationshipName, relationshipEntityName, entityName, parentId);
            }
            catch (FaultException<OrganizationServiceFault> ex) when (ex.Detail.ErrorCode == DuplicateRecordErrorCode)
            {
                // The records are already associated: nothing to do. Every other error goes to WorkflowActivityBase.
                common.Trace("The records are already associated: {0}", ex.Message);
            }
            #endregion
        }
    }
}
