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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _SerializingRecordURL = this.SerializingRecordURL.Get(executionContext);
            if (_SerializingRecordURL == null || _SerializingRecordURL == "")
            {
                return;
            }
            string[] urlParts = _SerializingRecordURL.Split("?".ToArray());
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
            string PrimaryIdAttribute = "" ;
            string PrimaryNameAttribute = "";
            List<string> atts= objCommon.getEntityAttributesToClone(entityName, objCommon.service, ref PrimaryIdAttribute, ref PrimaryNameAttribute);

            StringBuilder sJson = new StringBuilder("{\""+ entityName + "\": {");
            
            sJson.Append("\""+ PrimaryIdAttribute + "\": \""+ objectId + "\"");
            foreach (string att in atts)
            {
                if (retrievedObject.Attributes.Contains(att))
                {
                    
                    sJson.Append(",");
                    
                    
                    Type t = retrievedObject.Attributes[att].GetType();

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
                        OptionSetValue obj = (OptionSetValue)retrievedObject.Attributes[att];
                        sJson.Append("\"" + att + "\" : " + obj.Value);
                    }
                    else if (t.Equals(typeof(Money)))
                    {
                        Money obj=(Money)retrievedObject.Attributes[att];
                        sJson.Append("\"" + att + "\" : " + obj.Value);
                    }
                    else if (t.Equals(typeof(EntityReference)))
                    {
                        EntityReference obj=(EntityReference)retrievedObject.Attributes[att];
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
            this.OutputJson.Set(executionContext, sJson.ToString());

            #endregion

        }
       

    }


}
