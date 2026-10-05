using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Test-only columns on account that some tests need (a choice and a multi-select choice with three options each).
    /// They are created the first time they are needed and kept, so later runs don't wait for metadata changes.
    /// </summary>
    public abstract partial class IntegrationTestBase
    {
        /// <summary>A choice (picklist) column on account with three options.</summary>
        protected const string TestChoice = "new_wfttestchoice";

        /// <summary>A multi-select choice column on account with three options.</summary>
        protected const string TestChoices = "new_wfttestchoices";

        /// <summary>A text column on account with field security turned on.</summary>
        protected const string TestSecret = "new_wfttestsecret";

        private static readonly ConcurrentDictionary<string, int[]> OptionValues = new ConcurrentDictionary<string, int[]>();

        /// <summary>
        /// The option values of a test column on account (none for <see cref="TestSecret"/>), creating the column first
        /// when it doesn't exist.
        /// </summary>
        protected int[] TestOptions(string column)
        {
            return OptionValues.GetOrAdd($"{ConnectionVariable}|{column}", _ => EnsureTestColumn(column));
        }

        private int[] EnsureTestColumn(string column)
        {
            var metadata = RetrieveColumn(column) ?? CreateTestColumn(column);

            return metadata is EnumAttributeMetadata choice
                ? choice.OptionSet.Options.Select(o => o.Value.Value).OrderBy(v => v).ToArray()
                : new int[0];
        }

        private AttributeMetadata RetrieveColumn(string column)
        {
            try
            {
                return ((RetrieveAttributeResponse)Service.Execute(new RetrieveAttributeRequest
                {
                    EntityLogicalName = EntityNames.Account,
                    LogicalName = column,
                    RetrieveAsIfPublished = true
                })).AttributeMetadata;
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                return null;
            }
        }

        private AttributeMetadata CreateTestColumn(string column)
        {
            var options = new OptionSetMetadata
            {
                IsGlobal = false,
                OptionSetType = OptionSetType.Picklist
            };

            foreach (var name in new[] { "One", "Two", "Three" })
            {
                options.Options.Add(new OptionMetadata(new Label(name, 1033), null));
            }

            AttributeMetadata attribute;

            if (column == TestSecret)
            {
                attribute = new StringAttributeMetadata { MaxLength = 100, IsSecured = true };
            }
            else if (column == TestChoices)
            {
                attribute = new MultiSelectPicklistAttributeMetadata { OptionSet = options };
            }
            else
            {
                attribute = new PicklistAttributeMetadata { OptionSet = options };
            }

            attribute.SchemaName = column;
            attribute.LogicalName = column;
            attribute.DisplayName = new Label($"WFT test {column}", 1033);
            attribute.RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None);

            Service.Execute(new CreateAttributeRequest { EntityName = EntityNames.Account, Attribute = attribute });
            Service.Execute(new PublishXmlRequest
            {
                ParameterXml = $"<importexportxml><entities><entity>{EntityNames.Account}</entity></entities></importexportxml>"
            });

            // a new column takes a moment before records can be read and written with it
            WaitFor(() =>
            {
                Service.RetrieveMultiple(new QueryExpression(EntityNames.Account) { ColumnSet = new ColumnSet(column), TopCount = 1 });

                return true;
            });

            return RetrieveColumn(column);
        }

        /// <summary>Whether a column of a table has the given option, including unpublished changes.</summary>
        protected bool HasOption(string entityName, string column, int value)
        {
            // metadata changes show up after a short delay
            return WaitFor(() => ReadOption(entityName, column, value), expected: true) || ReadOption(entityName, column, value);
        }

        /// <summary>Whether a column of a table doesn't have the given option (waits for a removal to show up).</summary>
        protected bool LacksOption(string entityName, string column, int value)
        {
            return WaitFor(() => !ReadOption(entityName, column, value), expected: true);
        }

        private bool ReadOption(string entityName, string column, int value)
        {
            var metadata = (EnumAttributeMetadata)((RetrieveAttributeResponse)Service.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = column,
                RetrieveAsIfPublished = true
            })).AttributeMetadata;

            return metadata.OptionSet.Options.Any(o => o.Value == value);
        }

        /// <summary>
        /// Repeats a check until it returns <paramref name="expected"/> (or stops throwing), for up to a minute, because
        /// metadata changes reach the data and metadata caches with a delay. Returns whether it got there.
        /// </summary>
        protected static bool WaitFor(Func<bool> check, bool expected = true)
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    if (check() == expected)
                    {
                        return true;
                    }
                }
                catch (FaultException<OrganizationServiceFault>)
                {
                }

                Thread.Sleep(2000);
            }

            return false;
        }

        /// <summary>Whether a table exists.</summary>
        protected bool TableExists(string logicalName)
        {
            try
            {
                Service.Execute(new RetrieveEntityRequest { LogicalName = logicalName, EntityFilters = EntityFilters.Entity });

                return true;
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                return false;
            }
        }

        /// <summary>Whether a table has a column.</summary>
        protected bool HasColumn(string entityName, string column)
        {
            try
            {
                Service.Execute(new RetrieveAttributeRequest { EntityLogicalName = entityName, LogicalName = column });

                return true;
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                return false;
            }
        }
    }
}
