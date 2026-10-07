This action starts an on-demand workflow for every record related to the current record through a many-to-many relationship.

**Workflow Step Name:** Distribute Workflow (Many To Many)

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Relationship Name | String | Yes | Schema name of the N:N relationship. |
| Distributed Workflow | Lookup (Process) | Yes | The on-demand workflow to run for each related record. |

## Notes
- All related records are processed, page by page (no 5,000-record limit).
