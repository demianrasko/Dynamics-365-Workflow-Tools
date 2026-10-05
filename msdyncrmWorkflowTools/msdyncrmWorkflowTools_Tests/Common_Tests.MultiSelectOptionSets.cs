using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
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
    }
}
