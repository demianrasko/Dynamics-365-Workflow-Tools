using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
        [TestMethod]
        public void GetMultiSelectOptionSet_ReturnsTheValuesOrAnEmptyCollection()
        {
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["new_colors"] = Options(2, 5) };

            CollectionAssert.AreEqual(new[] { 2, 5 }, common.GetMultiSelectOptionSet(new EntityReference("account", RecordId), "new_colors").Select(v => v.Value).ToArray());
            Assert.AreEqual(0, common.GetMultiSelectOptionSet(new EntityReference("account", RecordId), "new_sizes").Count);
        }

        [TestMethod]
        public void GetOptionSetNames_UsesTheOptionLabels()
        {
            var options = new OptionMetadataCollection { new OptionMetadata(new Label("Red", 1033), 1), new OptionMetadata(new Label("Blue", 1033), 2) };
            service.OnExecute = r => new RetrieveAttributeResponse
            {
                Results = { ["AttributeMetadata"] = new MultiSelectPicklistAttributeMetadata { OptionSet = new OptionSetMetadata(options) } }
            };

            Assert.AreEqual("Blue,Red,3", common.GetOptionSetNames("account", "new_colors", Options(2, 1, 3)));
        }

        [TestMethod]
        public void SetMultiSelectOptionSets_ReplacesTheValues()
        {
            var values = Options(1, 2);

            common.SetMultiSelectOptionSet(new EntityReference("account", RecordId), "new_colors", values, false);

            Assert.AreEqual(0, service.Retrieved.Count);
            CollectionAssert.AreEqual(new[] { 1, 2 }, Values(service.Updated.Single(), "new_colors"));
        }

        [TestMethod]
        public void SetMultiSelectOptionSets_KeepsExistingValuesWithOneRetrieve()
        {
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["new_colors"] = Options(1, 3), ["new_sizes"] = Options(7) };
            var values = new Dictionary<string, OptionSetValueCollection>
            {
                ["new_colors"] = Options(3, 4),
                ["new_sizes"] = Options(8)
            };

            common.SetMultiSelectOptionSets(new EntityReference("account", RecordId), values, true);

            Assert.AreEqual(1, service.Retrieved.Count);
            var update = service.Updated.Single();
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, Values(update, "new_colors"));
            CollectionAssert.AreEqual(new[] { 7, 8 }, Values(update, "new_sizes"));
        }

        [TestMethod]
        public void SetMultiSelectOptionSets_NothingToSetSkipsTheUpdate()
        {
            common.SetMultiSelectOptionSets(new EntityReference("account", RecordId), new Dictionary<string, OptionSetValueCollection>(), false);

            Assert.AreEqual(0, service.Updated.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void MapMultiSelectOptionSets_MismatchedAttributeCountsThrow()
        {
            common.MapMultiSelectOptionSets(new EntityReference("lead", RecordId), new[] { "a", "b" }, new EntityReference("account", RecordId), new[] { "a" }, false);
        }

        [TestMethod]
        public void MapMultiSelectOptionSets_CopiesMultiSelectFieldsAndSkipsOthers()
        {
            var target = new EntityReference("account", Guid.NewGuid());
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["new_colors"] = Options(5), ["name"] = "text" };

            common.MapMultiSelectOptionSets(new EntityReference("lead", RecordId), new[] { "new_colors", "name", "new_missing" }, target, new[] { "new_tint", "new_x", "new_y" }, false);

            var update = service.Updated.Single();
            Assert.AreEqual(target.Id, update.Id);
            CollectionAssert.AreEqual(new[] { "new_tint" }, update.Attributes.Keys.ToArray());
            CollectionAssert.AreEqual(new[] { 5 }, Values(update, "new_tint"));
        }

        [TestMethod]
        public void CloneRecord_SetsTheReplacementValuesOnCreate()
        {
            var oldParent = new EntityReference("salesorder", Guid.NewGuid());
            var newParent = new EntityReference("salesorder", Guid.NewGuid());
            service.OnExecute = r => new RetrieveEntityResponse
            {
                Results = { ["EntityMetadata"] = EntityWithAttributes("salesorderdetailid", Attribute<StringAttributeMetadata>("productdescription"), Attribute<LookupAttributeMetadata>("salesorderid")) }
            };
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["productdescription"] = "Widget", ["salesorderid"] = oldParent };

            common.CloneRecord("salesorderdetail", RecordId, null, null, new Dictionary<string, object> { ["salesorderid"] = newParent, ["new_oldorderid"] = null });

            var copy = service.Created.Single();
            Assert.AreEqual("Widget", copy["productdescription"]);
            Assert.AreEqual(newParent, copy["salesorderid"]);
            Assert.IsTrue(copy.Contains("new_oldorderid") && copy["new_oldorderid"] == null);
            Assert.AreEqual(0, service.Updated.Count, "the copy is never created under the old parent and updated afterwards");
        }

        [TestMethod]
        public void CloneRecord_StartsActiveUnlessAskedToCopyTheStatus()
        {
            SetUpRecordToClone(state: 1, status: 2);

            common.CloneRecord("new_item", RecordId, null, null);

            Assert.IsFalse(service.Created.Single().Contains("statuscode"));
            Assert.AreEqual(0, service.Executed.OfType<SetStateRequest>().Count());
        }

        [TestMethod]
        public void CloneRecord_CopyStatus_SetsAnInactiveStateAfterTheCreate()
        {
            SetUpRecordToClone(state: 1, status: 2);

            var id = common.CloneRecord("new_item", RecordId, null, null, copyStatus: true);

            Assert.IsFalse(service.Created.Single().Contains("statuscode"));
            var request = service.Executed.OfType<SetStateRequest>().Single();
            Assert.AreEqual(id, request.EntityMoniker.Id);
            Assert.AreEqual(1, request.State.Value);
            Assert.AreEqual(2, request.Status.Value);
        }

        [TestMethod]
        public void CloneRecord_CopyStatus_SetsAnActiveStatusReasonOnCreate()
        {
            SetUpRecordToClone(state: 0, status: 100000001);

            common.CloneRecord("new_item", RecordId, null, null, copyStatus: true);

            Assert.AreEqual(100000001, service.Created.Single().GetAttributeValue<OptionSetValue>("statuscode").Value);
            Assert.AreEqual(0, service.Executed.OfType<SetStateRequest>().Count());
        }

        private void SetUpRecordToClone(int state, int status)
        {
            service.OnExecute = r => r is SetStateRequest
                ? new SetStateResponse()
                : (OrganizationResponse)new RetrieveEntityResponse
                {
                    Results = { ["EntityMetadata"] = EntityWithAttributes("new_itemid", Attribute<StringAttributeMetadata>("new_name"), Attribute<StateAttributeMetadata>("statecode"), Attribute<StatusAttributeMetadata>("statuscode")) }
                };
            service.OnRetrieve = (name, id, columns) => new Entity(name, id)
            {
                ["new_name"] = "Item",
                ["statecode"] = new OptionSetValue(state),
                ["statuscode"] = new OptionSetValue(status)
            };
        }

        [TestMethod]
        public void SetLookupAndSetMoney_UpdateTheField()
        {
            var record = new EntityReference("quote", RecordId);
            var account = new EntityReference("account", Guid.NewGuid());

            common.SetLookup(record, "customerid", account);
            common.SetMoney(record, "discountamount", 12.5m);

            Assert.AreEqual(account, service.Updated[0]["customerid"]);
            Assert.AreEqual(12.5m, service.Updated[1].GetAttributeValue<Money>("discountamount").Value);
        }

        [TestMethod]
        public void DeleteDisassociateAndRetrieveFirst()
        {
            var record = new EntityReference("account", RecordId);
            var related = new EntityReference("contact", Guid.NewGuid());
            var first = new Entity("contact", Guid.NewGuid());
            service.OnRetrieveMultiple = query => Collection(first, new Entity("contact", Guid.NewGuid()));

            common.DeleteRecord(record);
            common.DisassociateEntity(record, "new_account_contact", related);

            Assert.AreEqual(record, service.Deleted.Single());
            var call = service.Disassociated.Single();
            Assert.AreEqual(record, call.Record);
            Assert.AreEqual("new_account_contact", call.Relationship.SchemaName);
            Assert.AreEqual(related, call.Related.Single());
            Assert.AreSame(first, common.RetrieveFirst(new QueryExpression("contact")));
        }

        [TestMethod]
        public void SetState_SendsStateAndStatus()
        {
            service.OnExecute = r => new OrganizationResponse();
            var record = new EntityReference("account", RecordId);

            common.SetState(record, 1, 2);

            var request = service.Executed.Single();
            Assert.AreEqual("SetState", request.RequestName);
            Assert.AreEqual(record, request["EntityMoniker"]);
            Assert.AreEqual(1, ((OptionSetValue)request["State"]).Value);
            Assert.AreEqual(2, ((OptionSetValue)request["Status"]).Value);
        }

        [TestMethod]
        public void ActivityParties_FiltersByActivityAndParticipation()
        {
            var query = Common.ActivityPartiesQuery(IdA, 2);

            Assert.AreEqual("activityparty", query.EntityName);
            CollectionAssert.AreEqual(new[] { "partyid" }, query.ColumnSet.Columns.ToArray());
            AssertCondition(query.Criteria.Conditions[0], "activityid", ConditionOperator.Equal, IdA);
            AssertCondition(query.Criteria.Conditions[1], "participationtypemask", ConditionOperator.Equal, 2);
        }

        [TestMethod]
        public void CalculateRollupField_TrimsTheFieldName()
        {
            service.OnExecute = r => new CalculateRollupFieldResponse();

            common.CalculateRollupField(new EntityReference(EntityNames.Account, RecordId), "new_answeredcount ");

            Assert.AreEqual("new_answeredcount", ((CalculateRollupFieldRequest)service.Executed.Single()).FieldName);
            Assert.AreEqual(0, service.Updated.Count, "nothing is copied without a field to copy to");
        }

        [TestMethod]
        public void CalculateRollupField_CopiesTheNewValueToAnotherField()
        {
            var total = new Money(42.5m);
            service.OnExecute = r => new CalculateRollupFieldResponse { Results = { ["Entity"] = new Entity(EntityNames.Account, RecordId) { ["new_total"] = total } } };

            var value = common.CalculateRollupField(new EntityReference(EntityNames.Account, RecordId), "new_total", " new_totalcopy ");

            Assert.AreEqual(total, value);
            var update = service.Updated.Single();
            Assert.AreEqual(RecordId, update.Id);
            Assert.AreEqual(total, update["new_totalcopy"]);
        }
    }
}
