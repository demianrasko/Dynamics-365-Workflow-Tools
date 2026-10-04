using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class CalculateAgregateDate : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("FetchXML")]
        [Default("")]
        public InArgument<string> FetchXML { get; set; }

        [Output("Value")]
        public OutArgument<DateTime> Value { get; set; }

        [Output("Ok")]
        public OutArgument<bool> Ok { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var fetchXml = FetchXML.Get(executionContext);
            if (string.IsNullOrEmpty(fetchXml))
            {
                return;
            }

            objCommon.Trace("_FetchXML=" + fetchXml);

            var context = executionContext.GetExtension<IWorkflowContext>();

            #endregion

            #region "CalculateAgregateDate Execution"

           // string pagingCookie = null;
            const int pageNumber = 1;
            const int fetchCount = 1;
            var date = new DateTime(1753, 1, 1);

            Ok.Set(executionContext, false);

            fetchXml = fetchXml.Replace("{PARENT_GUID}", context.PrimaryEntityId.ToString());

            objCommon.Trace(fetchXml);
            var xml = Utility.CreateXml(fetchXml, null, pageNumber, fetchCount);

            var fetchRequest1 = new RetrieveMultipleRequest
            {
                Query = new FetchExpression(xml)
            };
            
            var returnCollection = ((RetrieveMultipleResponse)objCommon.service.Execute(fetchRequest1)).EntityCollection;
            
            objCommon.Trace($"Count {returnCollection.Entities.Count}");

            if (returnCollection.Entities.Count > 0)
            {
                if (returnCollection.Entities[0].Attributes.Count > 0)
                {
                    try
                    {
                        var value = returnCollection.Entities[0].Attributes.First().Value;
                        objCommon.Trace($"Attribute {returnCollection.Entities[0].Attributes.First().Key} - {value}");
            
                        switch (value)
                        {
                            case DateTime time:
                                date = time;
                                break;
                            case AliasedValue aliasedValue:
                                date = (DateTime)aliasedValue.Value;
                                break;
                        }

                        Ok.Set(executionContext, true);
                        objCommon.Trace($"date {date}");
                    }
                    catch (Exception e)
                    {
                        // Deliberately not rethrown: the "Ok" output stays false and the workflow decides what to do.
                        objCommon.Trace(Utility.HandleExceptions(e));
                    }
                }
            }

            Value.Set(executionContext, date);
            objCommon.Trace("Calculate Aggregate Date --- Done");

            #endregion
        }
    }
}