This step is very usefull to query an entity, filtering with one or two field conditions, and get one or two fields.
Could be very usefull to retrieve some parameters located in another entities such as parameters entities.

For use this in Workflows here are the steps:

First select the step:
![](Query%20Values%20Step_workflowActivity5.gif)

Then, fill all the required fields:
![](Query%20Values%20Step_workflowActivity6.gif)

The full fields description are:
* **EntityName (required)** : the schema name of the entity to be searched
* **Attribute1 (required)** :  first attribute to be retrieved
* **Attribute2** :  second attribute to be retrieved
* **FilterAttribute1 (required)** :  first filter attribute name 
* **ValueAttribute1 (required)** :  first filter attribute value 
* **FilterAttribute2** :  second filter attribute name 
* **ValueAttribute2** :  second filter attribute value 
* **ResultValue1** :  retrieved value for the first attribute
* **ResultValue2** :  retrieved value for the second attribute 

IMPORTANT NOTE: since version 1.0.36.0 the Attribute2 Parameter is required. for previous versions, please remember to fill something on it.

Then you can use the retrieved values in following steps of the Workflow:
![](Query%20Values%20Step_workflowActivity7.gif)

### Getting the URL of the record found

Query Values returns column values as text: an ID column gives the record's GUID, and a lookup gives the GUID of the record it points to. To turn that into a link to the record, add a **Get Record URL** step after it:

1. **Query Values:** set **Attribute1** to the table's ID column, for example `contactid` when EntityName is `contact`.
2. **Get Record URL:**
   * **Reference Record URL** = *Record URL (Dynamic)* of the workflow's record (its address is reused for the new link)
   * **Record ID** = *ResultValue1 (Query Values)*
   * **Entity Logical Name** = `contact`
3. Use its **Record URL** output, for example in an email or a URL field.

The same works with a lookup column in Attribute1: set Entity Logical Name to the table the lookup points to. If Query Values finds nothing, ResultValue1 is empty and Get Record URL stops the workflow with "Record ID '' is not a valid GUID", so check that ResultValue1 contains data first.





