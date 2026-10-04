using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

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
    public class CloneChildren : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"

            var relationshipName = RelationshipName.Get(executionContext);
            if (string.IsNullOrEmpty(relationshipName))
            {
                throw new InvalidPluginExecutionException("Relationship Name is required.");
            }

            var newParentFieldName = NewParentFieldNameToUpdate.Get(executionContext);
            if (string.IsNullOrEmpty(newParentFieldName))
            {
                throw new InvalidPluginExecutionException("New Parent Field Name is required.");
            }

            var source = SourceRecordUrl.Get(executionContext);
            if (string.IsNullOrEmpty(source))
            {
                throw new InvalidPluginExecutionException("Source Record URL is required.");
            }

            var parsedUrl = common.ParseRecordUrl(source);
            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            var destination = TargetRecordUrl.Get(executionContext);
            if (string.IsNullOrEmpty(destination))
            {
                throw new InvalidPluginExecutionException("Target Record URL is required.");
            }
            var parsedDestinationUrl = common.ParseRecordUrl(destination);
            common.Trace($"EntityName={parsedDestinationUrl.EntityName}--Id={parsedDestinationUrl.Id}");

            //Optional
            var oldParentFieldName = OldParentFieldNameToUpdate.Get(executionContext);
            var prefix = Prefix.Get(executionContext);
            var fieldstoIgnore = FieldstoIgnore.Get(executionContext);
            #endregion

            var children = common.GetChildRecords(relationshipName, parsedUrl.Id);

            foreach (var item in children.Entities)
            {
                var newRecordId = common.CloneRecord(item.LogicalName, item.Id, fieldstoIgnore, prefix);

                var update = new Entity(item.LogicalName);
                update.Id = newRecordId;
                update.Attributes.Add(newParentFieldName, parsedDestinationUrl.ToEntityReference());
                if (!string.IsNullOrEmpty(oldParentFieldName) && oldParentFieldName != newParentFieldName)
                {
                    update.Attributes.Add(oldParentFieldName, null);
                }

                common.Service.Update(update);
            }
        }
    }
}
