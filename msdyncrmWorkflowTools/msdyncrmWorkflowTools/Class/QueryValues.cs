using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

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
            #region "QueryExpression Execution"
            var qe = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = new ColumnSet(),
                TopCount = 1
            };

            if (!string.IsNullOrEmpty(attribute1))
            {
                qe.ColumnSet.Columns.Add(attribute1);
            }
            if (!string.IsNullOrEmpty(attribute2))
            {
                qe.ColumnSet.Columns.Add(attribute2);
            }

            var filter = new FilterExpression(LogicalOperator.And);

            if (!string.IsNullOrEmpty(filterAttribute1))
            {
                var condition1 = new ConditionExpression
                {
                    AttributeName = filterAttribute1
                };

                condition1.Values.Add(valueAttribute1);
                condition1.Operator = ConditionOperator.Equal;
                filter.Conditions.Add(condition1);
            }

            if (!string.IsNullOrEmpty(filterAttribute2))
            {
                var condition2 = new ConditionExpression
                {
                    AttributeName = filterAttribute2
                };

                condition2.Values.Add(valueAttribute2);
                condition2.Operator = ConditionOperator.Equal;
                filter.Conditions.Add(condition2);
            }

            qe.Criteria = filter;

            common.Trace("Executing Query...");

            var results = common.Service.RetrieveMultiple(qe);

            common.Trace($"Executed Query Ok, {results.Entities.Count} records ...");

            if (results.Entities.Count <= 0)
            {
                return;
            }

            common.Trace("Setting results");
            var record = results.Entities[0];

            if (!string.IsNullOrEmpty(attribute1) && record.Attributes.Contains(attribute1))
            {
                var value1 = Utility.AttributeValueToString(record.Attributes[attribute1]);
                common.Trace($"Setting result1: {value1}");
                ResultValue1.Set(executionContext, value1);
            }

            if (!string.IsNullOrEmpty(attribute2) && record.Attributes.Contains(attribute2))
            {
                var value2 = Utility.AttributeValueToString(record.Attributes[attribute2]);
                common.Trace($"Setting result2: {value2}");
                ResultValue2.Set(executionContext, value2);
            }

            common.Trace("End setting results");
            #endregion
        }
    }
}
