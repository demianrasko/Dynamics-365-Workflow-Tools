## Set Lookup Field from Record URL

This action lets you set a lookup field on the current entity using a Record URL. This is useful if you need to set a lookup value but are unable to get an actual entity reference. 
For example, you cannot set a lookup field with the Update Child Records workflow, but you could set a field on the child records to the record URL and then run a workflow on the child to set the lookup field from that record URL with this action.

### Parameters
**Record URL:** The record URL of the record that you want to put into the lookup field.

**Lookup Field Name:** The name of the lookup field that you want to set on the entity your workflow is running against. Eg. "pub_userlookup"
