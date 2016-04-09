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
    public class CloneRecord : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Clonning Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> ClonningRecordURL { get; set; }


        #endregion

        /*private EntityCollection getActivityObject(Entity entNewActivity, string activityFieldName)
        {
            Entity partyToFrom = new Entity("activityparty");
            partyToFrom["partyid"] = ((EntityReference)((EntityCollection)entNewActivity[activityFieldName]).Entities[0].Attributes["partyid"]);

            EntityCollection toFrom = new EntityCollection();
            toFrom.Entities.Add(partyToFrom);

            return toFrom;
        }*/

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

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _ClonningRecordURL = this.ClonningRecordURL.Get(executionContext);
            if (_ClonningRecordURL == null || _ClonningRecordURL == "")
            {
                return;
            }
            string[] urlParts = _ClonningRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string objectTypeCode = urlParams[0].Replace("etc=", "");
            string entityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
            string objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);


            #endregion

            #region "Clone Execution"

            Entity retrievedObject=objCommon.service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            objCommon.tracingService.Trace("retrieved object OK");

            Entity newEntity = new Entity(entityName);
            string PrimaryIdAttribute = "";
            List<string> atts= objCommon.getEntityAttributesToClone(entityName, objCommon.service, ref PrimaryIdAttribute);

            
            foreach (string att in atts)
            {
               
                if (retrievedObject.Attributes.Contains(att) && att!="statuscode" && att!="statecode"
                    || att.StartsWith("partylist-"))
                {
                    if (att.StartsWith("partylist-"))
                    {
                        string att2=att.Replace("partylist-", "");

                        string fetchParty = @"<fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='true'>
                                                <entity name='activityparty'>
                                                    <attribute name = 'partyid'/>
                                                        <filter type = 'and' >
                                                            <condition attribute = 'activityid' operator= 'eq' value = '" + objectId + @"' />
                                                            <condition attribute = 'participationtypemask' operator= 'eq' value = '" + getParticipation(att2) +@"' />
                                                         </filter>
                                                </entity>
                                            </fetch> ";

                        RetrieveMultipleRequest fetchRequest1 = new RetrieveMultipleRequest
                        {
                            Query = new FetchExpression(fetchParty)
                        };
                        objCommon.tracingService.Trace(fetchParty);
                        EntityCollection returnCollection = ((RetrieveMultipleResponse)objCommon.service.Execute(fetchRequest1)).EntityCollection;
                        
                        
                        EntityCollection arrPartiesNew=new EntityCollection();
                        objCommon.tracingService.Trace("attribute:{0}", att2);

                        foreach (Entity ent in returnCollection.Entities)
                        {
                            Entity party = new Entity("activityparty");
                            EntityReference partyid = (EntityReference)ent.Attributes["partyid"];


                            party.Attributes.Add("partyid", new EntityReference(partyid.LogicalName, partyid.Id));
                            objCommon.tracingService.Trace("attribute:{0}:{1}:{2}", att2, partyid.LogicalName, partyid.Id.ToString());
                            arrPartiesNew.Entities.Add(party);
                        }
                        
                        newEntity.Attributes.Add(att2, arrPartiesNew);
                        continue;
                        
                    }

                    objCommon.tracingService.Trace("attribute:{0}", att);
                    newEntity.Attributes.Add(att, retrievedObject.Attributes[att]);
                }
            }
            //throw new Exception("error demian");



            //foreach (KeyValuePair<string,object> att in retrievedObject.Attributes)
            //{
            //    if (atts.Contains(att.Key[0].ToString()))
            //    {
            //        newEntity.Attributes.Add(att.Key,att.Value);
            //    }
            //}
            objCommon.tracingService.Trace("creting cloned object...");
            Guid createdGUID=objCommon.service.Create(newEntity);
            objCommon.tracingService.Trace("created cloned object OK");

            Entity record = objCommon.service.Retrieve(entityName, createdGUID, new ColumnSet("statuscode", "statecode"));


            if (retrievedObject.Attributes["statuscode"] != record.Attributes["statuscode"] ||
                retrievedObject.Attributes["statecode"] != record.Attributes["statecode"])
            {
                Entity setStatusEnt = new Entity(entityName,createdGUID);
                setStatusEnt.Attributes.Add("statuscode", retrievedObject.Attributes["statuscode"]);
                setStatusEnt.Attributes.Add("statecode", retrievedObject.Attributes["statecode"]);

                objCommon.service.Update(setStatusEnt);
            }

            objCommon.tracingService.Trace("cloned object OK");

            #endregion

        }

        
    }

}
