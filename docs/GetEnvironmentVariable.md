This step reads an environment variable, so a workflow can use settings (URLs, ids, feature switches) that differ between environments without being edited.

It returns the variable's current value when one is set in this environment, otherwise its default value.

### Inputs

* **Schema Name**: the schema name of the environment variable, for example `new_ApiUrl` (shown in the solution, under the environment variable's Advanced options).

### Outputs

* **Value**: the current value, or the default value when no current value is set. Empty when the variable doesn't exist or has no value at all.
* **Found**: Yes when a value (current or default) was found, No otherwise.

### Notes

* Values are returned as text, whatever the variable's type (text, number, Yes/No, JSON). A data source or secret variable returns what is stored in the variable, not the secret itself.
* The step runs as the workflow's user, who needs read access to the Environment Variable Definition and Environment Variable Value tables.
* Available in both the Dynamics 365 and the Power Platform versions.
