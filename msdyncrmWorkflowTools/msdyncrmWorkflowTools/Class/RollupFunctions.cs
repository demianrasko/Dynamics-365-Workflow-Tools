using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace msdyncrmWorkflowTools
{
    public class RollupFunctions : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("FetchXML")]
        [Default("")]
        public InArgument<String> FetchXML { get; set; }

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
            var _FetchXML = this.FetchXML.Get(executionContext);
            if (_FetchXML == null || _FetchXML == "")
            {
                return;
            }
            
            objCommon.tracingService.Trace("_FetchXML=" + _FetchXML);

            var context = executionContext.GetExtension<IWorkflowContext>();

            #endregion

            #region "RollupFunctions Execution"
            string pagingCookie = null;
            var recordCount = 0;
            var pageNumber = 1;
            var fetchCount = 250;
            var objNumbers = new List<object>();

            while (true)
            {
                _FetchXML = _FetchXML.Replace("{PARENT_GUID}", context.PrimaryEntityId.ToString());

                var xml = CreateXml(_FetchXML, pagingCookie, pageNumber, fetchCount);
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
                    
                    objCommon.tracingService.Trace("Value: "+ attribute.Value);
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
            
            decimal _count = 0;
            decimal _sum = 0;
            decimal _min = 0;
            decimal _max = 0;
            decimal _average = 0;
            if (objNumbers.Count > 0)
            {
                foreach (var obj in objNumbers)
                {
                    _count++;
                    var number = this.GetValue(obj);

                    _sum += number;
                    if (number < _min || _count == 1) _min = number;
                    if (number > _max || _count == 1) _max = number;
                }

                
                if (_count > 0)
                {
                    _average = _sum / _count;
                }
            }
            
            this.Count.Set(executionContext, _count);
            this.Sum.Set(executionContext, _sum);
            this.Average.Set(executionContext, _average);
            this.Min.Set(executionContext, _min);
            this.Max.Set(executionContext, _max);

            #endregion

        }
               
        public string ExtractNodeValue(XmlNode parentNode, string name)
        {
            var childNode = parentNode.SelectSingleNode(name);

            if (null == childNode)
            {
                return null;
            }
            return childNode.InnerText;
        }

        public string ExtractAttribute(XmlDocument doc, string name)
        {
            var attrs = doc.DocumentElement.Attributes;
            var attr = (XmlAttribute)attrs.GetNamedItem(name);
            if (null == attr)
            {
                return null;
            }
            return attr.Value;
        }

        public string CreateXml(string xml, string cookie, int page, int count)
        {
            var stringReader = new StringReader(xml);
            var reader = new XmlTextReader(stringReader);

            // Load document
            var doc = new XmlDocument();
            doc.Load(reader);

            return CreateXml(doc, cookie, page, count);
        }

        public string CreateXml(XmlDocument doc, string cookie, int page, int count)
        {
            var attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                var pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            var pageAttr = doc.CreateAttribute("page");
            pageAttr.Value = System.Convert.ToString(page);
            attrs.Append(pageAttr);

            var countAttr = doc.CreateAttribute("count");
            countAttr.Value = System.Convert.ToString(count);
            attrs.Append(countAttr);

            var sb = new StringBuilder(1024);
            var stringWriter = new StringWriter(sb);

            var writer = new XmlTextWriter(stringWriter);
            doc.WriteTo(writer);
            writer.Close();

            return sb.ToString();
        }

        private decimal GetValue(object obj)
        {
            if (obj is Money)
            {
                return ((Money)obj).Value;
            }
            else if (obj is decimal || obj is int || obj is long || obj is short || obj is float || obj is double)
            { 
                return Convert.ToDecimal(obj);
            }
            return 0;
            //throw new Exception("Invalid field type provided.");
        }
    }
}
