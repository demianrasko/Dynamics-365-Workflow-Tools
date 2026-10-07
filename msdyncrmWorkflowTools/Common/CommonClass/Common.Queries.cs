using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// Converts FetchXML to a QueryExpression (FetchXmlToQueryExpressionRequest), so it can be paged and counted.
        /// </summary>
        public QueryExpression FetchXmlToQueryExpression(string fetchXml)
        {
            var response = (FetchXmlToQueryExpressionResponse)Service.Execute(new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml });
            return response.Query;
        }

        /// <summary>
        /// The first record a query returns, or null when there is none.
        /// </summary>
        public Entity RetrieveFirst(QueryBase query)
        {
            return Service.RetrieveMultiple(query).Entities.FirstOrDefault();
        }

        /// <summary>
        /// Counts the records a query returns, page by page, so there is no 5,000-row or 50,000-row aggregate limit.
        /// </summary>
        public int CountRecords(QueryExpression query)
        {
            query.ColumnSet = new ColumnSet(false);
            query.PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 };

            var count = 0;

            while (true)
            {
                var page = Service.RetrieveMultiple(query);
                count += page.Entities.Count;

                if (!page.MoreRecords)
                {
                    return count;
                }

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
        }

        /// <summary>
        /// Returns the ids of every record a query returns, page by page (no 5,000-row limit).
        /// </summary>
        public List<Guid> RetrieveAllIds(QueryExpression query)
        {
            query.ColumnSet = new ColumnSet(false);
            query.PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 };

            var ids = new List<Guid>();

            while (true)
            {
                var page = Service.RetrieveMultiple(query);
                ids.AddRange(page.Entities.Select(e => e.Id));

                if (!page.MoreRecords)
                {
                    return ids;
                }

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
        }

        /// <summary>
        /// The first record a FetchXML query returns (only one record is read), or null when there is none.
        /// </summary>
        public Entity RetrieveFirstWithFetchXml(string fetchXml)
        {
            var xml = Utility.HasFetchTop(fetchXml) ? fetchXml : Utility.CreateXml(fetchXml, null, 1, 1);

            return Service.RetrieveMultiple(new FetchExpression(xml)).Entities.FirstOrDefault();
        }

        /// <summary>
        /// The date a FetchXML query (usually an aggregate such as max(createdon)) returns: the first date in its first
        /// record (see <see cref="Utility.GetFirstFetchDate"/>).
        /// </summary>
        /// <param name="fetchXml">The query; {PARENT_GUID} is replaced by <paramref name="parentId"/>.</param>
        /// <param name="parentId">The record the workflow runs on.</param>
        /// <returns>The date, or null when the query returns no record or no date.</returns>
        /// <exception cref="InvalidPluginExecutionException">The query is empty.</exception>
        public DateTime? CalculateAggregateDate(string fetchXml, Guid parentId)
        {
            fetchXml = Utility.Required(fetchXml, "FetchXML").Replace("{PARENT_GUID}", parentId.ToString());
            Trace($"FetchXML={fetchXml}");

            var record = RetrieveFirstWithFetchXml(fetchXml);

            if (record == null)
            {
                Trace("No record found.");
                return null;
            }

            var date = Utility.GetFirstFetchDate(record, fetchXml);
            Trace(date.HasValue ? $"Date={date}" : "The record has no date.");

            return date;
        }

        /// <summary>
        /// Count, sum, average, min and max of the first attribute in a FetchXML query, over every record it returns
        /// (all pages), for the Rollup Functions activity. Records without a value are left out of the sum, average,
        /// min and max.
        /// </summary>
        /// <param name="fetchXml">The query; {PARENT_GUID} is replaced by <paramref name="parentId"/>.</param>
        /// <param name="parentId">The record the workflow runs on.</param>
        /// <exception cref="InvalidPluginExecutionException">The query is empty.</exception>
        public RollupResult CalculateRollup(string fetchXml, Guid parentId)
        {
            fetchXml = Utility.Required(fetchXml, "FetchXML").Replace("{PARENT_GUID}", parentId.ToString());
            Trace($"FetchXML={fetchXml}");

            // the calculations use the first attribute in the fetch
            var key = Utility.GetFirstFetchAttributeKey(fetchXml);
            var values = RetrieveAllWithFetchXml(fetchXml)
                .Select(record => Utility.ToDecimal(Utility.GetFirstFetchValue(record, key)))
                .ToList();

            var result = Utility.CalculateRollup(values);
            Trace($"Records={result.Count}, Sum={result.Sum}, Average={result.Average}, Min={result.Min}, Max={result.Max}");

            return result;
        }

        /// <summary>
        /// Returns every record a FetchXML query returns, reading the next page only when the caller needs more
        /// records (so stopping early, e.g. with Take, stops the paging). A fetch with a top attribute is run once,
        /// unpaged, because Dataverse does not allow top together with paging.
        /// </summary>
        /// <param name="fetchXml">The fetch query, without paging attributes.</param>
        /// <param name="pageSize">Records per page.</param>
        public IEnumerable<Entity> RetrieveAllWithFetchXml(string fetchXml, int pageSize = 250)
        {
            var canPage = !Utility.HasFetchTop(fetchXml);
            string pagingCookie = null;

            for (var pageNumber = 1; ; pageNumber++)
            {
                var xml = canPage ? Utility.CreateXml(fetchXml, pagingCookie, pageNumber, pageSize) : fetchXml;
                var page = Service.RetrieveMultiple(new FetchExpression(xml));

                foreach (var record in page.Entities)
                {
                    yield return record;
                }

                if (!canPage || !page.MoreRecords)
                {
                    yield break;
                }

                pagingCookie = page.PagingCookie;
            }
        }

        /// <summary>
        /// The first record of <paramref name="entityName"/> where every filter attribute equals its value (a null
        /// value matches an empty attribute). Empty column names and filters with an empty attribute name are skipped.
        /// </summary>
        public static QueryExpression FirstMatchQuery(string entityName, IEnumerable<string> columns, params KeyValuePair<string, object>[] equalFilters)
        {
            var query = new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet(columns.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray()),
                TopCount = 1
            };

            foreach (var filter in equalFilters.Where(f => !string.IsNullOrEmpty(f.Key)))
            {
                if (filter.Value == null)
                {
                    query.Criteria.AddCondition(filter.Key, ConditionOperator.Null);
                }
                else
                {
                    query.Criteria.AddCondition(filter.Key, ConditionOperator.Equal, filter.Value);
                }
            }

            return query;
        }

        /// <summary>
        /// A value typed into a workflow as text, converted for comparing with a column in a query: a number for whole
        /// number, decimal, currency, choice and status columns, true/false for Yes/No, a date, and the GUID for any
        /// lookup (see <see cref="Utility.ConvertToAttributeType"/>). Text columns compare with the text as it is.
        /// </summary>
        /// <returns>The value, or null for an empty value of a column that isn't text.</returns>
        /// <exception cref="InvalidPluginExecutionException">The text can't be read as the column's type.</exception>
        public object ToFilterValue(string entityName, string attributeName, string value)
        {
            var response = (RetrieveAttributeResponse)Service.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName
            });
            var attribute = response.AttributeMetadata;

            // a lookup is compared by its id, whichever table it points to
            if (attribute is LookupAttributeMetadata && Guid.TryParse(value, out var id))
            {
                return id;
            }

            return Utility.ToConditionValue(Utility.ConvertToAttributeType(value, attribute));
        }

        /// <summary>
        /// The first <paramref name="entityName"/> record whose attributes equal the given values (see
        /// <see cref="FirstMatchQuery"/>), or null when there is none.
        /// </summary>
        public Entity RetrieveFirstMatch(string entityName, IEnumerable<string> columns, params KeyValuePair<string, object>[] equalFilters)
        {
            return RetrieveFirst(FirstMatchQuery(entityName, columns, equalFilters));
        }

        /// <summary>
        /// Two column values, as text, of the first record whose filter columns equal the given values, for the Query
        /// Values activity. The filter values are typed as text and converted to each column's type
        /// (see <see cref="ToFilterValue"/>); an empty filter column is left out.
        /// </summary>
        /// <param name="value2">The second column's value; null when there's no match or no value.</param>
        /// <returns>The first column's value; null when there's no match or no value.</returns>
        public string QueryValues(string entityName, string attribute1, string attribute2, string filterAttribute1, string filterValue1,
            string filterAttribute2, string filterValue2, out string value2)
        {
            Trace($"EntityName: {entityName} - Attribute1:{attribute1} - Attribute2:{attribute2} - FilterAttribute1:{filterAttribute1} - FilterAttribute2:{filterAttribute2} - ValueAttribute1:{filterValue1} ValueAttribute2:{filterValue2}");

            var record = RetrieveFirstMatch(entityName,
                new[] { attribute1, attribute2 },
                new KeyValuePair<string, object>(filterAttribute1, string.IsNullOrEmpty(filterAttribute1) ? null : ToFilterValue(entityName, filterAttribute1, filterValue1)),
                new KeyValuePair<string, object>(filterAttribute2, string.IsNullOrEmpty(filterAttribute2) ? null : ToFilterValue(entityName, filterAttribute2, filterValue2)));

            if (record == null)
            {
                Trace("No matching record.");
            }

            value2 = ColumnText(record, attribute2);

            return ColumnText(record, attribute1);
        }

        private static string ColumnText(Entity record, string attributeName)
        {
            return record != null && !string.IsNullOrEmpty(attributeName) && record.Contains(attributeName)
                ? Utility.AttributeValueToString(record[attributeName])
                : null;
        }
    }
}
