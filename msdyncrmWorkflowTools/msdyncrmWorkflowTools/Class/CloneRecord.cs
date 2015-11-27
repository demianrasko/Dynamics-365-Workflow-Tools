using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
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

            List<string> atts= objCommon.getEntityAttributesToClone(entityName, objCommon.service);

            
            foreach (string att in atts)
            {
               
                if (retrievedObject.Attributes.Contains(att))
                {
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

            objCommon.service.Create(newEntity);
            objCommon.tracingService.Trace("cloned object OK");

            #endregion

        }

        
    }

}
