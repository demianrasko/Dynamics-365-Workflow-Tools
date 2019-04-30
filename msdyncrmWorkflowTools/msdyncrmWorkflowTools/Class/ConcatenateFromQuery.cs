using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace msdyncrmWorkflowTools
{
    public class ConcatenateFromQuery : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("FetchXML")]
        [Default("")]
        public InArgument<string> FetchXml { get; set; }

        [Input("AttributeName")]
        [Default("")]
        public InArgument<string> AttributeName { get; set; }

        [Input("Separator")]
        [Default(", ")]
        public InArgument<string> Separator { get; set; }

        [Input("FormatString")]
        [Default("")]
        public InArgument<string> FormatString { get; set; }


        [Output("ConcatenatedString")]
        public OutArgument<string> ConcatenatedString { get; set; }
        
        #endregion


        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("ConcatenateFromQuery -- Start!");
            #endregion

            #region "Read Parameters"
            var fetchXml = FetchXml.Get(executionContext);
            if (string.IsNullOrEmpty(fetchXml))
            {
                return;
            }
            objCommon.tracingService.Trace($"FetchXML={fetchXml}");

            var attributeFieldName = AttributeName.Get(executionContext);
            objCommon.tracingService.Trace($"AttributeName={attributeFieldName}");

            var separator = Separator.Get(executionContext);
            objCommon.tracingService.Trace($"Separator={separator}");

            var format = FormatString.Get(executionContext);
            objCommon.tracingService.Trace($"FormatString={format}");

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            #endregion

            #region "Concatenation Execution"
            string pagingCookie = null;
            
            bool hasMoreRecords = false;
            bool canPerformPaging = fetchXml.IndexOf("top=", StringComparison.CurrentCultureIgnoreCase) < 0;
            int pageNumber = canPerformPaging ? 1 : 0;
            int fetchCount = canPerformPaging ? 250 : 0;
            List<string> stringValues = new List<string>();
            do
            {
                objCommon.tracingService.Trace($"Fetch PageNumber={pageNumber}");

                fetchXml = fetchXml.Replace("{PARENT_GUID}", context.PrimaryEntityId.ToString());

                string xml = CreateXml(fetchXml, pagingCookie, pageNumber, fetchCount);
                RetrieveMultipleRequest fetchRequest1 = new RetrieveMultipleRequest
                {
                    Query = new FetchExpression(xml)
                };
                EntityCollection returnCollection =
                    ((RetrieveMultipleResponse) objCommon.service.Execute(fetchRequest1)).EntityCollection;
                bool attributeNamesSentToTrace = false;
                foreach (var entity in returnCollection.Entities)
                {
                    if (!entity.Attributes.Any())
                    {
                        continue;
                    }

                    if (!attributeNamesSentToTrace)
                    {
                        var attributeNames = entity.Attributes.Select(a => a.Key).Aggregate((x, y) => x + "," + y);
                        objCommon.tracingService.Trace($"List of attributes available: {attributeNames}");
                        attributeNamesSentToTrace = true;
                    }

                    object attribute = null;
                    if (!string.IsNullOrEmpty(attributeFieldName))
                    {
                        if (entity.Attributes.ContainsKey(attributeFieldName))
                        {
                            attribute = entity.Attributes[attributeFieldName];

                        }
                    }
                    else
                    {
                        attribute = entity.Attributes.First().Value;
                    }

                    if (attribute == null)
                    {
                        continue;
                    }

                    if (attribute is AliasedValue)
                    {
                        attribute = ((AliasedValue) attribute).Value;
                    }

                    if (attribute is EntityReference)
                    {
                        attribute = ((EntityReference) attribute).Name;
                    }
                    else if (attribute is Money)
                    {
                        attribute = ((Money) attribute).Value;
                    }
                    else if (attribute is OptionSetValue)
                    {
                        attribute = ((OptionSetValue) attribute).Value;
                        if (entity.FormattedValues.ContainsKey(attributeFieldName))
                        {
                            attribute = entity.FormattedValues[attributeFieldName];
                        }
                    }

                    var attributeValueAsString = string.Format($"{{0:{format}}}", attribute);
                    stringValues.Add(attributeValueAsString);
                }

                if (canPerformPaging && returnCollection.MoreRecords)
                {
                    pageNumber++;
                    pagingCookie = returnCollection.PagingCookie;
                    hasMoreRecords = returnCollection.MoreRecords;
                }
            } while (hasMoreRecords);

            if (stringValues.Any())
            {
                var concatenatedString = stringValues.Aggregate((x, y) => x + separator + y);
                objCommon.tracingService.Trace($"Concatenated string: {concatenatedString}");
                ConcatenatedString.Set(executionContext,concatenatedString);
            }
            else
            {
                objCommon.tracingService.Trace("No data found to concatenate");
            }

            objCommon.tracingService.Trace("ConcatenateFromQuery -- Done!");

            #endregion

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
            if (doc.DocumentElement == null)
            {
                return string.Empty;
            }
            XmlAttributeCollection attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                XmlAttribute pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            if (page > 0)
            {
                XmlAttribute pageAttr = doc.CreateAttribute("page");
                pageAttr.Value = System.Convert.ToString(page);
                attrs.Append(pageAttr);
            }

            if (count > 0)
            {
                XmlAttribute countAttr = doc.CreateAttribute("count");
                countAttr.Value = System.Convert.ToString(count);
                attrs.Append(countAttr);
            }

            StringBuilder sb = new StringBuilder(1024);
            StringWriter stringWriter = new StringWriter(sb);

            XmlTextWriter writer = new XmlTextWriter(stringWriter);
            doc.WriteTo(writer);
            writer.Close();

            return sb.ToString();
        }


    }

}
