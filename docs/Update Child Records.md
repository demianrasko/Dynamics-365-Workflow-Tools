The Update Child Records allows you to update multiple child records from a parent record. You can update one field of the Childs based on a dynamic value or a parent field, or based on string value.

For using this action you need to select the action:

![](Update%20Child%20Records_wf1.png)

Then fill all the parameters:

![](Update%20Child%20Records_wf2.png)

The parameters are:
* **Parent Record URL**: the URL of the parent record (recover dynamic on the workflow)
* **Relationship Name**: The name of the relationship between the parent and child entity
* **Parent Field Name**: (optional) the schema name of the field in the parent entity 
* **Value to Set**: (optional) the string value to be set (if the previuos one is empty)
* **Child Field Name to Update**: The destination field name on the child entity

NOTES:
1) The relationship Name must existe on CRM
2) The parent and child field types must to be the same 
3) for Boolean fields please include "1" for true and "0" for false
