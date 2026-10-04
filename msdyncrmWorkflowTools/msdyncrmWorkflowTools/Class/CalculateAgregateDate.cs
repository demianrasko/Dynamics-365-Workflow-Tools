using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Linq;

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var fetchXml = FetchXML.Get(executionContext);
            if (string.IsNullOrEmpty(fetchXml))
            {
                throw new InvalidPluginExecutionException("FetchXML is required.");
            }

            common.Trace($"_FetchXML={fetchXml}");

            var context = executionContext.GetExtension<IWorkflowContext>();

            #endregion

            #region "CalculateAgregateDate Execution"

           // string pagingCookie = null;
            const int pageNumber = 1;
            const int fetchCount = 1;
            var date = new DateTime(1753, 1, 1);

            Ok.Set(executionContext, false);

            fetchXml = fetchXml.Replace("{PARENT_GUID}", context.PrimaryEntityId.ToString());

            common.Trace(fetchXml);
            var xml = Utility.CreateXml(fetchXml, null, pageNumber, fetchCount);

            var request = new RetrieveMultipleRequest
            {
                Query = new FetchExpression(xml)
            };

            var returnCollection = ((RetrieveMultipleResponse)common.service.Execute(request)).EntityCollection;

            common.Trace($"Count {returnCollection.Entities.Count}");

            if (returnCollection.Entities.Count > 0)
            {
                if (returnCollection.Entities[0].Attributes.Count > 0)
                {
                    try
                    {
                        var value = returnCollection.Entities[0].Attributes.First().Value;
                        common.Trace($"Attribute {returnCollection.Entities[0].Attributes.First().Key} - {value}");

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
                        common.Trace($"date {date}");
                    }
                    catch (Exception e)
                    {
                        // Deliberately not rethrown: the "Ok" output stays false and the workflow decides what to do.
                        common.Trace(Utility.HandleExceptions(e));
                    }
                }
            }

            Value.Set(executionContext, date);
            common.Trace("Calculate Aggregate Date --- Done");

            #endregion
        }
    }
}