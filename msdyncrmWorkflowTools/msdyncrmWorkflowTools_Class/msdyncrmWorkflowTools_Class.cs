using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class msdyncrmWorkflowTools_Class
    {
        private IOrganizationService service;
        private ITracingService tracing;

        public msdyncrmWorkflowTools_Class(IOrganizationService _service, ITracingService _tracing)
        {
            service = _service;
            tracing = _tracing;
        }

        public msdyncrmWorkflowTools_Class(IOrganizationService _service)
        {
            service = _service;
            tracing = null;
        }

        public void QueryValues()
        {
        }


        public void DeleteOptionValue(bool globalOptionSet, string attributeName, string entityName, int optionValue)
        {
            if (globalOptionSet)
            {

                DeleteOptionValueRequest deleteOptionValueRequest =
                  new DeleteOptionValueRequest
                  {
                      OptionSetName = attributeName,
                      Value = optionValue                    
                  };
                service.Execute(deleteOptionValueRequest);
            }
            else
            {
                // Create a request.
                DeleteOptionValueRequest insertOptionValueRequest =
                   new DeleteOptionValueRequest
                   {
                       AttributeLogicalName = attributeName,
                       EntityLogicalName = entityName,
                       Value = optionValue
                   };
                service.Execute(insertOptionValueRequest);
            }

        }


        public void InsertOptionValue(bool globalOptionSet, string attributeName, string entityName, string optionText, int optionValue, int languageCode)
        {
            if (globalOptionSet)
            {
                InsertOptionValueRequest insertOptionValueRequest =
                  new InsertOptionValueRequest
                  {
                      OptionSetName= attributeName,
                      Value = optionValue,
                      Label = new Label(optionText, languageCode)
                  };
                int insertOptionValue = ((InsertOptionValueResponse)service.Execute(insertOptionValueRequest)).NewOptionValue;
            }
            else
            {
                // Create a request.
                InsertOptionValueRequest insertOptionValueRequest =
                   new InsertOptionValueRequest
                   {
                       AttributeLogicalName = attributeName,
                       EntityLogicalName = entityName,
                       Value = optionValue,
                       Label = new Label(optionText, languageCode)
                   };
                int insertOptionValue = ((InsertOptionValueResponse)service.Execute(insertOptionValueRequest)).NewOptionValue;
            }
            // Execute the request.
            
        }
        public void AssociateEntity(string PrimaryEntityName, Guid PrimaryEntityId, string _relationshipName, string _relationshipEntityName, string entityName, string ParentId)
        {
            try
            {
                EntityCollection relations = getAssociations(PrimaryEntityName, PrimaryEntityId, _relationshipName, _relationshipEntityName, entityName, ParentId);


                if (relations.Entities.Count == 0)
                {
                    EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                    relatedEntities.Add(new EntityReference(entityName, new Guid(ParentId)));
                    Relationship relationship = new Relationship(_relationshipName);
                    service.Associate(PrimaryEntityName, PrimaryEntityId, relationship, relatedEntities);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //    objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);//
                //throw ex;
                // if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                //{
                // throw ex;
                //}
            }
            
        }

        public EntityCollection getAssociations(string PrimaryEntityName, Guid PrimaryEntityId, string _relationshipName, string _relationshipEntityName, string entityName, string ParentId)
        {
            //
            string fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                      <entity name='" + PrimaryEntityName + @"'>
                                        <link-entity name='" + _relationshipEntityName + @"' from='" + PrimaryEntityName + @"id' to='" + PrimaryEntityName + @"id' visible='false' intersect='true'>
                                        <link-entity name='"+ PrimaryEntityName + @"' from='"+ PrimaryEntityName + @"id' to='"+ PrimaryEntityName + @"id' alias='ab'>
                                            <filter type='and'>
                                            <condition attribute='"+ PrimaryEntityName + @"id' operator='eq' value='"+ PrimaryEntityId.ToString()+ @"' />
                                            </filter>
                                        </link-entity>
                                        <link-entity name='" + entityName + @"' from='" + entityName + @"id' to='" + entityName + @"id' alias='ac'>
                                                <filter type='and'>
                                                  <condition attribute='" + entityName + @"id' operator='eq' value='" + ParentId + @"' />
                                                </filter>
                                              </link-entity>
                                        </link-entity>
                                      </entity>
                                    </fetch>";
            
            EntityCollection relations = service.RetrieveMultiple(new FetchExpression(fetchXML));

            return relations;
        }

        public void UpdateChildRecords(string relationshipName, string parentEntityType, string parentEntityId, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate)
        {
            //1) Get child lookup field name
            RetrieveRelationshipRequest req = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };
            RetrieveRelationshipResponse res = (RetrieveRelationshipResponse)service.Execute(req);
            OneToManyRelationshipMetadata rel = (OneToManyRelationshipMetadata)res.RelationshipMetadata;
            string childEntityType = rel.ReferencingEntity;
            string childEntityFieldName = rel.ReferencingAttribute;

            //2) retrieve all child records
            QueryByAttribute querybyattribute = new QueryByAttribute(childEntityType);
            querybyattribute.ColumnSet = new ColumnSet(childEntityFieldName);
            querybyattribute.Attributes.AddRange(childEntityFieldName);
            querybyattribute.Values.AddRange(new Guid(parentEntityId));
            EntityCollection retrieved = service.RetrieveMultiple(querybyattribute);

            //2') retrieve parent fielv value
            var valueToUpdate=new object();
            if (parentFieldNameToUpdate != null && parentFieldNameToUpdate != "")
            {
                Entity retrievedEntity = (Entity)service.Retrieve(parentEntityType, new Guid(parentEntityId), new ColumnSet(parentFieldNameToUpdate));
                if (retrievedEntity.Attributes.Contains(parentFieldNameToUpdate))
                {
                    valueToUpdate = retrievedEntity.Attributes[parentFieldNameToUpdate];
                }
                else
                {
                    valueToUpdate = null;
                }
            }
            else
            {
                valueToUpdate = setValueToUpdate;
            }

            //3) update each child record

            foreach (Entity child in retrieved.Entities)
            {
                Entity entUpdate = new Entity(childEntityType);
                entUpdate.Id = child.Id;
                entUpdate.Attributes.Add(childFieldNameToUpdate, valueToUpdate);
                service.Update(entUpdate);
            }


        }

    }
}
