using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Linq;
using System.Text;

namespace msdyncrmWorkflowTools
{
    public class EntityJsonSerializer : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Serializing Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> SerializingRecordURL { get; set; }

        [Output("Output Json")] public OutArgument<string> OutputJson { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"

            var serializingRecordUrl = SerializingRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(serializingRecordUrl))
            {
                return;
            }

            var parsedUrl = Utility.ParseRecordUrl(serializingRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var objectId = parsedUrl.Id;
            var entityName = common.GetEntityNameFromCode(objectTypeCode);

            common.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            #endregion

            #region "Clone Execution"

            var retrievedObject =
                common.service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            common.Trace("retrieved object OK");

            //var newEntity = new Entity(entityName);
            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;
            var attributesToClone = common.GetEntityAttributesToClone(entityName, ref primaryIdAttribute, ref primaryNameAttribute);

            var sJson = new StringBuilder("{\"" + entityName + "\": {");

            sJson.Append("\"" + primaryIdAttribute + "\": \"" + objectId + "\"");

            foreach (var att in attributesToClone.Where(att => retrievedObject.Attributes.Contains(att)))
            {
                sJson.Append(",");

                // TODO: Needs unit tests, if not already created    
                var t = retrievedObject.Attributes[att].GetType();

                if (t == typeof(string))
                {
                    sJson.Append("\"" + att + "\" : \"" +
                                 retrievedObject.Attributes[att].ToString().Replace("\\", "\\\\") + "\"");
                }
                else if (t == typeof(bool))
                {
                    sJson.Append("\"" + att + "\" : " + retrievedObject.Attributes[att].ToString().ToLower() + string.Empty);
                }
                else if (t == typeof(OptionSetValue))
                {
                    var obj = (OptionSetValue)retrievedObject.Attributes[att];
                    sJson.Append("\"" + att + "\" : " + obj.Value);
                }
                else if (t == typeof(Money))
                {
                    var obj = (Money)retrievedObject.Attributes[att];
                    sJson.Append("\"" + att + "\" : " + obj.Value);
                }
                else if (t == typeof(EntityReference))
                {
                    var obj = (EntityReference)retrievedObject.Attributes[att];
                    sJson.Append("\"" + att + "\" : { \"typename\" : \"" + obj.LogicalName.ToLower() +
                                 "\", \"id\" :\"" + obj.Id.ToString() + "\", \"name\":\"" + obj.Name + "\" }");
                }
                else
                {
                    sJson.Append("\"" + att + "\" : " + retrievedObject.Attributes[att]);
                }

                common.Trace("attribute:{0}", att);
            }

            sJson.Append("}}");
            common.Trace("json object OK");
            OutputJson.Set(executionContext, sJson.ToString());

            #endregion
        }
    }
}
