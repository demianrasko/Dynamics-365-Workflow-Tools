using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class CloneRecord : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Clonning Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ClonningRecordURL { get; set; }

        
        [Input("Prefix")]
        [Default("")]
        public InArgument<string> Prefix { get; set; }

        [Input("Fields to Ignore")]
        [Default("")]
        public InArgument<string> FieldstoIgnore { get; set; }

        [Output("Cloned Guid")]
        public OutArgument<string> ClonedGuid { get; set; }

        
        #endregion

        /*private EntityCollection getActivityObject(Entity entNewActivity, string activityFieldName)
        {
            Entity partyToFrom = new Entity("activityparty");
            partyToFrom["partyid"] = ((EntityReference)((EntityCollection)entNewActivity[activityFieldName]).Entities[0].Attributes["partyid"]);

            EntityCollection toFrom = new EntityCollection();
            toFrom.Entities.Add(partyToFrom);

            return toFrom;
        }*/


        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _ClonningRecordURL = ClonningRecordURL.Get(executionContext);
            if (_ClonningRecordURL == null || _ClonningRecordURL == string.Empty)
            {
                return;
            }
            var parsedUrl = Utility.ParseRecordUrl(_ClonningRecordURL);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode);
            var objectId = parsedUrl.Id;
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
