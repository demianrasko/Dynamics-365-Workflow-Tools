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


namespace msdyncrmWorkflowTools.Class
{
    public class DeleteRecord : CodeActivity
    {
        [RequiredArgument]
        [Input("Delete Using Record URL")]
        [Default("True")]
        public InArgument<bool> DeleteUsingRecordURL { get; set; }

        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> DeleteRecordURL { get; set; }

        [Input("Entity Type Name")]
        [ReferenceTarget("")]
        public InArgument<String> EntityTypeName { get; set; }

        [Input("Entity Guid")]
        [ReferenceTarget("")]
        public InArgument<String> EntityGuid { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _deleteRecordURL = this.DeleteRecordURL.Get(executionContext);
            string entityName = "";
            string objectId = "";
            if (_deleteRecordURL != null)
            {
                string[] urlParts = _deleteRecordURL.Split("?".ToArray());
                string[] urlParams = urlParts[1].Split("&".ToCharArray());
                string objectTypeCode = urlParams[0].Replace("etc=", "");
                entityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
                objectId = urlParams[1].Replace("id=", "");
                objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);
            }
            bool _deleteUsingRecordURL = this.DeleteUsingRecordURL.Get(executionContext);
            String _entityTypeName = this.EntityTypeName.Get(executionContext);
            String _entityGuid = this.EntityGuid.Get(executionContext);

            #endregion

            #region "Delete Record Execution"

            if (_deleteUsingRecordURL)
            {
                objCommon.tracingService.Trace("Deleting record by URL: {0}", _deleteRecordURL);

                if (_deleteRecordURL == null || _deleteRecordURL == "" )
                {
                    throw new InvalidOperationException("ERROR: Delete Record URL to be deleted missing.");
                }
                objCommon.service.Delete(entityName, new Guid (objectId));
            }
            else
            {
                objCommon.tracingService.Trace("Record type to be deleted: "+ _entityTypeName+" and ID:"+ _entityGuid);
                if (_entityTypeName == null || _entityTypeName == "" || _entityGuid == null || _entityGuid == "")
                {
                    throw new InvalidOperationException("ERROR: Entity Type name or GUID to be deleted missing.");
                }
                objCommon.tracingService.Trace("Deleting record by Guid: {0}-{1}", _entityTypeName, _entityGuid);
                objCommon.service.Delete(_entityTypeName, new Guid (_entityGuid));
            }


            #endregion

        }
    }
}
