using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class RollupFunctions : CodeActivity
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

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"
            
            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var fetchXml = FetchXML.Get(executionContext);
            
            if (string.IsNullOrEmpty(fetchXml))
            {
                return;
            }
            
            objCommon.tracingService.Trace("_FetchXML=" + fetchXml);

            var context = executionContext.GetExtension<IWorkflowContext>();

            #endregion

            #region "RollupFunctions Execution"
            const int fetchCount = 250;
            string pagingCookie = null;
            //var recordCount = 0;
            var pageNumber = 1;
            var objNumbers = new List<object>();

            while (true)
            {
                fetchXml = fetchXml.Replace("{PARENT_GUID}", context.PrimaryEntityId.ToString());

                var xml = CreateXml(fetchXml, pagingCookie, pageNumber, fetchCount);
                var fetchRequest1 = new RetrieveMultipleRequest
                {
                    Query = new FetchExpression(xml)
                };

                var returnCollection = ((RetrieveMultipleResponse)objCommon.service.Execute(fetchRequest1)).EntityCollection;
                
                foreach (var c in returnCollection.Entities)
                {
                    var attribute=new KeyValuePair<string, object>();
                
                    foreach (var att in c.Attributes)
                    {
                        attribute = att;
                        break;
                    }
                    
                    objCommon.tracingService.Trace($"Value: {attribute.Value}");
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
            
            objCommon.tracingService.Trace("Query Data --- Done");
            
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

                    if (number < min || count == 1) min = number;
                    if (number > max || count == 1) max = number;
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

        public string CreateXml(string xml, string cookie, int page, int count)
        {
            var stringReader = new StringReader(xml);
            var reader = new XmlTextReader(stringReader);

            var doc = new XmlDocument();
            doc.Load(reader);

            return CreateXml(doc, cookie, page, count);
        }

        public string CreateXml(XmlDocument doc, string cookie, int page, int count)
        {
            if (doc.DocumentElement == null)
            {
                return string.Empty;
            }

            var attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                var pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            var pageAttr = doc.CreateAttribute("page");
            pageAttr.Value = Convert.ToString(page);
            attrs.Append(pageAttr);

            var countAttr = doc.CreateAttribute("count");
            countAttr.Value = Convert.ToString(count);
            attrs.Append(countAttr);

            var sb = new StringBuilder(1024);
            var stringWriter = new StringWriter(sb);

            var writer = new XmlTextWriter(stringWriter);
            doc.WriteTo(writer);
            writer.Close();

            return sb.ToString();
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
