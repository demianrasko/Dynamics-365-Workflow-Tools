using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
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
    /// <summary>
    /// Clones Child records 
    /// 
    /// 1. Takes all the child record from "Source Record URL" using "Relationship Name"
    /// 2. Creates a clone of all the child records
    /// 3. Reparents the child records by blanking "Old Parent Field" and setting the "New Parent Field Name" Lookup to "Target Record URL"
    /// 
    /// Note: "Old Parent Field" is optional if the new parent relationship is with the same entity / lookup field
    /// 
    /// </summary>
    public class CloneChildren : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Source Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> SourceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Target Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> TargetRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Relationship Name")]
        [ReferenceTarget("")]
        public InArgument<String> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("New Parent Field Name")]
        [ReferenceTarget("")]
        public InArgument<String> NewParentFieldNameToUpdate { get; set; }

        [Input("Old Parent Field Name")]
        [ReferenceTarget("")]
        public InArgument<String> OldParentFieldNameToUpdate { get; set; }

        [Input("Prefix")]
        [Default("")]
        public InArgument<String> Prefix { get; set; }

        [Input("Fields to Ignore")]
        [Default("")]
        public InArgument<String> FieldstoIgnore { get; set; }

        #endregion



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"

            String _relationshipName = this.RelationshipName.Get(executionContext);
            if (_relationshipName == null || _relationshipName == "")
            {
                return;
            }

            String _newParentFieldName = this.NewParentFieldNameToUpdate.Get(executionContext);
            if (_newParentFieldName == null || _newParentFieldName == "")
            {
                return;
            }

            String _source = this.SourceRecordUrl.Get(executionContext);
            if (_source == null || _source == "")
            {
                return;
            }

            string[] urlParts = _source.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string parentObjectTypeCode = urlParams[0].Replace("etc=", "");
            string parentEntityName = objCommon.sGetEntityNameFromCode(parentObjectTypeCode, objCommon.service);
            string parentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + parentObjectTypeCode + "--ParentId=" + parentId);

            String _destination = this.TargetRecordUrl.Get(executionContext);
            if (_destination == null || _destination == "")
            {
                return;
            }
            string[] destinationUrlParts = _destination.Split("?".ToArray());
            string[] destinationUrlParams = destinationUrlParts[1].Split("&".ToCharArray());
            string destinationObjectTypeCode = destinationUrlParams[0].Replace("etc=", "");
            string destinationEntityName = objCommon.sGetEntityNameFromCode(destinationObjectTypeCode, objCommon.service);
            string destinationId = destinationUrlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + destinationObjectTypeCode + "--ParentId=" + destinationId);


            //Optional
            String _oldParentFieldName = this.OldParentFieldNameToUpdate.Get(executionContext);
            string prefix = this.Prefix.Get(executionContext);
            string fieldstoIgnore = this.FieldstoIgnore.Get(executionContext);

            #endregion

            var tools = new msdyncrmWorkflowTools_Class(objCommon.service);

            var children = tools.GetChildRecords(_relationshipName, parentId);
             
            foreach (var item in children.Entities)
            {
                var newRecordId = objCommon.CloneRecord(item.LogicalName, item.Id.ToString(), fieldstoIgnore, prefix);

                Entity update = new Entity(item.LogicalName);
                update.Id = newRecordId;
                update.Attributes.Add(_newParentFieldName, new EntityReference(destinationEntityName, new Guid(destinationId)));
                if (!string.IsNullOrEmpty(_oldParentFieldName) && _oldParentFieldName != _newParentFieldName)
                {
                    update.Attributes.Add(_oldParentFieldName, null);
                }

                objCommon.service.Update(update);

            }
            

        }



    }

}
