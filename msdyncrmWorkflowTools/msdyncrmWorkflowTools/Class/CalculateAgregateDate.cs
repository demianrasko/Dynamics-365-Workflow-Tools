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
    public class CalculateAgregateDate : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("FetchXML")]
        [Default("")]
        public InArgument<String> FetchXML { get; set; }


        [Output("Value")]
        public OutArgument<DateTime> Value { get; set; }

        [Output("Ok")]
        public OutArgument<bool> Ok { get; set; }

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

            #region "CalculateAgregateDate Execution"

            string pagingCookie = null;
            var pageNumber = 1;
            var fetchCount = 1;
            var date = new DateTime(1753, 1, 1);
            Ok.Set(executionContext, false);
            _FetchXML = _FetchXML.Replace("{PARENT_GUID}", context.PrimaryEntityId.ToString());

            objCommon.tracingService.Trace(_FetchXML);
            var xml = CreateXml(_FetchXML, pagingCookie, pageNumber, fetchCount);
            var fetchRequest1 = new RetrieveMultipleRequest
            {
                Query = new FetchExpression(xml)
            };
            var returnCollection = ((RetrieveMultipleResponse)objCommon.service.Execute(fetchRequest1)).EntityCollection;
            objCommon.tracingService.Trace(string.Format("Count {0}", returnCollection.Entities.Count));
            if (returnCollection.Entities.Count > 0)
            {
                if (returnCollection.Entities[0].Attributes.Count > 0)
                {
                    try
                    {
                        var value = returnCollection.Entities[0].Attributes.First().Value;
                        objCommon.tracingService.Trace(string.Format("Attribute {0} - {1}", returnCollection.Entities[0].Attributes.First().Key, value));
                        if (value is DateTime)
                            date = (DateTime)value;
                        if (value is Microsoft.Xrm.Sdk.AliasedValue)
                            date = (DateTime)((Microsoft.Xrm.Sdk.AliasedValue)value).Value;
                        Ok.Set(executionContext, true);
                        objCommon.tracingService.Trace(string.Format("date {0}", date));
                    }
                    catch (Exception e)
                    {
                        objCommon.tracingService.Trace(e.ToString());
                    }
                }
            }
            Value.Set(executionContext, date);
            objCommon.tracingService.Trace("Calculate Agregate Date --- Done");

            #endregion

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


    }
}