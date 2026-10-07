using System;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Links that open a record, a new record form or a table's default view in the Dynamics 365 mobile app.
    /// </summary>
    public sealed class MobileDeepLinks
    {
        public MobileDeepLinks(string entityName, Guid id)
        {
            Edit = $"ms-dynamicsxrm://?pagetype=entity&etn={entityName}&id={id}";
            New = $"ms-dynamicsxrm://?pagetype=create&etn={entityName}";
            DefaultView = $"ms-dynamicsxrm://?pagetype=view&etn={entityName}";
        }

        /// <summary>Opens the record.</summary>
        public string Edit { get; }

        /// <summary>Opens a new record form of the table.</summary>
        public string New { get; }

        /// <summary>Opens the table's default view.</summary>
        public string DefaultView { get; }
    }
}
