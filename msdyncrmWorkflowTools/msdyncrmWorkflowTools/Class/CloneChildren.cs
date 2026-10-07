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
    [ActivityName("Clone Children")]
    public class CloneChildren : WorkflowActivityBase
    {
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

        [Input("Copy Status")]
        [Default("False")]
        public InArgument<bool> CopyStatus { get; set; }

        [Input("Only Active Children")]
        [Default("False")]
        public InArgument<bool> OnlyActiveChildren { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.CloneChildren(SourceRecordUrl.Get(executionContext), TargetRecordUrl.Get(executionContext), RelationshipName.Get(executionContext),
                NewParentFieldNameToUpdate.Get(executionContext), OldParentFieldNameToUpdate.Get(executionContext), Prefix.Get(executionContext),
                FieldstoIgnore.Get(executionContext), CopyStatus.Get(executionContext), OnlyActiveChildren.Get(executionContext));
        }
    }
}
