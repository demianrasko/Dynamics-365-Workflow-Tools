using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Reads an environment variable: its current value, or its default value when no current value is set.
    /// </summary>
    [ActivityName("Get Environment Variable")]
    public class GetEnvironmentVariable : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Schema Name")]
        public InArgument<string> SchemaName { get; set; }

        [Output("Value")]
        public OutArgument<string> Value { get; set; }

        [Output("Found")]
        public OutArgument<bool> Found { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var value = common.GetEnvironmentVariable(SchemaName.Get(executionContext));

            Value.Set(executionContext, value ?? string.Empty);
            Found.Set(executionContext, value != null);
        }
    }
}
