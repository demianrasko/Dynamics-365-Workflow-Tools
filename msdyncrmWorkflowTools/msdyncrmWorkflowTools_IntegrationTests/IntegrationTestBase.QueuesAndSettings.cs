using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void GetEnvironmentVariable_UsesTheCurrentValueOverTheDefault()
        {
            var schemaName = $"new_WftTest{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var definition = Create(new Entity(EntityNames.EnvironmentVariableDefinition)
            {
                [AttributeNames.SchemaName] = schemaName,
                ["displayname"] = UniqueName("variable"),
                ["type"] = new OptionSetValue(100000000), // String
                [AttributeNames.DefaultValue] = "default value"
            });

            Assert.AreEqual("default value", Common.GetEnvironmentVariable(schemaName));

            Create(new Entity(EntityNames.EnvironmentVariableValue)
            {
                [AttributeNames.SchemaName] = schemaName,
                [AttributeNames.EnvironmentVariableDefinitionId] = definition,
                [AttributeNames.Value] = "current value"
            });

            Assert.AreEqual("current value", Common.GetEnvironmentVariable(schemaName));
            Assert.IsNull(Common.GetEnvironmentVariable($"new_WftMissing{Guid.NewGuid():N}"));
        }
    }
}
