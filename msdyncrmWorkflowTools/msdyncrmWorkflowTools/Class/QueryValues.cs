using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class QueryValues: CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("EntityName")]
        [Default("")]
        public InArgument<String> EntityName { get; set; }

        [RequiredArgument]
        [Input("Attribute1")]
        [ReferenceTarget("")]
        public InArgument<String> Attribute1 { get; set; }

        [RequiredArgument]
        [Input("Attribute2")]
        [ReferenceTarget("")]
        public InArgument<String> Attribute2 { get; set; }

        [RequiredArgument]
        [Input("FilterAttibute1")]
        [ReferenceTarget("")]
        public InArgument<String> FilterAttribute1 { get; set; }

        [RequiredArgument]
        [Input("ValueAttribute1")]
        [ReferenceTarget("")]
        public InArgument<String> ValueAttribute1 { get; set; }

        
        [Input("FilterAttribute2")]
        [ReferenceTarget("")]
        public InArgument<String> FilterAttribute2 { get; set; }

        
        [Input("ValueAttribute2")]
        [ReferenceTarget("")]
        public InArgument<String> ValueAttribute2 { get; set; }



        [Output("ResultValue1")]
        public OutArgument<String> ResultValue1 { get; set; }


        [Output("ResultValue2")]
        public OutArgument<String> ResultValue2 { get; set; }

        
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _EntityName = this.EntityName.Get(executionContext);
            String _Attribute1 = this.Attribute1.Get(executionContext);
            String _Attribute2 = this.Attribute2.Get(executionContext);
            String _FilterAttribute1 = this.FilterAttribute1.Get(executionContext);
            String _FilterAttribute2 = this.FilterAttribute2.Get(executionContext);
            String _ValueAttribute1 = this.ValueAttribute1.Get(executionContext);
            String _ValueAttribute2 = this.ValueAttribute2.Get(executionContext);

            objCommon.tracingService.Trace(String.Format("EntityName: {0} - Attribute1:{1} - Attribute2:{2} - FilterAttribute1:{3} - FilterAttribute2:{4} - ValueAttribute1:{5} ValueAttribute2:{6}",
                _EntityName, _Attribute1, _Attribute2, _FilterAttribute1, _FilterAttribute2, _ValueAttribute1, _ValueAttribute2));
            #endregion
            try
            {
               


                #region "QueryExpression Execution"
                QueryExpression qe = new QueryExpression();
                qe.EntityName = _EntityName;
                qe.ColumnSet = new ColumnSet();
                if (_Attribute1 != null && _Attribute1 != "") qe.ColumnSet.Columns.Add(_Attribute1);
                if (_Attribute2 != null && _Attribute2 != "") qe.ColumnSet.Columns.Add(_Attribute2);

                FilterExpression filter = new FilterExpression(LogicalOperator.And);
                if (_FilterAttribute1 != null && _FilterAttribute1 != "")
                {
                    ConditionExpression condition1 = new ConditionExpression();
                    condition1.AttributeName = _FilterAttribute1;
                    condition1.Values.Add(_ValueAttribute1);
                    condition1.Operator = ConditionOperator.Equal;
                    filter.Conditions.Add(condition1);
                }
                if (_FilterAttribute2 != null && _FilterAttribute2 != "")
                {
                    ConditionExpression condition2 = new ConditionExpression();
                    condition2.AttributeName = _FilterAttribute2;
                    condition2.Values.Add(_ValueAttribute2);
                    condition2.Operator = ConditionOperator.Equal;
                    filter.Conditions.Add(condition2);
                }
                qe.Criteria = filter;

                objCommon.tracingService.Trace(String.Format("Executing Query..."));

                EntityCollection results = objCommon.service.RetrieveMultiple(qe);

                objCommon.tracingService.Trace(String.Format("Executed Query Ok, {0} records ...", results.Entities.Count));


                if (results.Entities.Count > 0)
                {
                    objCommon.tracingService.Trace(String.Format("Setting results"));
                    if (results.Entities[0].Attributes.Contains(_Attribute1))
                    {
                        objCommon.tracingService.Trace(String.Format("Setting result1: {0}", results.Entities[0].Attributes[_Attribute1]));
                        if (results.Entities[0].Attributes[_Attribute1] is Microsoft.Xrm.Sdk.OptionSetValue)
                        {
                            objCommon.tracingService.Trace("Value1 Is an OptionSetValue");
                            OptionSetValue val = (OptionSetValue)results.Entities[0].Attributes[_Attribute1];
                            this.ResultValue1.Set(executionContext, val.Value.ToString());
                        }
                        else if (results.Entities[0].Attributes[_Attribute1] is EntityReference)
                        {
                            objCommon.tracingService.Trace("Value1 Is an EntityReference");
                            EntityReference val = (EntityReference)results.Entities[0].Attributes[_Attribute1];
                            this.ResultValue1.Set(executionContext, val.Id.ToString());
                        }
                        else
                        {
                            this.ResultValue1.Set(executionContext, results.Entities[0].Attributes[_Attribute1].ToString());
                        }
                    }
                    if (results.Entities[0].Attributes.Contains(_Attribute2))
                    {
                        objCommon.tracingService.Trace(String.Format("Setting result2: {0}", results.Entities[0].Attributes[_Attribute2]));
                        if (results.Entities[0].Attributes[_Attribute2] is Microsoft.Xrm.Sdk.OptionSetValue)
                        {
                            objCommon.tracingService.Trace("Value2 Is an OptionSetValue");

                            OptionSetValue val = (OptionSetValue)results.Entities[0].Attributes[_Attribute2];
                            this.ResultValue2.Set(executionContext, val.Value.ToString());
                        }
                        else if (results.Entities[0].Attributes[_Attribute2] is EntityReference)
                        {
                            objCommon.tracingService.Trace("Value2 Is an EntityReference");
                            EntityReference val = (EntityReference)results.Entities[0].Attributes[_Attribute2];
                            this.ResultValue2.Set(executionContext, val.Id.ToString());
                        }
                        else
                        {
                            this.ResultValue2.Set(executionContext, results.Entities[0].Attributes[_Attribute2].ToString());
                        }
                    }
                    objCommon.tracingService.Trace(String.Format("End setting results"));
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                objCommon.tracingService.Trace(String.Format("error: {0} - {1}",ex.Message, ex.StackTrace));
                throw ex;

            }
        }
    }
}
