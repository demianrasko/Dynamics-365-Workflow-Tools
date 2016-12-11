using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
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
    class Common
    {
        public ITracingService tracingService;
        public IWorkflowContext context;
        public IOrganizationServiceFactory serviceFactory;
        public IOrganizationService service;

        public  Common(CodeActivityContext executionContext)
        {
            tracingService = executionContext.GetExtension<ITracingService>();
            context = executionContext.GetExtension<IWorkflowContext>();
            serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            service = serviceFactory.CreateOrganizationService(context.UserId);
        }

        /// <summary>
        /// Query the Metadata to get the Entity Schema Name from the Object Type Code
        /// </summary>
        /// <param name="ObjectTypeCode"></param>
        /// <param name="service"></param>
        /// <returns>Entity Schema Name</returns>
        public string sGetEntityNameFromCode(string ObjectTypeCode, IOrganizationService service)
        {
            MetadataFilterExpression entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, Convert.ToInt32(ObjectTypeCode)));
            EntityQueryExpression entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };
            RetrieveMetadataChangesRequest retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression,
                ClientVersionStamp = null
            };
            RetrieveMetadataChangesResponse response = (RetrieveMetadataChangesResponse)service.Execute(retrieveMetadataChangesRequest);

            EntityMetadata entityMetadata = (EntityMetadata)response.EntityMetadata[0];
            return entityMetadata.SchemaName.ToLower();
        }


        public EntityCollection getAssociations(string PrimaryEntityName, string _relationshipName, string entityName, string ParentId)
        {
            //
            string fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                      <entity name='" + PrimaryEntityName + @"'>
                                        <link-entity name='" + _relationshipName + @"' from='" + PrimaryEntityName + @"id' to='" + PrimaryEntityName + @"id' visible='false' intersect='true'>
                                         <link-entity name='" + entityName + @"' from='" + entityName + @"id' to='" + entityName + @"id' alias='ac'>
                                                <filter type='and'>
                                                  <condition attribute='" + entityName + @"id' operator='eq' value='" + ParentId + @"' />
                                                </filter>
                                              </link-entity>
                                        </link-entity>
                                      </entity>
                                    </fetch>";
            tracingService.Trace(String.Format("FetchXML: {0} ", fetchXML));
            EntityCollection relations = service.RetrieveMultiple(new FetchExpression(fetchXML));

            return relations;
        }

        public List<string> getEntityAttributesToClone(string entityName, IOrganizationService service, 
            ref string PrimaryIdAttribute)
        {
            

            List<string> atts = new List<string>();
            RetrieveEntityRequest req = new RetrieveEntityRequest() {
                EntityFilters=EntityFilters.Attributes, 
                LogicalName= entityName
            };

            RetrieveEntityResponse res = (RetrieveEntityResponse)service.Execute(req);
            PrimaryIdAttribute = res.EntityMetadata.PrimaryIdAttribute;

            foreach (AttributeMetadata attMetadata in res.EntityMetadata.Attributes)
            {
                if ((attMetadata.IsValidForCreate.Value ||attMetadata.IsValidForUpdate.Value) 
                    && !attMetadata.IsPrimaryId.Value)
                {
                    //tracingService.Trace("Tipo:{0}", attMetadata.AttributeTypeName.Value.ToLower());
                    if (attMetadata.AttributeTypeName.Value.ToLower()== "partylisttype")
                    {
                        atts.Add("partylist-"+attMetadata.LogicalName);
                        //atts.Add(attMetadata.LogicalName);
                    }
                    else
                    {
                        atts.Add(attMetadata.LogicalName);
                    }
                }
            }

            return (atts);
        }

       
    }
}
