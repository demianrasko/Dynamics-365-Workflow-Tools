using System;
using System.Activities;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class EntityJsonSerializer : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Serializing Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> SerializingRecordURL { get; set; }

        [Output("Output Json")] public OutArgument<string> OutputJson { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");

            #endregion

            #region "Read Parameters"

            var serializingRecordUrl = SerializingRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(serializingRecordUrl))
            {
                return;
            }

            var parsedUrl = Utility.ParseRecordUrl(serializingRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var objectId = parsedUrl.Id;
            var entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);

            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            #endregion

            #region "Clone Execution"

            var retrievedObject =
                objCommon.service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            objCommon.tracingService.Trace("retrieved object OK");

            //var newEntity = new Entity(entityName);
            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;
            var attributesToClone = objCommon.GetEntityAttributesToClone(entityName, objCommon.service,
                ref primaryIdAttribute, ref primaryNameAttribute);

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

                objCommon.tracingService.Trace("attribute:{0}", att);
            }

            sJson.Append("}}");
            objCommon.tracingService.Trace("json object OK");
            OutputJson.Set(executionContext, sJson.ToString());

            #endregion
        }
    }
}
