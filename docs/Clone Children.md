This step is very usefull to Clone Child records. You pass the URL of one record and the relationship name, and it will clone the child records associating them with a new parent.

For using this activity you must access here and select Clone Children:

![](Clone%20Children_1.gif)

Then, you have to define the parameters for the source and destination parent records, as well as the clone options for the children.

The full params description is:
* **Source Record URL (required)** : the URL of the parent record that contains the child records you want to clone.
* **Target Record URL (required)** : the URL of the parent record that will contain the cloned child records.
* **Relationship Name (required)** : the schema name for the relationship to use to find the child records.
* **New Parent Field Name (required)** : the schema name for the Lookup field on the child records for the new parent.
* **Old Parent Field Name (optional)** : the schema name for the Lookup field on the child records for the original parent. This optional parameter is used in case the cloned children should be linked to the new parent using a different lookup field. If not specified it will use the same as the New Parent lookup.
* **Prefix (optional)** : the prefix will be addedd at the name attribute of the clonned record. 
* **Fields to Ignore (optional)** : the list of attributes you want to ignore in cloning, separate by ";" (in lowercase)



Note: The Record URL, is a standard feature of Dynamics CRM, that contains the full URL of a record. In this URL you have the entity type, and the record GUID. Right now this is the only way we have to pass a "Dynamic" EntityReference (with not hard coding an entity type) to Workflows Activities. If you pass this string URL as a parameter, in the Workflow Activity you can retrieve this entity Reference.
