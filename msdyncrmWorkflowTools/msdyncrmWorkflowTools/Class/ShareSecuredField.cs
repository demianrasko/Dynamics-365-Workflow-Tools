using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class ShareSecuredField : CodeActivity
    {
        #region "Parameter Definition"


        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> RecordURL { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<String> AttributeName { get; set; }

        [Input("Share With User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> UserToShare { get; set; }

        [Input("Share With Team")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> TeamToShare { get; set; }

        [RequiredArgument]
        [Input("Allow Read")]
        [Default("true")]
        public InArgument<Boolean> AllowRead { get; set; }

        [RequiredArgument]
        [Input("Allow Update")]
        [Default("true")]
        public InArgument<Boolean> AllowUpdate { get; set; }


        #endregion

        
        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            
            objCommon.tracingService.Trace("Entered ShareSecuredField.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}", executionContext.ActivityInstanceId, executionContext.WorkflowInstanceId);

            #endregion

            #region "Read Parameters"
            String _RecordURL = this.RecordURL.Get(executionContext);
            if (_RecordURL == null || _RecordURL == "")
            {
                return;
            }
            string[] urlParts = _RecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string objectTypeCode = urlParams[0].Replace("etc=", "");
            string entityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
            string objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            #endregion

            #region "Clone Execution"


            ExecuteCore(executionContext, objCommon.context, objCommon.service, entityName, new Guid (objectId));


            objCommon.tracingService.Trace("OK");

            #endregion

        }

        private void ExecuteCore(CodeActivityContext executionContext, IWorkflowContext context, IOrganizationService service, string entityName, Guid entityId)
        {
            

            //string entityName = context.PrimaryEntityName;
            //Guid entityId = context.PrimaryEntityId;
            string attributeName = this.AttributeName.Get(executionContext);
            EntityReference userToShare = this.UserToShare.Get(executionContext);
            EntityReference teamToShare = this.TeamToShare.Get(executionContext);
            bool allowRead = this.AllowRead.Get(executionContext);
            bool allowUpdate = this.AllowUpdate.Get(executionContext);

            if (userToShare != null)
            {
                Guid objectId = userToShare.Id;
                ShareSecuredFieldCore(service, entityName, attributeName, entityId, objectId, allowRead, allowUpdate, false);
            }

            if (teamToShare != null)
            {
                Guid objectId = teamToShare.Id;
                ShareSecuredFieldCore(service, entityName, attributeName, entityId, objectId, allowRead, allowUpdate);
            }
        }

        private void ShareSecuredFieldCore(IOrganizationService service, string entityName, string attributeName, Guid objectId, Guid principalId, bool allowRead, bool allowUpdate, bool shareWithTeam = true)
        {
            // Create the request
            RetrieveAttributeRequest attributeRequest = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };

            // Execute the request
            RetrieveAttributeResponse attributeResponse = (RetrieveAttributeResponse)service.Execute(attributeRequest);

            if (attributeResponse.AttributeMetadata != null && attributeResponse.AttributeMetadata.IsSecured != null && attributeResponse.AttributeMetadata.IsSecured.HasValue && attributeResponse.AttributeMetadata.IsSecured.Value)
            {
                // Create the query for retrieve User Shared Attribute permissions.
                QueryExpression queryPOAA = new QueryExpression("principalobjectattributeaccess");
                queryPOAA.ColumnSet = new ColumnSet(new string[] { "readaccess", "updateaccess" });
                queryPOAA.Criteria.FilterOperator = LogicalOperator.And;
                queryPOAA.Criteria.Conditions.Add(new ConditionExpression("attributeid", ConditionOperator.Equal, attributeResponse.AttributeMetadata.MetadataId));
                queryPOAA.Criteria.Conditions.Add(new ConditionExpression("objectid", ConditionOperator.Equal, objectId));
                queryPOAA.Criteria.Conditions.Add(new ConditionExpression("principalid", ConditionOperator.Equal, principalId));

                // Execute the query.
                EntityCollection responsePOAA = service.RetrieveMultiple(queryPOAA);

                if (responsePOAA.Entities.Count > 0)
                {
                    Entity poaa = responsePOAA.Entities[0];

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
                        Entity poaa = new Entity("principalobjectattributeaccess");
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
