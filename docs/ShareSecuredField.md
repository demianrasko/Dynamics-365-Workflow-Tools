This action shares a secured field (column security) of a record with a user and/or a team, updates their existing access, or removes it.

**Workflow Step Name:** Share Secured Field

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Record URL | String | Yes | Record URL (Dynamic) of the record whose field is shared. |
| Attribute Name | String | Yes | Logical name of the secured field. |
| Share With User | Lookup (User) | No | User to share the field with. |
| Share With Team | Lookup (Team) | No | Team to share the field with. |
| Allow Read | Boolean | Yes (default true) | Grant read access. |
| Allow Update | Boolean | Yes (default true) | Grant update access. |

## Notes
- If neither Allow Read nor Allow Update is set, the existing share is removed.
- If the field is not secured, nothing is shared (the step traces this).
- Fill in the user, the team, or both.
