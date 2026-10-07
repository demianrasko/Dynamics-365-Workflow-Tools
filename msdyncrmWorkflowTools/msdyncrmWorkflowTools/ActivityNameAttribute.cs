using System;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// The name a workflow activity has in the workflow designer, e.g. "Add Role To Team". tools\Build-Solutions.ps1
    /// registers the activity with it (name and friendly name); every activity needs one.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ActivityNameAttribute : Attribute
    {
        public ActivityNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
