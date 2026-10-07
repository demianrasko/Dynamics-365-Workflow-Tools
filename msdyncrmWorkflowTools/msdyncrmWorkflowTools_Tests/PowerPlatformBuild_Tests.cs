using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace msdyncrmWorkflowTools_Tests
{
    /// <summary>
    /// Checks the Power Platform build (msdyncrmWorkflowTools.csproj built with /p:PowerPlatform=true) against the normal
    /// build: it must leave out exactly the activities that need Dynamics 365 tables, so it imports into a Dataverse
    /// environment without the Dynamics 365 apps. Inconclusive when the two builds aren't next to the test project.
    /// </summary>
    [TestClass]
    public class PowerPlatformBuild_Tests
    {
        private static readonly string[] Dynamics365Tables = { "incident", "lead", "list", "opportunity", "product", "quote", "quotedetail", "salesliterature", "uom" };

        private static readonly string[] Dynamics365Activities =
        {
            "msdyncrmWorkflowTools.CreateOpportunityProduct",
            "msdyncrmWorkflowTools.CreateQuoteFromOpportunity",
            "msdyncrmWorkflowTools.QualifyLead",
            "msdyncrmWorkflowTools.UpdateProductQuoteValue",
            "msdyncrmWorkflowTools.UpdateQuoteValue",
            "msdyncrmWorkflowTools.WinQuote",
            "msdyncrmWorkflowTools.Class.AddMarketingListToCampaign",
            "msdyncrmWorkflowTools.Class.AddToMarketingList",
            "msdyncrmWorkflowTools.Class.CopyMarketingListMembers",
            "msdyncrmWorkflowTools.Class.CopyToStaticList",
            "msdyncrmWorkflowTools.Class.IsMemberOfMarketingList",
            "msdyncrmWorkflowTools.Class.RemoveFromAllMarketingLists",
            "msdyncrmWorkflowTools.Class.RemoveFromMarketingList",
            "msdyncrmWorkflowTools.Class.ResolveCase",
            "msdyncrmWorkflowTools.Class.SalesLiteratureToEmail"
        };

        [TestMethod]
        public void PowerPlatformBuild_LeavesOutOnlyDynamics365Activities()
        {
            var dynamics365Build = GetActivityTypes(LoadWorkflowAssembly(string.Empty)).Select(t => t.FullName).ToList();
            var powerPlatformBuild = GetActivityTypes(LoadWorkflowAssembly("-PowerPlatform")).Select(t => t.FullName).ToList();

            CollectionAssert.AreEquivalent(Dynamics365Activities, dynamics365Build.Except(powerPlatformBuild).ToList());
            CollectionAssert.AreEquivalent(new List<string>(), powerPlatformBuild.Except(dynamics365Build).ToList());
        }

        [TestMethod]
        public void PowerPlatformBuild_HasNoLookupsToDynamics365Tables()
        {
            var lookups = (from type in GetActivityTypes(LoadWorkflowAssembly("-PowerPlatform"))
                           from property in type.GetProperties()
                           from attribute in property.GetCustomAttributesData()
                           where attribute.AttributeType.Name == "ReferenceTargetAttribute"
                           let table = attribute.ConstructorArguments[0].Value as string
                           where Dynamics365Tables.Contains(table)
                           select $"{type.Name}.{property.Name} ({table})").ToList();

            Assert.AreEqual(0, lookups.Count, $"Wrap these activities in #if !POWERPLATFORM: {string.Join(", ", lookups)}");
        }

        [TestMethod]
        public void EveryActivityHasAUniqueDesignerName()
        {
            foreach (var suffix in new[] { string.Empty, "-PowerPlatform" })
            {
                var names = GetActivityTypes(LoadWorkflowAssembly(suffix)).ToDictionary(
                    t => t.FullName,
                    t => t.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.Name == "ActivityNameAttribute")?.ConstructorArguments[0].Value as string);

                var unnamed = names.Where(n => string.IsNullOrWhiteSpace(n.Value)).Select(n => n.Key).ToList();
                Assert.AreEqual(0, unnamed.Count, $"Add [ActivityName(\"...\")] to: {string.Join(", ", unnamed)}");

                var duplicates = names.GroupBy(n => n.Value, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                Assert.AreEqual(0, duplicates.Count, $"Activity names used more than once: {string.Join(", ", duplicates)}");
            }
        }

        [TestMethod]
        public void EveryArgumentOfThePublishedVersionIsUnchanged()
        {
            // workflows saved on 1.0.61.1 pass their inputs by property name, case included
            var types = GetActivityTypes(LoadWorkflowAssembly(string.Empty)).ToDictionary(t => t.FullName);
            var problems = new List<string>();

            foreach (var argument in ReleasedActivityArguments.Version_1_0_61_1)
            {
                var parts = argument.Split(' ');
                var typeName = parts[0];
                var propertyName = parts[1];
                var expected = $"{parts[2]}`1[{parts[3]}]";

                if (!types.TryGetValue(typeName, out var type))
                {
                    problems.Add($"{typeName} is missing");
                    continue;
                }

                var property = type.GetProperty(propertyName);

                if (property == null)
                {
                    problems.Add($"{typeName}.{propertyName} is missing (renamed?)");
                }
                else if ($"{property.PropertyType.Name}[{property.PropertyType.GetGenericArguments().FirstOrDefault()?.FullName}]" != expected)
                {
                    problems.Add($"{typeName}.{propertyName} is {property.PropertyType}, was {expected}");
                }
            }

            Assert.AreEqual(0, problems.Count, $"Saved workflows would break: {string.Join("; ", problems.Distinct())}");
        }

        /// <summary>
        /// Loads msdyncrmWorkflowTools.dll (or powerplatformWorkflowTools.dll for the Power Platform build) from the workflow project's bin folder for the test's configuration plus
        /// <paramref name="suffix"/>. It's loaded from bytes so both builds, which share an identity, can be loaded side by side.
        /// </summary>
        private static Assembly LoadWorkflowAssembly(string suffix)
        {
            var testFolder = new DirectoryInfo(Path.GetDirectoryName(typeof(PowerPlatformBuild_Tests).Assembly.Location));
            var solutionFolder = testFolder.Parent?.Parent?.Parent;

            if (solutionFolder == null)
            {
                Assert.Inconclusive($"The solution folder was not found above {testFolder.FullName}.");
            }

            var assemblyName = string.IsNullOrEmpty(suffix) ? "msdyncrmWorkflowTools" : "powerplatformWorkflowTools";
            var path = Path.Combine(solutionFolder.FullName, "msdyncrmWorkflowTools", "bin", $"{testFolder.Name}{suffix}", $"{assemblyName}.dll");

            if (!File.Exists(path))
            {
                Assert.Inconclusive($"{path} was not found. Build msdyncrmWorkflowTools.csproj with /p:PowerPlatform=true first.");
            }

            return Assembly.Load(File.ReadAllBytes(path));
        }

        private static IEnumerable<Type> GetActivityTypes(Assembly assembly)
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            return types.Where(t => typeof(CodeActivity).IsAssignableFrom(t) && !t.IsAbstract);
        }
    }
}
