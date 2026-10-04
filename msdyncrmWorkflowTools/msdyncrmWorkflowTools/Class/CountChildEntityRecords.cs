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
        public InArgument<string> RecordUrl { get; set; }

        [Input("FetchXML Filter (Child)")]
        [ReferenceTarget("")]
        public InArgument<string> FilterExpressionXml { get; set; }

        [Output("Result")]
        public OutArgument<int> Result { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var childEntityName = ChildEntityName.Get(executionContext);
            var parentLookupName = ParentLookupName.Get(executionContext);
            var recordUrl = RecordUrl.Get(executionContext);
            common.Trace($"ChildEntityName={childEntityName}--ParentLookupName={parentLookupName}--RecordURL={recordUrl}");

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL (Parent) is required.");
            }
            var parsedUrl = common.ParseRecordUrl(recordUrl);
            var parentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var parenEntityName = parsedUrl.EntityName;
            var parentEntityId = parsedUrl.Id;

            common.Trace($"ParentObjectTypeCode={parentObjectTypeCode}--ParentId={parentEntityId}");
            #endregion

            #region "Process"

            var filterExpressionXml = FilterExpressionXml.Get(executionContext);
            var query = string.IsNullOrWhiteSpace(filterExpressionXml)
                ? Queries.ChildRecords(childEntityName, parentLookupName, new Guid(parentEntityId))
                : common.FetchXmlToQueryExpression(Queries.ChildRecordsFetchXml(childEntityName, parentLookupName, new Guid(parentEntityId), filterExpressionXml));

            var count = common.CountRecords(query);
            common.Trace($"{childEntityName} records with {parentLookupName} = {parentEntityId}: {count}");

            Result.Set(executionContext, count);
            #endregion
        }
    }
}
