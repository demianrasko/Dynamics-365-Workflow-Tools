using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Registers the built Power Platform assembly in the Power Platform test environment, the way the Plugin
    /// Registration Tool would, and checks what Dataverse makes of it: that it loads in the sandbox, that every
    /// activity registers, and that the designer sees each activity's inputs and outputs. The assembly stays
    /// registered afterwards, so real test workflows can use it.
    /// </summary>
    /// <remarks>
    /// Only the Power Platform build is deployed: the Dynamics 365 test environment has the managed workflow tools
    /// solution installed, which the manual upgrade test uses.
    /// </remarks>
    [TestClass]
    [TestCategory("Deployment")]
    public class Deployment_IntegrationTests
    {
        private const string ConnectionVariable = "DATAVERSE_CONNECTION_PP";

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PowerPlatformBuild_RegistersInTheSandboxWithEveryActivity()
        {
            var service = DataverseConnection.Connect(ConnectionVariable);

            if (service == null)
            {
                Assert.Inconclusive($"Set {ConnectionVariable} (tools\\Set-DataverseTestConnections.ps1) to run the deployment test.");
            }

            var dll = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\msdyncrmWorkflowTools\bin\Release-PowerPlatform\powerplatformWorkflowTools.dll"));

            if (!File.Exists(dll))
            {
                Assert.Inconclusive($"Build the Power Platform version first (/p:PowerPlatform=true); {dll} doesn't exist.");
            }

            var name = AssemblyName.GetAssemblyName(dll);
            var activities = ActivitiesIn(dll, out var designerNames);
            TestContext.WriteLine($"{name.Name} {name.Version}: {activities.Count} activities");

            var assemblyId = RegisterAssembly(service, name, File.ReadAllBytes(dll), activities.Keys);
            RegisterActivities(service, assemblyId, name, designerNames, activities);

            var registered = service.RetrieveMultiple(new QueryExpression("plugintype")
            {
                ColumnSet = new ColumnSet("typename", "customworkflowactivityinfo"),
                Criteria = { Conditions = { new ConditionExpression("pluginassemblyid", ConditionOperator.Equal, assemblyId) } }
            }).Entities.ToDictionary(t => t.GetAttributeValue<string>("typename"), t => t.GetAttributeValue<string>("customworkflowactivityinfo") ?? string.Empty);

            CollectionAssert.AreEquivalent(activities.Keys.ToList(), registered.Keys.ToList());

            var missing = (from activity in activities
                           from label in activity.Value
                           where registered[activity.Key].IndexOf(label, StringComparison.Ordinal) < 0
                           select $"{activity.Key}: {label}").ToList();

            Assert.AreEqual(0, missing.Count, $"Inputs or outputs Dataverse doesn't show: {string.Join("; ", missing)}");
        }

        /// <summary>
        /// The workflow activities in an assembly, with the labels of their inputs and outputs, and their designer names
        /// ([ActivityName]).
        /// </summary>
        private static Dictionary<string, List<string>> ActivitiesIn(string dll, out Dictionary<string, string> designerNames)
        {
            Type[] types;

            try
            {
                types = Assembly.LoadFrom(dll).GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            var activityTypes = types.Where(t => t.IsPublic && !t.IsAbstract && IsCodeActivity(t)).ToList();

            designerNames = activityTypes.ToDictionary(
                t => t.FullName,
                t => t.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.Name == "ActivityNameAttribute")?.ConstructorArguments[0].Value as string ?? t.FullName);

            return activityTypes
                .ToDictionary(
                    t => t.FullName,
                    t => t.GetProperties()
                        .SelectMany(p => p.GetCustomAttributesData())
                        .Where(a => a.AttributeType.Name == "InputAttribute" || a.AttributeType.Name == "OutputAttribute")
                        .Select(a => (string)a.ConstructorArguments[0].Value)
                        .ToList());
        }

        private static bool IsCodeActivity(Type type)
        {
            for (var t = type.BaseType; t != null; t = t.BaseType)
            {
                if (t.FullName == "System.Activities.CodeActivity")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Creates the assembly record, or updates its content (removing activities the build no longer has).</summary>
        private Guid RegisterAssembly(IOrganizationService service, AssemblyName name, byte[] content, IEnumerable<string> activityTypes)
        {
            var existing = service.RetrieveMultiple(new QueryExpression("pluginassembly")
            {
                ColumnSet = new ColumnSet("ismanaged", "version"),
                Criteria = { Conditions = { new ConditionExpression("name", ConditionOperator.Equal, name.Name) } }
            }).Entities.FirstOrDefault();

            if (existing == null)
            {
                TestContext.WriteLine($"Registering {name.Name} {name.Version}");

                return service.Create(new Entity("pluginassembly")
                {
                    ["name"] = name.Name,
                    ["content"] = Convert.ToBase64String(content),
                    ["isolationmode"] = new OptionSetValue(2), // sandbox
                    ["sourcetype"] = new OptionSetValue(0) // database
                });
            }

            if (existing.GetAttributeValue<bool>("ismanaged"))
            {
                Assert.Inconclusive($"{name.Name} belongs to a managed solution in this environment; it isn't replaced by a test build.");
            }

            // a type that is no longer in the assembly has to be removed before the new content is accepted
            var wanted = new HashSet<string>(activityTypes);

            foreach (var type in service.RetrieveMultiple(new QueryExpression("plugintype")
            {
                ColumnSet = new ColumnSet("typename"),
                Criteria = { Conditions = { new ConditionExpression("pluginassemblyid", ConditionOperator.Equal, existing.Id) } }
            }).Entities.Where(t => !wanted.Contains(t.GetAttributeValue<string>("typename"))))
            {
                TestContext.WriteLine($"Removing {type.GetAttributeValue<string>("typename")}");
                service.Delete("plugintype", type.Id);
            }

            TestContext.WriteLine($"Updating {name.Name} {existing.GetAttributeValue<string>("version")} to {name.Version}");
            service.Update(new Entity("pluginassembly", existing.Id) { ["content"] = Convert.ToBase64String(content) });

            return existing.Id;
        }

        /// <summary>
        /// Registers every activity that isn't registered yet under its designer name, and corrects the name (and
        /// group) of those that are. Dataverse describes an activity's inputs and outputs (customworkflowactivityinfo)
        /// only when it's registered, so an activity that has gained one since is registered again, with the same id.
        /// </summary>
        private void RegisterActivities(IOrganizationService service, Guid assemblyId, AssemblyName name, Dictionary<string, string> designerNames,
            Dictionary<string, List<string>> labels)
        {
            var group = $"{name.Name} ({name.Version})";
            var registered = service.RetrieveMultiple(new QueryExpression("plugintype")
            {
                ColumnSet = new ColumnSet("typename", "name", "friendlyname", "workflowactivitygroupname", "customworkflowactivityinfo"),
                Criteria = { Conditions = { new ConditionExpression("pluginassemblyid", ConditionOperator.Equal, assemblyId) } }
            }).Entities.ToDictionary(t => t.GetAttributeValue<string>("typename"));

            foreach (var activity in designerNames)
            {
                Guid? typeId = null;

                if (registered.TryGetValue(activity.Key, out var existing))
                {
                    var info = existing.GetAttributeValue<string>("customworkflowactivityinfo") ?? string.Empty;

                    if (labels[activity.Key].All(label => info.IndexOf(label, StringComparison.Ordinal) >= 0))
                    {
                        if (existing.GetAttributeValue<string>("name") != activity.Value || existing.GetAttributeValue<string>("friendlyname") != activity.Value
                            || existing.GetAttributeValue<string>("workflowactivitygroupname") != group)
                        {
                            service.Update(new Entity("plugintype", existing.Id)
                            {
                                ["name"] = activity.Value,
                                ["friendlyname"] = activity.Value,
                                ["workflowactivitygroupname"] = group
                            });
                        }

                        continue;
                    }

                    TestContext.WriteLine($"Registering {activity.Key} again: its inputs or outputs changed");
                    service.Delete("plugintype", existing.Id);
                    typeId = existing.Id;
                }

                var typeName = activity.Key;
                var type = new Entity("plugintype")
                {
                    ["pluginassemblyid"] = new EntityReference("pluginassembly", assemblyId),
                    ["typename"] = typeName,
                    ["name"] = activity.Value,
                    ["friendlyname"] = activity.Value,
                    ["workflowactivitygroupname"] = group
                };

                if (typeId.HasValue)
                {
                    // the same id, so the Power Platform solution's identity file still matches
                    type.Id = typeId.Value;
                }

                try
                {
                    service.Create(type);
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    Assert.Fail($"Dataverse wouldn't register {typeName}: {ex.Detail.Message}");
                }
            }
        }
    }
}
