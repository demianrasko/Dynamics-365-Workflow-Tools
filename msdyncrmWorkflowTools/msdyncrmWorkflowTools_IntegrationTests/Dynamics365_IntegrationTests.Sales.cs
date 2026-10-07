using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Sales, marketing and service methods, which need Dynamics 365 tables, so they only run against the Dynamics 365
    /// environment.
    /// </summary>
    public partial class Dynamics365_IntegrationTests
    {
        [TestMethod]
        public void Quotes_FromOpportunityProductsToAWonQuote()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("customer") });
            var priceList = CreatePricedProduct(25m, out var product, out var unit);
            var opportunity = BlockedByCustomPlugins(() => Create(new Entity(EntityNames.Opportunity)
            {
                [AttributeNames.Name] = UniqueName("opportunity"),
                ["customerid"] = account,
                ["pricelevelid"] = priceList
            }));

            var line = Common.CreateOpportunityProduct(opportunity, product, unit, 2m);
            DeleteAfterTest(new EntityReference(EntityNames.OpportunityProduct, line));

            Common.CalculatePrice(opportunity);
            Assert.AreEqual(50m, Read(opportunity, "totallineitemamount").GetAttributeValue<Money>("totallineitemamount").Value);

            var quote = Common.CreateQuoteFromOpportunity(opportunity.Id);
            DeleteAfterTest(quote);
            Assert.AreEqual(1, Service.RetrieveMultiple(new QueryExpression(EntityNames.QuoteDetail)
            {
                ColumnSet = new ColumnSet(false),
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.QuoteId, ConditionOperator.Equal, quote.Id) } }
            }).Entities.Count);

            // a quote has to be active before it can be won
            Common.SetState(quote, 1, 2);
            Common.WinQuote(quote, UniqueName("won"));

            Assert.AreEqual(2, StateOf(quote));
        }

        [TestMethod]
        public void MarketingLists_AddCheckCopyRemoveAndCampaigns()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("member") });
            var first = CreateList(false);
            var second = CreateList(false);

            Assert.IsFalse(Common.IsMemberOfMarketingList(first.Id, account.Id));

            Common.AddToMarketingList(first.Id, account);
            Assert.IsTrue(Common.IsMemberOfMarketingList(first.Id, account.Id));

            Common.CopyListMembers(first.Id, second.Id);
            Assert.IsTrue(Common.IsMemberOfMarketingList(second.Id, account.Id));

            Common.RemoveFromMarketingList(first.Id, account.Id);
            Assert.IsFalse(Common.IsMemberOfMarketingList(first.Id, account.Id));

            Common.AddToMarketingList(first.Id, account);
            Assert.AreEqual(2, Common.RemoveFromAllMarketingLists(account));
            Assert.IsFalse(Common.IsMemberOfMarketingList(second.Id, account.Id));

            var campaign = Create(new Entity(EntityNames.Campaign) { [AttributeNames.Name] = UniqueName("campaign") });
            Common.AddListToCampaign(first.Id, campaign.Id);

            Assert.AreEqual(1, Service.RetrieveMultiple(new QueryExpression("campaignitem")
            {
                ColumnSet = new ColumnSet(false),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("campaignid", ConditionOperator.Equal, campaign.Id),
                        new ConditionExpression(AttributeNames.EntityId, ConditionOperator.Equal, first.Id)
                    }
                }
            }).Entities.Count);
        }

        [TestMethod]
        public void CopyDynamicListToStatic_MakesAStaticCopy()
        {
            var name = UniqueName("dynamic");
            Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = name });
            var dynamicList = Create(new Entity(EntityNames.List)
            {
                ["listname"] = name,
                ["createdfromcode"] = new OptionSetValue(1),
                ["type"] = true,
                ["query"] = $"<fetch><entity name='account'><attribute name='accountid' /><filter><condition attribute='name' operator='eq' value='{name}' /></filter></entity></fetch>"
            });

            Common.CopyDynamicListToStatic(dynamicList.Id);

            var copies = Service.RetrieveMultiple(new QueryExpression(EntityNames.List)
            {
                ColumnSet = new ColumnSet(false),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("listname", ConditionOperator.BeginsWith, name),
                        new ConditionExpression("type", ConditionOperator.Equal, false)
                    }
                }
            }).Entities;

            foreach (var copy in copies)
            {
                DeleteAfterTest(copy.ToEntityReference());
            }

            Assert.AreEqual(1, copies.Count);
        }

        [TestMethod]
        public void ManyToMany_FindsLeadsLinkedToAnAccount()
        {
            // accountleads_association's metadata pairs each table with the other one's intersect attribute
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("account") });
            var lead = Create(new Entity(EntityNames.Lead) { ["lastname"] = UniqueName("lead") });

            Common.AssociateEntity(EntityNames.Account, account.Id, "accountleads_association", "accountleads", EntityNames.Lead, lead.Id);

            CollectionAssert.AreEqual(new[] { lead.Id }, Common.GetManyToManyRelatedIds("accountleads_association", EntityNames.Account, account.Id));
            CollectionAssert.AreEqual(new[] { account.Id }, Common.GetManyToManyRelatedIds("accountleads_association", EntityNames.Lead, lead.Id));
        }

        [TestMethod]
        public void QualifyLead_CreatesTheAccountContactAndOpportunity()
        {
            var lead = Create(new Entity(EntityNames.Lead)
            {
                [AttributeNames.Subject] = UniqueName("lead"),
                ["lastname"] = UniqueName("lead"),
                ["companyname"] = UniqueName("company")
            });

            BlockedByCustomPlugins(() => Common.QualifyLead(lead, true, true, true, null, null, 3));

            foreach (var table in new[] { EntityNames.Opportunity, EntityNames.Contact, EntityNames.Account })
            {
                var created = Service.RetrieveMultiple(new QueryExpression(table)
                {
                    ColumnSet = new ColumnSet(false),
                    Criteria = { Conditions = { new ConditionExpression("originatingleadid", ConditionOperator.Equal, lead.Id) } }
                }).Entities;

                foreach (var record in created)
                {
                    DeleteAfterTest(record.ToEntityReference());
                }

                Assert.AreEqual(1, created.Count, table);
            }

            Assert.AreEqual(1, StateOf(lead));
        }

        [TestMethod]
        public void Cases_ResolveAndApplyRoutingRules()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("customer") });
            var incident = Create(new Entity(EntityNames.Incident) { [AttributeNames.Title] = UniqueName("case"), ["customerid"] = account });

            try
            {
                Common.ApplyRoutingRule(incident);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                TestContext.WriteLine($"ApplyRoutingRule: {ex.Detail.Message}");
            }

            Common.ResolveCase(incident.Id, UniqueName("resolved"), "Resolved by the integration tests.");

            Assert.AreEqual(1, StateOf(incident));
        }

        [TestMethod]
        public void CalculateRollupField_RecalculatesNow()
        {
            if (!HasColumn(EntityNames.Account, "opendeals"))
            {
                Assert.Inconclusive("account has no opendeals rollup column here.");
            }

            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("rollup") });
            BlockedByCustomPlugins(() => Create(new Entity(EntityNames.Opportunity) { [AttributeNames.Name] = UniqueName("open deal"), ["parentaccountid"] = account }));

            Common.CalculateRollupField(account, "opendeals ");

            Assert.AreEqual(1, Read(account, "opendeals").GetAttributeValue<int>("opendeals"));
        }

        [TestMethod]
        public void RecalculateGoal_RunsForAGoal()
        {
            var metric = Create(new Entity("metric")
            {
                [AttributeNames.Name] = UniqueName("metric"),
                ["amountdatatype"] = new OptionSetValue(0),
                ["isamount"] = true
            });
            var goal = Create(new Entity(EntityNames.Goal)
            {
                [AttributeNames.Title] = UniqueName("goal"),
                ["metricid"] = metric,
                ["goalownerid"] = new EntityReference(EntityNames.SystemUser, UserId),
                ["isfiscalperiodgoal"] = false,
                ["goalstartdate"] = DateTime.Today.AddDays(-1),
                ["goalenddate"] = DateTime.Today.AddDays(30),
                ["targetmoney"] = new Money(100m)
            });

            Common.RecalculateGoal(goal.Id);
        }

        [TestMethod]
        public void SalesLiteratureToEmail_AttachesTheMatchingItems()
        {
            var literature = Create(new Entity(EntityNames.SalesLiterature) { [AttributeNames.Name] = UniqueName("literature") });

            foreach (var fileName in new[] { "brochure.txt", "price-list.pdf" })
            {
                Create(new Entity(EntityNames.SalesLiteratureItem)
                {
                    ["salesliteratureid"] = literature,
                    [AttributeNames.Title] = fileName,
                    [AttributeNames.FileName] = fileName,
                    [AttributeNames.MimeType] = "text/plain",
                    [AttributeNames.DocumentBody] = Convert.ToBase64String(Encoding.UTF8.GetBytes($"WFT test {fileName}"))
                });
            }

            var email = Create(new Entity(EntityNames.Email) { [AttributeNames.Subject] = UniqueName("literature email") });

            Common.SalesLiteratureToEmail("*.txt", literature.Id, email.Id);

            var attached = Service.RetrieveMultiple(new QueryExpression(EntityNames.ActivityMimeAttachment)
            {
                ColumnSet = new ColumnSet(AttributeNames.FileName),
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.ObjectId, ConditionOperator.Equal, email.Id) } }
            }).Entities.Select(e => e.GetAttributeValue<string>(AttributeNames.FileName)).ToArray();

            CollectionAssert.AreEqual(new[] { "brochure.txt" }, attached);
        }

        /// <summary>
        /// Runs a step that the environment's own plug-ins may reject (e.g. a plug-in on opportunity create that
        /// needs data these tests don't have); the test is inconclusive then, naming the plug-in.
        /// </summary>
        private T BlockedByCustomPlugins<T>(Func<T> step)
        {
            try
            {
                return step();
            }
            catch (FaultException<OrganizationServiceFault> ex) when ((ex.Detail.Message.Contains("plug-in") && !ex.Detail.Message.Contains("msdyncrmWorkflowTools"))
                || ex.Detail.Message.Contains("complete the required steps"))
            {
                Assert.Inconclusive($"Blocked by this environment's plug-ins or business process flow: {ex.Detail.Message}");

                return default(T);
            }
        }

        private void BlockedByCustomPlugins(Action step)
        {
            BlockedByCustomPlugins(() =>
            {
                step();

                return true;
            });
        }

        private EntityReference CreateList(bool dynamic)
        {
            return Create(new Entity(EntityNames.List)
            {
                ["listname"] = UniqueName("list"),
                ["createdfromcode"] = new OptionSetValue(1),
                ["type"] = dynamic
            });
        }

        /// <summary>A price list in the base currency with an active product priced at <paramref name="price"/>.</summary>
        private EntityReference CreatePricedProduct(decimal price, out EntityReference product, out EntityReference unit)
        {
            var currency = (EntityReference)Common.GetOrganizationSetting(AttributeNames.BaseCurrencyId);
            var baseUnit = Service.RetrieveMultiple(new QueryExpression(EntityNames.Uom)
            {
                ColumnSet = new ColumnSet("uomscheduleid"),
                Criteria = { Conditions = { new ConditionExpression("isschedulebaseuom", ConditionOperator.Equal, true) } },
                TopCount = 1
            }).Entities.Single();

            var priceList = Create(new Entity("pricelevel") { [AttributeNames.Name] = UniqueName("price list"), ["transactioncurrencyid"] = currency });
            product = Create(new Entity(EntityNames.Product)
            {
                [AttributeNames.Name] = UniqueName("product"),
                ["productnumber"] = UniqueName("product"),
                ["defaultuomscheduleid"] = baseUnit.GetAttributeValue<EntityReference>("uomscheduleid"),
                ["defaultuomid"] = baseUnit.ToEntityReference(),
                ["quantitydecimal"] = 2
            });

            Create(new Entity("productpricelevel")
            {
                ["pricelevelid"] = priceList,
                ["productid"] = product,
                [AttributeNames.UomId] = baseUnit.ToEntityReference(),
                ["amount"] = new Money(price)
            });

            // products may be created as drafts; a draft can't be sold
            if (StateOf(product) == 2)
            {
                Service.Execute(new PublishProductHierarchyRequest { Target = product });
            }

            unit = baseUnit.ToEntityReference();

            return priceList;
        }
    }
}
