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


        public EntityCollection getAssociations(string PrimaryEntityName, Guid PrimaryEntityId, string _relationshipName, string entityName, string ParentId)
        {
            //
            string fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                      <entity name='" + PrimaryEntityName + @"'>
                                        <link-entity name='" + _relationshipName + @"' from='" + PrimaryEntityName + @"id' to='" + PrimaryEntityName + @"id' visible='false' intersect='true'>
                                        
                                            <filter type='and'>
                                            <condition attribute='" + PrimaryEntityName + @"id' operator='eq' value='" + PrimaryEntityId.ToString() + @"' />
                                            </filter>
                                       
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
            ref string PrimaryIdAttribute, ref string PrimaryNameAttribute)
        {


            List<string> atts = new List<string>();
            RetrieveEntityRequest req = new RetrieveEntityRequest()
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = entityName
            };

            RetrieveEntityResponse res = (RetrieveEntityResponse)service.Execute(req);
            PrimaryIdAttribute = res.EntityMetadata.PrimaryIdAttribute;

            foreach (AttributeMetadata attMetadata in res.EntityMetadata.Attributes)
            {
                if (attMetadata.IsPrimaryName.Value)
                {
                    PrimaryNameAttribute = attMetadata.LogicalName;
                }
                if ((attMetadata.IsValidForCreate.Value || attMetadata.IsValidForUpdate.Value)
                    && !attMetadata.IsPrimaryId.Value)
                {
                    //tracingService.Trace("Tipo:{0}", attMetadata.AttributeTypeName.Value.ToLower());
                    if (attMetadata.AttributeTypeName.Value.ToLower() == "partylisttype")
                    {
                        atts.Add("partylist-" + attMetadata.LogicalName);
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

        public Guid CloneRecord(string entityName, string objectId, string fieldstoIgnore, string prefix)
        {
            tracingService.Trace("entering CloneRecord");
            if (fieldstoIgnore == null) fieldstoIgnore = "";
            fieldstoIgnore = fieldstoIgnore.ToLower();
            tracingService.Trace("fieldstoIgnore="+ fieldstoIgnore);
            Entity retrievedObject = service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            tracingService.Trace("retrieved object OK");

            Entity newEntity = new Entity(entityName);
            string PrimaryIdAttribute = "";
            string PrimaryNameAttribute = "";
            List<string> atts = getEntityAttributesToClone(entityName, service, ref PrimaryIdAttribute, ref PrimaryNameAttribute);



            foreach (string att in atts)
            {
                if (fieldstoIgnore != null && fieldstoIgnore != "")
                {
                    if (Array.IndexOf(fieldstoIgnore.Split(';'), att) >= 0 || Array.IndexOf(fieldstoIgnore.Split(','), att) >= 0)
                    {
                        continue;
                    }
                }


                if (retrievedObject.Attributes.Contains(att) && att != "statuscode" && att != "statecode"
                    || att.StartsWith("partylist-"))
                {
                    if (att.StartsWith("partylist-"))
                    {
                        string att2 = att.Replace("partylist-", "");

                        string fetchParty = @"<fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='true'>
                                                <entity name='activityparty'>
                                                    <attribute name = 'partyid'/>
                                                        <filter type = 'and' >
                                                            <condition attribute = 'activityid' operator= 'eq' value = '" + objectId + @"' />
                                                            <condition attribute = 'participationtypemask' operator= 'eq' value = '" + getParticipation(att2) + @"' />
                                                         </filter>
                                                </entity>
                                            </fetch> ";

                        RetrieveMultipleRequest fetchRequest1 = new RetrieveMultipleRequest
                        {
                            Query = new FetchExpression(fetchParty)
                        };
                        tracingService.Trace(fetchParty);
                        EntityCollection returnCollection = ((RetrieveMultipleResponse)service.Execute(fetchRequest1)).EntityCollection;


                        EntityCollection arrPartiesNew = new EntityCollection();
                        tracingService.Trace("attribute:{0}", att2);

                        foreach (Entity ent in returnCollection.Entities)
                        {
                            Entity party = new Entity("activityparty");
                            EntityReference partyid = (EntityReference)ent.Attributes["partyid"];


                            party.Attributes.Add("partyid", new EntityReference(partyid.LogicalName, partyid.Id));
                            tracingService.Trace("attribute:{0}:{1}:{2}", att2, partyid.LogicalName, partyid.Id.ToString());
                            arrPartiesNew.Entities.Add(party);
                        }

                        newEntity.Attributes.Add(att2, arrPartiesNew);
                        continue;

                    }

                    tracingService.Trace("attribute:{0}", att);
                    if (att == PrimaryNameAttribute && prefix != null)
                    {
                        retrievedObject.Attributes[att] = prefix + retrievedObject.Attributes[att];
                    }
                    newEntity.Attributes.Add(att, retrievedObject.Attributes[att]);
                }
            }

            tracingService.Trace("creating cloned object...");
            Guid createdGUID = service.Create(newEntity);
            tracingService.Trace("created cloned object OK");

            if (newEntity.Attributes.Contains("statuscode") && newEntity.Attributes.Contains("statecode"))
            {
                Entity record = service.Retrieve(entityName, createdGUID, new ColumnSet("statuscode", "statecode"));


                if (retrievedObject.Attributes["statuscode"] != record.Attributes["statuscode"] ||
                    retrievedObject.Attributes["statecode"] != record.Attributes["statecode"])
                {
                    Entity setStatusEnt = new Entity(entityName, createdGUID);
                    setStatusEnt.Attributes.Add("statuscode", retrievedObject.Attributes["statuscode"]);
                    setStatusEnt.Attributes.Add("statecode", retrievedObject.Attributes["statecode"]);

                    service.Update(setStatusEnt);
                }
            }

            tracingService.Trace("cloned object OK");
            return createdGUID;
        }

        protected string getParticipation(string attributeName)
        {
            string sReturn = "";
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
            /*Sender  1
                Specifies the sender.

                ToRecipient
                2
                Specifies the recipient in the To field.

                CCRecipient
                3
                Specifies the recipient in the Cc field.

                BccRecipient
                4
                Specifies the recipient in the Bcc field.

                RequiredAttendee
                5
                Specifies a required attendee.

                OptionalAttendee
                6
                Specifies an optional attendee.

                Organizer
                7
                Specifies the activity organizer.

                Regarding
                8
                Specifies the regarding item.

                Owner
                9
                Specifies the activity owner.

                Resource
                10
                Specifies a resource.

                Customer
                11

            */
        }



    }
}
