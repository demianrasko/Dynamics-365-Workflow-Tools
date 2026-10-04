using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CloneRecord : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var _ClonningRecordURL = ClonningRecordURL.Get(executionContext);
            if (string.IsNullOrEmpty(_ClonningRecordURL))
            {
                throw new InvalidPluginExecutionException("Clonning Record URL is required.");
            }
            var parsedUrl = Utility.ParseRecordUrl(_ClonningRecordURL);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var entityName = common.GetEntityNameFromCode(objectTypeCode);
            var objectId = parsedUrl.Id;
            common.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var prefix = Prefix.Get(executionContext);
            var fieldstoIgnore = FieldstoIgnore.Get(executionContext);
            #endregion

            #region "Clone Execution"

            var createdGUID = common.CloneRecord(entityName, objectId, fieldstoIgnore, prefix);
            ClonedGuid.Set(executionContext, createdGUID.ToString());

            common.Trace("cloned object OK");

            #endregion
        }
    }
}
