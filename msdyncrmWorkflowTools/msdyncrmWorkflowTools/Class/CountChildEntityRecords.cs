using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CountChildEntityRecords : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Child Entity Schema Name")]
        [Default("")]
        public InArgument<string> ChildEntityName { get; set; }

        [RequiredArgument]
        [Input("Parent Lookup Field Name on Child")]
        [Default("")]
        public InArgument<string> ParentLookupName { get; set; }

        [RequiredArgument]
        [Input("Record URL (Parent)")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        [Output("Result")]
        public OutArgument<int> Result { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var _childEntityName = ChildEntityName.Get(executionContext);
            var _parentLookupName = ParentLookupName.Get(executionContext);
            var _recordURL = RecordURL.Get(executionContext);
            common.Trace($"ChildEntityName={_childEntityName}--ParentLookupName={_parentLookupName}--RecordURL={_recordURL}");
            if (_recordURL == null || _recordURL == string.Empty)
            {
                throw new InvalidPluginExecutionException("Record URL (Parent) is required.");
            }
            var parsedUrl = Utility.ParseRecordUrl(_recordURL);
            var ParentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var ParenEntityName = common.GetEntityNameFromCode(ParentObjectTypeCode);
            var ParentEntityId = parsedUrl.Id;
            common.Trace($"ParentObjectTypeCode={ParentObjectTypeCode}--ParentId={ParentEntityId}");
            #endregion

            #region "Process"

            var count = common.CountRecords(Queries.ChildRecords(_childEntityName, _parentLookupName, new Guid(ParentEntityId)));
            common.Trace($"{_childEntityName} records with {_parentLookupName} = {ParentEntityId}: {count}");

            Result.Set(executionContext, count);
            #endregion
        }
    }
}
