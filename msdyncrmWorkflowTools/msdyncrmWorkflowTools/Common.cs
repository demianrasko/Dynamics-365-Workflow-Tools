using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools
{
    public class Common
    {
        public ITracingService tracingService;
        public IWorkflowContext context;
        public IOrganizationServiceFactory serviceFactory;
        public IOrganizationService service;

        public Common(CodeActivityContext executionContext)
        {
            tracingService = executionContext.GetExtension<ITracingService>();
            context = executionContext.GetExtension<IWorkflowContext>();
            serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            service = serviceFactory.CreateOrganizationService(context.UserId);
        }

        /// <summary>
        /// Query the Metadata to get the Entity Schema Name from the Object Type Code
        /// </summary>
        /// <param name="objectTypeCode"></param>
        /// <param name="service"></param>
        /// <returns>Entity Schema Name</returns>
        public string GetEntityNameFromCode(string objectTypeCode, IOrganizationService service)
        {
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, Convert.ToInt32(objectTypeCode)));
            
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };
            
            var request = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression,
                ClientVersionStamp = null
            };

            var response = (RetrieveMetadataChangesResponse)service.Execute(request);

            var entityMetadata = response.EntityMetadata[0];

            return entityMetadata.SchemaName.ToLower();
        }
        
        public EntityCollection GetAssociations(string primaryEntityName, Guid primaryEntityId, string relationshipName, string entityName, string parentId)
        {
            //
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                      <entity name='" + primaryEntityName + @"'>
                                        <link-entity name='" + relationshipName + @"' from='" + primaryEntityName + @"id' to='" + primaryEntityName + @"id' visible='false' intersect='true'>
                                        
                                            <filter type='and'>
                                            <condition attribute='" + primaryEntityName + @"id' operator='eq' value='" + primaryEntityId.ToString() + @"' />
                                            </filter>
                                       
                                        <link-entity name='" + entityName + @"' from='" + entityName + @"id' to='" + entityName + @"id' alias='ac'>
                                                <filter type='and'>
                                                  <condition attribute='" + entityName + @"id' operator='eq' value='" + parentId + @"' />
                                                </filter>
                                              </link-entity>
                                        </link-entity>
                                      </entity>
                                    </fetch>";
            
            tracingService.Trace($"FetchXML: {fetchXml} ");
            
            var relations = service.RetrieveMultiple(new FetchExpression(fetchXml));

            return relations;
        }

        public List<string> GetEntityAttributesToClone(string entityName, IOrganizationService service,
            ref string primaryIdAttribute, ref string primaryNameAttribute)
        {
            var atts = new List<string>();
            var request = new RetrieveEntityRequest()
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = entityName
            };

            var response = (RetrieveEntityResponse)service.Execute(request);
            primaryIdAttribute = response.EntityMetadata.PrimaryIdAttribute;

            foreach (var attMetadata in response.EntityMetadata.Attributes)
            {
                if (attMetadata.IsPrimaryName != null && attMetadata.IsPrimaryName.Value)
                {
                    primaryNameAttribute = attMetadata.LogicalName;
                }

                if (attMetadata.IsValidForCreate != null && 
                    ((!attMetadata.IsValidForCreate.Value && 
                      !attMetadata.IsValidForUpdate.Value)
                    || attMetadata.IsPrimaryId.Value))
                {
                    continue;
                }

                atts.Add(attMetadata.AttributeTypeName.Value.ToLower() == "partylisttype"
                    ? $"partylist-{attMetadata.LogicalName}"
                    : attMetadata.LogicalName);
            }

            return atts;
        }

        public Guid CloneRecord(string entityName, string objectId, string fieldstoIgnore, string prefix)
        {
            tracingService.Trace("entering CloneRecord");
            if (fieldstoIgnore == null)
            {
                fieldstoIgnore = "";
            }

            fieldstoIgnore = fieldstoIgnore.ToLower();
            tracingService.Trace($"{nameof(fieldstoIgnore)}={fieldstoIgnore}");
            
            var retrievedObject = service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            tracingService.Trace("retrieved object OK");

            var newEntity = new Entity(entityName);
            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;
            
            var attributesToClone = GetEntityAttributesToClone(entityName, service, ref primaryIdAttribute, ref primaryNameAttribute);

            foreach (var att in attributesToClone)
            {
                if (!string.IsNullOrEmpty(fieldstoIgnore))
                {
                    if (Array.IndexOf(fieldstoIgnore.Split(';'), att) >= 0 || Array.IndexOf(fieldstoIgnore.Split(','), att) >= 0)
                    {
                        continue;
                    }
                }


                if ((!retrievedObject.Attributes.Contains(att) || att == "statuscode" || att == "statecode")
                    && !att.StartsWith("partylist-"))
                {
                    continue;
                }

                EntityCollection arrPartiesNew = new EntityCollection();
                if (att.StartsWith("partylist-"))
                {
                    var att2 = att.Replace("partylist-", string.Empty);

                    var fetchParty = @"<fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='true'>
                                                <entity name='activityparty'>
                                                    <attribute name = 'partyid'/>
                                                        <filter type = 'and' >
                                                            <condition attribute = 'activityid' operator= 'eq' value = '" + objectId + @"' />
                                                            <condition attribute = 'participationtypemask' operator= 'eq' value = '" + GetParticipation(att2) + @"' />
                                                         </filter>
                                                </entity>
                                            </fetch> ";

                    var request = new RetrieveMultipleRequest
                    {
                        Query = new FetchExpression(fetchParty)
                    };

                    tracingService.Trace(fetchParty);
                    var returnCollection = ((RetrieveMultipleResponse)service.Execute(request)).EntityCollection;
                    
                    tracingService.Trace("attribute:{0}", att2);

                    var party = new Entity("activityparty");
                    foreach (var ent in returnCollection.Entities)
                    {
                        var partyid = (EntityReference)ent.Attributes["partyid"];

                        party.Attributes.Add("partyid", new EntityReference(partyid.LogicalName, partyid.Id));
                        tracingService.Trace("attribute:{0}:{1}:{2}", att2, partyid.LogicalName, partyid.Id.ToString());
                        arrPartiesNew.Entities.Add(party);
                    }

                    newEntity.Attributes.Add(att2, arrPartiesNew);
                    continue;
                }

                tracingService.Trace("attribute:{0}", att);

                if (att == primaryNameAttribute && prefix != null)
                {
                    retrievedObject.Attributes[att] = prefix + retrievedObject.Attributes[att];
                }
                
                newEntity.Attributes.Add(att, retrievedObject.Attributes[att]);
            }

            tracingService.Trace("creating cloned object...");
            var id = service.Create(newEntity);
            tracingService.Trace("created cloned object OK");

            if (newEntity.Attributes.Contains("statuscode") && newEntity.Attributes.Contains("statecode"))
            {
                var record = service.Retrieve(entityName, id, new ColumnSet("statuscode", "statecode"));


                if (retrievedObject.Attributes["statuscode"] != record.Attributes["statuscode"] ||
                    retrievedObject.Attributes["statecode"] != record.Attributes["statecode"])
                {
                    var setStatusEnt = new Entity(entityName, id);
                    setStatusEnt.Attributes.Add("statuscode", retrievedObject.Attributes["statuscode"]);
                    setStatusEnt.Attributes.Add("statecode", retrievedObject.Attributes["statecode"]);

                    service.Update(setStatusEnt);
                }
            }

            tracingService.Trace("cloned object OK");

            return id;
        }

        protected string GetParticipation(string attributeName)
        {
            var sReturn = string.Empty;

            switch (attributeName)
            {
                case "from":
                    sReturn = "1";
                    break;
                case "to":
                    sReturn = "2";
                    break;
                case "cc":
                    sReturn = "3";
                    break;
                case "bcc":
                    sReturn = "4";
                    break;
                case "organizer":
                    sReturn = "7";
                    break;
                case "requiredattendees":
                    sReturn = "5";
                    break;
                case "optionalattendees":
                    sReturn = "6";
                    break;
                case "customer":
                    sReturn = "11";
                    break;
                case "resources":
                    sReturn = "10";
                    break;
            }

            return sReturn;
        }
    }
}
