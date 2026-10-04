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
    public class UpdateChildRecords : CodeActivity
    {

        [RequiredArgument]
        [Input("Parent Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> ParentRecordURL { get; set; }

        [RequiredArgument]
        [Input("Relationship Name")]
        [ReferenceTarget("")]
        public InArgument<String> RelationshipName { get; set; }

        [Input("Parent Field Name")]
        [ReferenceTarget("")]
        public InArgument<String> ParentFieldNameToUpdate { get; set; }

        [Input("Value to Set")]
        [ReferenceTarget("")]
        public InArgument<String> ValueToSet{ get; set; }

        [RequiredArgument]
        [Input("Child Field Name to Update")]
        [ReferenceTarget("")]
        public InArgument<String> ChildFieldNameToUpdate { get; set; }

        [RequiredArgument]
        [Input("Update only Active")]
        public InArgument<Boolean> UpdateonlyActive { get; set; }

        //string relationshipName, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate
        //string parentEntityId, string parentEntityType, 

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _ParentRecordURL = this.ParentRecordURL.Get(executionContext);
            if (_ParentRecordURL == null || _ParentRecordURL == "")
            {
                return;
            }
            var urlParts = _ParentRecordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", "");
            var parentEntityType = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
            var parentEntityId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + parentEntityId);

            var _RelationshipName = this.RelationshipName.Get(executionContext);
            var _ParentFieldNameToUpdate = this.ParentFieldNameToUpdate.Get(executionContext);
            var _ValueToSet = this.ValueToSet.Get(executionContext);
            var _ChildFieldNameToUpdate = this.ChildFieldNameToUpdate.Get(executionContext);
            var _UpdateonlyActive = this.UpdateonlyActive.Get(executionContext);

            objCommon.tracingService.Trace("RelationshipName=" + _RelationshipName + "--_ParentFieldNameToUpdate=" + _ParentFieldNameToUpdate);
            objCommon.tracingService.Trace("_ValueToSet=" + _ValueToSet + "--_ChildFieldNameToUpdate=" + _ChildFieldNameToUpdate);
            #endregion

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service);
            commonClass.UpdateChildRecords(_RelationshipName, parentEntityType, parentEntityId, _ParentFieldNameToUpdate, _ValueToSet, _ChildFieldNameToUpdate, _UpdateonlyActive);
            
        }
    }
}
