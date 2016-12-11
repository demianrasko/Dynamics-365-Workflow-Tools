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


        //string relationshipName, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate
        //string parentEntityId, string parentEntityType, 

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _ParentRecordURL = this.ParentRecordURL.Get(executionContext);
            if (_ParentRecordURL == null || _ParentRecordURL == "")
            {
                return;
            }
            string[] urlParts = _ParentRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string objectTypeCode = urlParams[0].Replace("etc=", "");
            string parentEntityType = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
            string parentEntityId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + parentEntityId);

            String _RelationshipName = this.RelationshipName.Get(executionContext);
            String _ParentFieldNameToUpdate = this.ParentFieldNameToUpdate.Get(executionContext);
            String _ValueToSet = this.ValueToSet.Get(executionContext);
            String _ChildFieldNameToUpdate = this.ChildFieldNameToUpdate.Get(executionContext);

            objCommon.tracingService.Trace("RelationshipName=" + _RelationshipName + "--_ParentFieldNameToUpdate=" + _ParentFieldNameToUpdate);
            objCommon.tracingService.Trace("_ValueToSet=" + _ValueToSet + "--_ChildFieldNameToUpdate=" + _ChildFieldNameToUpdate);
            #endregion

            msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service);
            commonClass.UpdateChildRecords(_RelationshipName, parentEntityType, parentEntityId, _ParentFieldNameToUpdate, _ValueToSet, _ChildFieldNameToUpdate);
            
        }
    }
}
