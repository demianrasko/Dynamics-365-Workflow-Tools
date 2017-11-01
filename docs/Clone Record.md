This step is very usefull to Clone some record. You pass the URL of one record, and it will create a new record, with the same values.

For using this activity you mus access here and select Clone Record:

![](Clone%20Record_wfclone.gif)

Then, you must select the record URL(dynamic) field from the entity you want to clone as follows:

![](44_1.png)

The full params description is:
* **Clonning Record URL (required)** : the URL of the record you want to clone
* **Prefix (optional)** : the prefix will be addedd at the name attribute of the clonned record 
* **Fields to Ignore (optional)** : the list of attributes you want to ignore in cloning, separate by ";"

* **Output parameter: Cloned Guid** : the string with the GUID of the new record, you can use it like this:
![](44_2.png)

Note: The Parent Record URL, is a standard feature of Dynamics CRM, taht contains the full URL of a record. In this URL you have the entity type, and the record GUID. Right now this is the only way we have to pass a "Dynamic" EntityReference (with not hard coding an entity type) to Workflows Activities. If you pass this string URL as a parameter, in the Workflow Activity you can retrieve this entity Reference.
