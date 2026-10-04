using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class ShareSecuredField : WorkflowActivityBase
    {
        #region "Parameter Definition"


        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Input("Share With User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> UserToShare { get; set; }

        [Input("Share With Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> TeamToShare { get; set; }

        [RequiredArgument]
        [Input("Allow Read")]
        [Default("true")]
        public InArgument<bool> AllowRead { get; set; }

        [RequiredArgument]
        [Input("Allow Update")]
        [Default("true")]
        public InArgument<bool> AllowUpdate { get; set; }


        #endregion

        
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Load CRM Service from context"

            
            objCommon.Trace("Entered ShareSecuredField.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}", executionContext.ActivityInstanceId, executionContext.WorkflowInstanceId);

            #endregion

            #region "Read Parameters"
            var _RecordURL = RecordURL.Get(executionContext);
            if (_RecordURL == null || _RecordURL == string.Empty)
            {
                return;
            }
            var parsedUrl = Utility.ParseRecordUrl(_RecordURL);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode);
            var objectId = parsedUrl.Id;
            objCommon.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            #endregion

            #region "Clone Execution"


            ExecuteCore(executionContext, objCommon.context, objCommon.service, entityName, new Guid (objectId));


            objCommon.Trace("OK");

            #endregion

        }

        private void ExecuteCore(CodeActivityContext executionContext, IWorkflowContext context, IOrganizationService service, string entityName, Guid entityId)
        {
            

            //string entityName = context.PrimaryEntityName;
            //Guid entityId = context.PrimaryEntityId;
            var attributeName = AttributeName.Get(executionContext);
            var userToShare = UserToShare.Get(executionContext);
            var teamToShare = TeamToShare.Get(executionContext);
            var allowRead = AllowRead.Get(executionContext);
            var allowUpdate = AllowUpdate.Get(executionContext);

            if (userToShare != null)
            {
                var objectId = userToShare.Id;
                ShareSecuredFieldCore(service, entityName, attributeName, entityId, objectId, allowRead, allowUpdate, false);
            }

            if (teamToShare != null)
            {
                var objectId = teamToShare.Id;
                ShareSecuredFieldCore(service, entityName, attributeName, entityId, objectId, allowRead, allowUpdate);
            }
        }

        private void ShareSecuredFieldCore(IOrganizationService service, string entityName, string attributeName, Guid objectId, Guid principalId, bool allowRead, bool allowUpdate, bool shareWithTeam = true)
        {
            // Create the request
            var attributeRequest = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };

            // Execute the request
            var attributeResponse = (RetrieveAttributeResponse)service.Execute(attributeRequest);

            if (attributeResponse.AttributeMetadata != null && attributeResponse.AttributeMetadata.IsSecured != null && attributeResponse.AttributeMetadata.IsSecured.HasValue && attributeResponse.AttributeMetadata.IsSecured.Value)
            {
                // Create the query for retrieve User Shared Attribute permissions.
                var queryPOAA = new QueryExpression("principalobjectattributeaccess");
                queryPOAA.ColumnSet = new ColumnSet(new string[] { "readaccess", "updateaccess" });
                queryPOAA.Criteria.FilterOperator = LogicalOperator.And;
                queryPOAA.Criteria.Conditions.Add(new ConditionExpression("attributeid", ConditionOperator.Equal, attributeResponse.AttributeMetadata.MetadataId));
                queryPOAA.Criteria.Conditions.Add(new ConditionExpression("objectid", ConditionOperator.Equal, objectId));
                queryPOAA.Criteria.Conditions.Add(new ConditionExpression("principalid", ConditionOperator.Equal, principalId));

                // Execute the query.
                var responsePOAA = service.RetrieveMultiple(queryPOAA);

                if (responsePOAA.Entities.Count > 0)
                {
                    var poaa = responsePOAA.Entities[0];

                    if (allowRead || allowUpdate)
                    {
                        poaa["readaccess"] = allowRead;
                        poaa["updateaccess"] = allowUpdate;

                        service.Update(poaa);
                    }
                    else
                    {
                        service.Delete("principalobjectattributeaccess", poaa.Id);
                    }
                }
                else
                {
                    if (allowRead || allowUpdate)
                    {
                        // Create POAA entity for user
                        var poaa = new Entity("principalobjectattributeaccess");
                        poaa["attributeid"] = attributeResponse.AttributeMetadata.MetadataId;
                        poaa["objectid"] = new EntityReference(entityName, objectId);
                        poaa["readaccess"] = allowRead;
                        poaa["updateaccess"] = allowUpdate;
                        if (shareWithTeam)
                        {
                            poaa["principalid"] = new EntityReference("team", principalId);
                        }
                        else
                        {
                            poaa["principalid"] = new EntityReference("systemuser", principalId);
                        }

                        service.Create(poaa);
                    }
                }
            }
        }


    }

}
