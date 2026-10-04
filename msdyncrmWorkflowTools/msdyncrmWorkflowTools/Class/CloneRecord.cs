using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class CloneRecord : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Clonning Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ClonningRecordURL { get; set; }

        [Input("Prefix")]
        [Default("")]
        public InArgument<string> Prefix { get; set; }

        [Input("Fields to Ignore")]
        [Default("")]
        public InArgument<string> FieldstoIgnore { get; set; }

        [Output("Cloned Guid")]
        public OutArgument<string> ClonedGuid { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var _ClonningRecordURL = ClonningRecordURL.Get(executionContext);
            if (string.IsNullOrEmpty(_ClonningRecordURL))
            {
                throw new InvalidPluginExecutionException("Cloning Record URL is required.");
            }
            var parsedUrl = common.ParseRecordUrl(_ClonningRecordURL);
            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            var prefix = Prefix.Get(executionContext);
            var fieldsToIgnore = FieldstoIgnore.Get(executionContext);
            #endregion

            #region "Clone Execution"

            var createdGuid = common.CloneRecord(parsedUrl.EntityName, parsedUrl.Id, fieldsToIgnore, prefix);

            ClonedGuid.Set(executionContext, createdGuid.ToString());

            common.Trace("cloned object OK");
            #endregion
        }
    }
}
