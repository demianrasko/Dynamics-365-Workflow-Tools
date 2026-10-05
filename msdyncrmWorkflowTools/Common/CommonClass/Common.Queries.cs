using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
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
        /// The first record of <paramref name="entityName"/> where every filter attribute equals its value.
        /// Empty column names and filters with an empty attribute name are skipped.
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
                query.Criteria.AddCondition(filter.Key, ConditionOperator.Equal, filter.Value);
            }

            return query;
        }

        /// <summary>
        /// The first <paramref name="entityName"/> record whose attributes equal the given values (see
        /// <see cref="FirstMatchQuery"/>), or null when there is none.
        /// </summary>
        public Entity RetrieveFirstMatch(string entityName, IEnumerable<string> columns, params KeyValuePair<string, object>[] equalFilters)
        {
            return RetrieveFirst(FirstMatchQuery(entityName, columns, equalFilters));
        }
    }
}
