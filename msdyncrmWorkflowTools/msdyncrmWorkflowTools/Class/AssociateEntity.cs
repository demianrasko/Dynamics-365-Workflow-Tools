using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.ServiceModel;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Associate Entity")]
    public class AssociateEntity : WorkflowActivityBase
    {
        /// <summary>Dataverse "Cannot insert duplicate key" (0x80040237): the association already exists.</summary>
        private const int DuplicateRecordErrorCode = -2147220937;

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var relationshipName = RelationshipName.Get(executionContext);
            var relationshipEntityName = RelationshipEntityName.Get(executionContext);
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            var parsedUrl = common.ParseRecordUrl(recordUrl);

            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            try
            {
                common.AssociateEntity(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId, relationshipName, relationshipEntityName, parsedUrl.EntityName, parsedUrl.Id);
            }
            catch (FaultException<OrganizationServiceFault> ex) when (ex.Detail.ErrorCode == DuplicateRecordErrorCode)
            {
                // The records are already associated: nothing to do. Every other error goes to WorkflowActivityBase.
                common.Trace($"The records are already associated: {ex.Message}");
            }
        }
    }
}
