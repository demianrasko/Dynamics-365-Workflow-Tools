using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
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
            #region "Read Parameters"
            var relationshipName = RelationshipName.Get(executionContext);
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            var parsedUrl = common.ParseRecordUrl(recordUrl);
            //var parentObjectTypeCode=parsedUrl.ObjectTypeCode;
            //var parentId = parsedUrl.Id;
            //var entityName = parsedUrl.EntityName;

            common.Trace($"ParentObjectTypeCode={parsedUrl.EntityName}--ParentId={parsedUrl.Id}");
            #endregion

            #region "Disassociate Execution"

            var relatedEntities = new EntityReferenceCollection
            {
                new EntityReference(parsedUrl.EntityName, new Guid(parsedUrl.Id))
            };

            var relationship = new Relationship(relationshipName);

            common.Service.Disassociate(common.Context.PrimaryEntityName, common.Context.PrimaryEntityId, relationship,relatedEntities);

            #endregion
        }
    }
}
