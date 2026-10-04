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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var relationshipName = RelationshipName.Get(executionContext);
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                return;
            }

            var parsedUrl = Utility.ParseRecordUrl(recordUrl);
            var parentObjectTypeCode=parsedUrl.ObjectTypeCode;
            var parentId = parsedUrl.Id;

            var entityName = objCommon.GetEntityNameFromCode(parentObjectTypeCode);

            objCommon.Trace($"ParentObjectTypeCode={parentObjectTypeCode}--ParentId={parentId}");
            #endregion
            
            #region "Disassociate Execution"

            var relatedEntities = new EntityReferenceCollection
            {
                new EntityReference(entityName, new Guid(parentId))
            };

            var relationship = new Relationship(relationshipName);
            
            objCommon.service.Disassociate(objCommon.context.PrimaryEntityName, objCommon.context.PrimaryEntityId, relationship,relatedEntities);
            
            #endregion
        }
    }
}
