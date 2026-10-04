using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class CloneRecord : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Clonning Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> ClonningRecordURL { get; set; }

        
        [Input("Prefix")]
        [Default("")]
        public InArgument<String> Prefix { get; set; }

        [Input("Fields to Ignore")]
        [Default("")]
        public InArgument<String> FieldstoIgnore { get; set; }

        [Output("Cloned Guid")]
        public OutArgument<String> ClonedGuid { get; set; }

        
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
            var sReturn = "";
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

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _ClonningRecordURL = ClonningRecordURL.Get(executionContext);
            if (_ClonningRecordURL == null || _ClonningRecordURL == "")
            {
                return;
            }
            var urlParts = _ClonningRecordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", "");
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);
            var objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            var prefix = Prefix.Get(executionContext);
            var fieldstoIgnore = FieldstoIgnore.Get(executionContext);
            #endregion

            #region "Clone Execution"

            var createdGUID = objCommon.CloneRecord(entityName, objectId, fieldstoIgnore, prefix);
            ClonedGuid.Set(executionContext, createdGUID.ToString());
            

            objCommon.tracingService.Trace("cloned object OK");

            #endregion

        }

        
    }

}
