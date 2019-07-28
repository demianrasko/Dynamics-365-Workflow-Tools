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
            
            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _FetchXML = this.FetchXML.Get(executionContext);
            if (_FetchXML == null || _FetchXML == "")
            {
                return;
            }
            
            objCommon.tracingService.Trace("_FetchXML=" + _FetchXML);

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            #endregion

            #region "RollupFunctions Execution"
            string pagingCookie = null;
            int recordCount = 0;
            int pageNumber = 1;
            int fetchCount = 250;
            List<object> objNumbers = new List<object>();

            while (true)
            {
                _FetchXML = _FetchXML.Replace("{PARENT_GUID}", context.PrimaryEntityId.ToString());

                string xml = CreateXml(_FetchXML, pagingCookie, pageNumber, fetchCount);
                RetrieveMultipleRequest fetchRequest1 = new RetrieveMultipleRequest
                {
                    Query = new FetchExpression(xml)
                };
                EntityCollection returnCollection = ((RetrieveMultipleResponse)objCommon.service.Execute(fetchRequest1)).EntityCollection;
                foreach (var c in returnCollection.Entities)
                {
                    KeyValuePair<string, object> attribute=new KeyValuePair<string, object>();
                    foreach (KeyValuePair<string,object> att in c.Attributes)
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
            if (objNumbers.Count <= 0)
            {
                foreach (object obj in objNumbers)
                {
                    _count++;
                    decimal number = this.GetValue(obj);

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
            XmlNode childNode = parentNode.SelectSingleNode(name);

            if (null == childNode)
            {
                return null;
            }
            return childNode.InnerText;
        }

        public string ExtractAttribute(XmlDocument doc, string name)
        {
            XmlAttributeCollection attrs = doc.DocumentElement.Attributes;
            XmlAttribute attr = (XmlAttribute)attrs.GetNamedItem(name);
            if (null == attr)
            {
                return null;
            }
            return attr.Value;
        }

        public string CreateXml(string xml, string cookie, int page, int count)
        {
            StringReader stringReader = new StringReader(xml);
            XmlTextReader reader = new XmlTextReader(stringReader);

            // Load document
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);

            return CreateXml(doc, cookie, page, count);
        }

        public string CreateXml(XmlDocument doc, string cookie, int page, int count)
        {
            XmlAttributeCollection attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                XmlAttribute pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            XmlAttribute pageAttr = doc.CreateAttribute("page");
            pageAttr.Value = System.Convert.ToString(page);
            attrs.Append(pageAttr);

            XmlAttribute countAttr = doc.CreateAttribute("count");
            countAttr.Value = System.Convert.ToString(count);
            attrs.Append(countAttr);

            StringBuilder sb = new StringBuilder(1024);
            StringWriter stringWriter = new StringWriter(sb);

            XmlTextWriter writer = new XmlTextWriter(stringWriter);
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
            else if (obj is decimal)
            { 
                return Convert.ToDecimal(obj);
            }
            
            throw new Exception("Invalid field type provided.");
        }
    }
}
