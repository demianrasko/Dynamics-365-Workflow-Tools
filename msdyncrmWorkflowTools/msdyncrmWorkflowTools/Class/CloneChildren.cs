using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

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
        public InArgument<string> SourceRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Target Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> TargetRecordUrl { get; set; }

        [RequiredArgument]
        [Input("Relationship Name")]
        [ReferenceTarget("")]
        public InArgument<string> RelationshipName { get; set; }

        [RequiredArgument]
        [Input("New Parent Field Name")]
        [ReferenceTarget("")]
        public InArgument<string> NewParentFieldNameToUpdate { get; set; }

        [Input("Old Parent Field Name")]
        [ReferenceTarget("")]
        public InArgument<string> OldParentFieldNameToUpdate { get; set; }

        [Input("Prefix")]
        [Default("")]
        public InArgument<string> Prefix { get; set; }

        [Input("Fields to Ignore")]
        [Default("")]
        public InArgument<string> FieldstoIgnore { get; set; }

        #endregion



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"

            var _relationshipName = RelationshipName.Get(executionContext);
            if (_relationshipName == null || _relationshipName == "")
            {
                return;
            }

            var _newParentFieldName = NewParentFieldNameToUpdate.Get(executionContext);
            if (_newParentFieldName == null || _newParentFieldName == "")
            {
                return;
            }

            var _source = SourceRecordUrl.Get(executionContext);
            if (_source == null || _source == "")
            {
                return;
            }

            var urlParts = _source.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var parentObjectTypeCode = urlParams[0].Replace("etc=", "");
            var parentEntityName = objCommon.GetEntityNameFromCode(parentObjectTypeCode, objCommon.service);
            var parentId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + parentObjectTypeCode + "--ParentId=" + parentId);

            var _destination = TargetRecordUrl.Get(executionContext);
            if (_destination == null || _destination == "")
            {
                return;
            }
            var destinationUrlParts = _destination.Split("?".ToArray());
            var destinationUrlParams = destinationUrlParts[1].Split("&".ToCharArray());
            var destinationObjectTypeCode = destinationUrlParams[0].Replace("etc=", "");
            var destinationEntityName = objCommon.GetEntityNameFromCode(destinationObjectTypeCode, objCommon.service);
            var destinationId = destinationUrlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + destinationObjectTypeCode + "--ParentId=" + destinationId);


            //Optional
            var _oldParentFieldName = OldParentFieldNameToUpdate.Get(executionContext);
            var prefix = Prefix.Get(executionContext);
            var fieldstoIgnore = FieldstoIgnore.Get(executionContext);

            #endregion

            var tools = new msdyncrmWorkflowTools_Class(objCommon.service);

            var children = tools.GetChildRecords(_relationshipName, parentId);
             
            foreach (var item in children.Entities)
            {
                var newRecordId = objCommon.CloneRecord(item.LogicalName, item.Id.ToString(), fieldstoIgnore, prefix);

                var update = new Entity(item.LogicalName);
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
