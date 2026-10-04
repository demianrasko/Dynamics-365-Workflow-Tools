using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class EntityJsonSerializer : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Serializing Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> SerializingRecordURL { get; set; }


        [Output("Output Json")]
        public OutArgument<string> OutputJson { get; set; }

        #endregion



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _SerializingRecordURL = SerializingRecordURL.Get(executionContext);
            if (_SerializingRecordURL == null || _SerializingRecordURL == "")
            {
                return;
            }
            var urlParts = _SerializingRecordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", "");
            var entityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
            var objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);


            #endregion

            #region "Clone Execution"

            var retrievedObject=objCommon.service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            objCommon.tracingService.Trace("retrieved object OK");

            var newEntity = new Entity(entityName);
            var PrimaryIdAttribute = "" ;
            var PrimaryNameAttribute = "";
            var atts= objCommon.getEntityAttributesToClone(entityName, objCommon.service, ref PrimaryIdAttribute, ref PrimaryNameAttribute);

            var sJson = new StringBuilder("{\""+ entityName + "\": {");
            
            sJson.Append("\""+ PrimaryIdAttribute + "\": \""+ objectId + "\"");
            foreach (var att in atts)
            {
                if (retrievedObject.Attributes.Contains(att))
                {
                    
                    sJson.Append(",");
                    
                    
                    var t = retrievedObject.Attributes[att].GetType();

                    if  (t.Equals(typeof(string)))
                    {
                        sJson.Append("\"" + att + "\" : \"" + retrievedObject.Attributes[att].ToString().Replace("\\","\\\\") + "\"");   
                    }
                    else if (t.Equals(typeof(bool)))
                    {
                        sJson.Append("\"" + att + "\" : " + retrievedObject.Attributes[att].ToString().ToLower() + "");
                    }
                    else if (t.Equals(typeof(OptionSetValue)))
                    {
                        var obj = (OptionSetValue)retrievedObject.Attributes[att];
                        sJson.Append("\"" + att + "\" : " + obj.Value);
                    }
                    else if (t.Equals(typeof(Money)))
                    {
                        var obj=(Money)retrievedObject.Attributes[att];
                        sJson.Append("\"" + att + "\" : " + obj.Value);
                    }
                    else if (t.Equals(typeof(EntityReference)))
                    {
                        var obj=(EntityReference)retrievedObject.Attributes[att];
                        sJson.Append("\"" + att + "\" : { \"typename\" : \"" + obj.LogicalName.ToLower() + "\", \"id\" :\""+ obj.Id.ToString()+"\", \"name\":\""+obj.Name+"\" }");
                    }
                    else 
                    {
                        sJson.Append("\"" + att + "\" : " + retrievedObject.Attributes[att]);
                    }
                    objCommon.tracingService.Trace("attribute:{0}", att);
                }
                
            }
            sJson.Append("}}");
            objCommon.tracingService.Trace("json object OK");
            OutputJson.Set(executionContext, sJson.ToString());

            #endregion

        }
       

    }


}
