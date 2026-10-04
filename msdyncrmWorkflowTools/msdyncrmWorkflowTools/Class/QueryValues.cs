using Microsoft.Xrm.Sdk;
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var entityName = EntityName.Get(executionContext);
            var attribute1 = Attribute1.Get(executionContext);
            var attribute2 = Attribute2.Get(executionContext);
            var filterAttribute1 = FilterAttribute1.Get(executionContext);
            var filterAttribute2 = FilterAttribute2.Get(executionContext);
            var valueAttribute1 = ValueAttribute1.Get(executionContext);
            var valueAttribute2 = ValueAttribute2.Get(executionContext);

            objCommon.Trace(
                $"EntityName: {entityName} - Attribute1:{attribute1} - Attribute2:{attribute2} - FilterAttribute1:{filterAttribute1} - FilterAttribute2:{filterAttribute2} - ValueAttribute1:{valueAttribute1} ValueAttribute2:{valueAttribute2}");
            #endregion
            #region "QueryExpression Execution"
            var qe = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = new ColumnSet()
            };

            if (!string.IsNullOrEmpty(attribute1)) qe.ColumnSet.Columns.Add(attribute1);
            if (!string.IsNullOrEmpty(attribute2)) qe.ColumnSet.Columns.Add(attribute2);

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

            objCommon.Trace("Executing Query...");

            var results = objCommon.service.RetrieveMultiple(qe);

            objCommon.Trace($"Executed Query Ok, {results.Entities.Count} records ...");

            if (results.Entities.Count <= 0)
            {
                return;
            }

            objCommon.Trace("Setting results");
            if (results.Entities[0].Attributes.Contains(attribute1))
            {
                objCommon.Trace($"Setting result1: {results.Entities[0].Attributes[attribute1]}");

                // TODO: Is there a better way to do this?
                switch (results.Entities[0].Attributes[attribute1])
                {
                    case OptionSetValue _:
                    {
                        objCommon.Trace("Value1 Is an OptionSetValue");
                        var val = (OptionSetValue)results.Entities[0].Attributes[attribute1];
                        ResultValue1.Set(executionContext, val.Value.ToString());
                        break;
                    }
                    case EntityReference _:
                    {
                        objCommon.Trace("Value1 Is an EntityReference");
                        var val = (EntityReference)results.Entities[0].Attributes[attribute1];
                        ResultValue1.Set(executionContext, val.Id.ToString());
                        break;
                    }
                    default:
                        ResultValue1.Set(executionContext, results.Entities[0].Attributes[attribute1].ToString());
                        break;
                }
            }

            if (results.Entities[0].Attributes.Contains(attribute2))
            {
                objCommon.Trace($"Setting result2: {results.Entities[0].Attributes[attribute2]}");

                switch (results.Entities[0].Attributes[attribute2])
                {
                    case OptionSetValue _:
                    {
                        objCommon.Trace("Value2 Is an OptionSetValue");

                        var val = (OptionSetValue)results.Entities[0].Attributes[attribute2];
                        ResultValue2.Set(executionContext, val.Value.ToString());
                        break;
                    }
                    case EntityReference _:
                    {
                        objCommon.Trace("Value2 Is an EntityReference");
                        var val = (EntityReference)results.Entities[0].Attributes[attribute2];
                        ResultValue2.Set(executionContext, val.Id.ToString());
                        break;
                    }
                    default:
                        ResultValue2.Set(executionContext, results.Entities[0].Attributes[attribute2].ToString());
                        break;
                }
            }
            
            objCommon.Trace("End setting results");
            #endregion
        }
    }
}
