using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
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
            common.Trace("ChildEntityName=" + _childEntityName + "--ParentLookupName=" + _parentLookupName + "--RecordURL=" + _recordURL);
            if (_recordURL == null || _recordURL == string.Empty)
            {
                throw new InvalidPluginExecutionException("Record URL (Parent) is required.");
            }
            var parsedUrl = Utility.ParseRecordUrl(_recordURL);
            var ParentObjectTypeCode = parsedUrl.ObjectTypeCode;
            var ParenEntityName = common.GetEntityNameFromCode(ParentObjectTypeCode);
            var ParentEntityId = parsedUrl.Id;
            common.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentEntityId);
            #endregion

            #region "Process"

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                <entity name='{0}'>                                                                           
                                <filter type='and'>
                                    <condition attribute='{1}' operator='eq' value='{2}' />                                                                                 
                                    </filter>
                                </entity>
                            </fetch>";
            fetchXml = string.Format(fetchXml, _childEntityName, _parentLookupName, ParentEntityId);
            common.Trace($"FetchXML: {fetchXml} ");
            var results = common.service.RetrieveMultiple(new FetchExpression(fetchXml));

            Result.Set(executionContext, results.Entities.Count);
            #endregion
        }
    }
}
