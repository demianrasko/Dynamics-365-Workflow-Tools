using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class DisassociateEntity : WorkflowActivityBase
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
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            common.DisassociateEntity(
                new EntityReference(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId),
                RelationshipName.Get(executionContext),
                common.GetRecordReference(recordUrl));
        }
    }
}
