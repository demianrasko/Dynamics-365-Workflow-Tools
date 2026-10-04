using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk.Workflow;

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
        public InArgument<string> DeleteRecordURL { get; set; }

        [Input("Entity Type Name")]
        [ReferenceTarget("")]
        public InArgument<string> EntityTypeName { get; set; }

        [Input("Entity Guid")]
        [ReferenceTarget("")]
        public InArgument<string> EntityGuid { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var deleteRecordUrl = DeleteRecordURL.Get(executionContext);
            var entityName = string.Empty;
            var objectId = string.Empty;

            if (deleteRecordUrl != null)
            {
                var urlParts = deleteRecordUrl.Split("?".ToArray());
                var urlParams = urlParts[1].Split("&".ToCharArray());
                var objectTypeCode = urlParams[0].Replace("etc=", string.Empty);
                entityName = objCommon.GetEntityNameFromCode(objectTypeCode, objCommon.service);
                objectId = urlParams[1].Replace("id=", string.Empty);
                objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);
            }

            var deleteUsingRecordUrl = DeleteUsingRecordURL.Get(executionContext);
            var entityTypeName = EntityTypeName.Get(executionContext);
            var entityGuid = EntityGuid.Get(executionContext);

            #endregion

            #region "Delete Record Execution"

            if (deleteUsingRecordUrl)
            {
                objCommon.tracingService.Trace("Deleting record by URL: {0}", deleteRecordUrl);

                if (string.IsNullOrEmpty(deleteRecordUrl) )
                {
                    throw new InvalidOperationException("ERROR: Delete Record URL to be deleted missing.");
                }
                objCommon.service.Delete(entityName, new Guid (objectId));
            }
            else
            {
                objCommon.tracingService.Trace("Record type to be deleted: "+ entityTypeName+" and ID:"+ entityGuid);
                if (string.IsNullOrEmpty(entityTypeName) || entityGuid == null || entityGuid == string.Empty)
                {
                    throw new InvalidOperationException("ERROR: Entity Type name or GUID to be deleted missing.");
                }

                objCommon.tracingService.Trace("Deleting record by Guid: {0}-{1}", entityTypeName, entityGuid);
                objCommon.service.Delete(entityTypeName, new Guid (entityGuid));
            }

            #endregion
        }
    }
}
