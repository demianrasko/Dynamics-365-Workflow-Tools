using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Xml;

namespace msdyncrmWorkflowTools
{
    public class RollupFunctions : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("FetchXML")]
        [Default("")]
        public InArgument<string> FetchXML { get; set; }

        [Output("Count")]
        public OutArgument<decimal> Count { get; set; }

        [Output("Sum")]
        public OutArgument<decimal> Sum { get; set; }

        [Output("Average")]
        public OutArgument<decimal> Average { get; set; }

        [Output("Max")]
        public OutArgument<decimal> Max { get; set; }

        [Output("Min")]
        public OutArgument<decimal> Min { get; set; }
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
            #endregion

            #region "RollupFunctions Execution"
            const int fetchCount = 250;
            string pagingCookie = null;
            //var recordCount = 0;
            var pageNumber = 1;
            var objNumbers = new List<object>();

            while (true)
            {
                fetchXml = fetchXml.Replace("{PARENT_GUID}", common.Context.PrimaryEntityId.ToString());

                var xml = Utility.CreateXml(fetchXml, pagingCookie, pageNumber, fetchCount);
                var request = new RetrieveMultipleRequest
                {
                    Query = new FetchExpression(xml)
                };

                var returnCollection = ((RetrieveMultipleResponse)common.Service.Execute(request)).EntityCollection;

                foreach (var c in returnCollection.Entities)
                {
                    var attribute=new KeyValuePair<string, object>();

                    foreach (var att in c.Attributes)
                    {
                        attribute = att;
                        break;
                    }

                    common.Trace($"Value: {attribute.Value}");
                    objNumbers.Add(attribute.Value);
                }
                if (returnCollection.MoreRecords)
                {
                    pageNumber++;
                    pagingCookie = returnCollection.PagingCookie;
                }
                else
                {
                    break;
                }
            }

            common.Trace("Query Data --- Done");

            decimal count = 0;
            decimal sum = 0;
            decimal min = 0;
            decimal max = 0;
            decimal average = 0;

            if (objNumbers.Count > 0)
            {
                foreach (var obj in objNumbers)
                {
                    count++;
                    var number = GetValue(obj);

                    sum += number;

                    if (number < min || count == 1)
                    {
                        min = number;
                    }
                    if (number > max || count == 1)
                    {
                        max = number;
                    }
                }

                if (count > 0)
                {
                    average = sum / count;
                }
            }

            Count.Set(executionContext, count);
            Sum.Set(executionContext, sum);
            Average.Set(executionContext, average);
            Min.Set(executionContext, min);
            Max.Set(executionContext, max);
            #endregion
        }

        public string ExtractNodeValue(XmlNode parentNode, string name)
        {
            var childNode = parentNode.SelectSingleNode(name);

            return childNode?.InnerText;
        }

        public string ExtractAttribute(XmlDocument doc, string name)
        {
            if (doc.DocumentElement == null)
            {
                return string.Empty;
            }

            var attrs = doc.DocumentElement.Attributes;
            var attr = (XmlAttribute)attrs.GetNamedItem(name);

            return attr?.Value;
        }

        private static decimal GetValue(object obj)
        {
            switch (obj)
            {
                case Money money:
                    return money.Value;
                case decimal _:
                case int _:
                case long _:
                case short _:
                case float _:
                case double _:
                    return Convert.ToDecimal(obj);
                default:
                    return 0;
                    //throw new Exception("Invalid field type provided.");
            }
        }
    }
}
