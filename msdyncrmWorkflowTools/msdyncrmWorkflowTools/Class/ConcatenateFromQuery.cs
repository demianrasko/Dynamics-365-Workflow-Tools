using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public class ConcatenateFromQuery : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Load CRM Service from context"
            objCommon.Trace("ConcatenateFromQuery -- Start!");
            #endregion

            #region "Read Parameters"
            var fetchXml = FetchXml.Get(executionContext);
            if (string.IsNullOrEmpty(fetchXml))
            {
                return;
            }

            objCommon.Trace($"FetchXML={fetchXml}");

            var attributeFieldName = AttributeName.Get(executionContext);
            objCommon.Trace($"AttributeName={attributeFieldName}");

            var separator = Separator.Get(executionContext);
            objCommon.Trace($"Separator={separator}");

            var format = FormatString.Get(executionContext);
            objCommon.Trace($"FormatString={format}");

            var context = executionContext.GetExtension<IWorkflowContext>();

            #endregion

            #region "Concatenation Execution"
            string pagingCookie = null;
            
            var hasMoreRecords = false;
            var canPerformPaging = fetchXml.IndexOf("top=", StringComparison.CurrentCultureIgnoreCase) < 0;
            var pageNumber = canPerformPaging ? 1 : 0;
            var fetchCount = canPerformPaging ? 250 : 0;
            var stringValues = new List<string>();
            do
            {
                objCommon.Trace($"Fetch PageNumber={pageNumber}");

                fetchXml = fetchXml.Replace("{PARENT_GUID}", context.PrimaryEntityId.ToString());

                var xml = Utility.CreateXml(fetchXml, pagingCookie, pageNumber, fetchCount);
                var fetchRequest1 = new RetrieveMultipleRequest
                {
                    Query = new FetchExpression(xml)
                };

                var returnCollection = ((RetrieveMultipleResponse) objCommon.service.Execute(fetchRequest1)).EntityCollection;
                var attributeNamesSentToTrace = false;

                foreach (var entity in returnCollection.Entities)
                {
                    if (!entity.Attributes.Any())
                    {
                        continue;
                    }

                    if (!attributeNamesSentToTrace)
                    {
                        var attributeNames = entity.Attributes.Select(a => a.Key).Aggregate((x, y) => x + "," + y);
                        objCommon.Trace($"List of attributes available: {attributeNames}");
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

                    switch (attribute)
                    {
                        case null:
                            continue;
                        case AliasedValue value:
                            attribute = value.Value;
                            break;
                    }

                    switch (attribute)
                    {
                        case EntityReference reference:
                            attribute = reference.Name;
                            break;
                        case Money money:
                            attribute = money.Value;
                            break;
                        case OptionSetValue value:
                        {
                            attribute = value.Value;
                            if (entity.FormattedValues.ContainsKey(attributeFieldName))
                            {
                                attribute = entity.FormattedValues[attributeFieldName];
                            }
                            break;
                        }
                    }

                    var attributeValueAsString = string.Format($"{attribute:format}");
                    stringValues.Add(attributeValueAsString);
                }

                if (!canPerformPaging || !returnCollection.MoreRecords)
                {
                    continue;
                }

                pageNumber++;
                pagingCookie = returnCollection.PagingCookie;
                hasMoreRecords = returnCollection.MoreRecords;
            } while (hasMoreRecords);

            if (stringValues.Any())
            {
                var concatenatedString = stringValues.Aggregate((x, y) => x + separator + y);
                objCommon.Trace($"Concatenated string: {concatenatedString}");
                ConcatenatedString.Set(executionContext,concatenatedString);
            }
            else
            {
                objCommon.Trace("No data found to concatenate");
            }

            objCommon.Trace("ConcatenateFromQuery -- Done!");

            #endregion
        }
        


    }

}
