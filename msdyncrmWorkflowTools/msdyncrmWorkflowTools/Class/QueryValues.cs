using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public class QueryValues: WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("EntityName")]
        [Default("")]
        public InArgument<string> EntityName { get; set; }

        [RequiredArgument]
        [Input("Attribute1")]
        [ReferenceTarget("")]
        public InArgument<string> Attribute1 { get; set; }

        [RequiredArgument]
        [Input("Attribute2")]
        [ReferenceTarget("")]
        public InArgument<string> Attribute2 { get; set; }

        [RequiredArgument]
        [Input("FilterAttibute1")]
        [ReferenceTarget("")]
        public InArgument<string> FilterAttribute1 { get; set; }

        [RequiredArgument]
        [Input("ValueAttribute1")]
        [ReferenceTarget("")]
        public InArgument<string> ValueAttribute1 { get; set; }

        [Input("FilterAttribute2")]
        [ReferenceTarget("")]
        public InArgument<string> FilterAttribute2 { get; set; }

        [Input("ValueAttribute2")]
        [ReferenceTarget("")]
        public InArgument<string> ValueAttribute2 { get; set; }

        [Output("ResultValue1")]
        public OutArgument<string> ResultValue1 { get; set; }

        [Output("ResultValue2")]
        public OutArgument<string> ResultValue2 { get; set; }

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var entityName = EntityName.Get(executionContext);
            var attribute1 = Attribute1.Get(executionContext);
            var attribute2 = Attribute2.Get(executionContext);
            var filterAttribute1 = FilterAttribute1.Get(executionContext);
            var filterAttribute2 = FilterAttribute2.Get(executionContext);
            var valueAttribute1 = ValueAttribute1.Get(executionContext);
            var valueAttribute2 = ValueAttribute2.Get(executionContext);

            common.Trace(
                $"EntityName: {entityName} - Attribute1:{attribute1} - Attribute2:{attribute2} - FilterAttribute1:{filterAttribute1} - FilterAttribute2:{filterAttribute2} - ValueAttribute1:{valueAttribute1} ValueAttribute2:{valueAttribute2}");
            #endregion

            var query = Queries.FirstMatch(entityName,
                new[] { attribute1, attribute2 },
                new KeyValuePair<string, object>(filterAttribute1, valueAttribute1),
                new KeyValuePair<string, object>(filterAttribute2, valueAttribute2));

            var record = common.Service.RetrieveMultiple(query).Entities.FirstOrDefault();

            if (record == null)
            {
                common.Trace("No matching record.");
                return;
            }

            if (!string.IsNullOrEmpty(attribute1) && record.Contains(attribute1))
            {
                ResultValue1.Set(executionContext, Utility.AttributeValueToString(record[attribute1]));
            }

            if (!string.IsNullOrEmpty(attribute2) && record.Contains(attribute2))
            {
                ResultValue2.Set(executionContext, Utility.AttributeValueToString(record[attribute2]));
            }
        }
    }
}
