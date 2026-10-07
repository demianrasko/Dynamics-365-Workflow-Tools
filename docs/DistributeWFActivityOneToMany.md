This action starts an on-demand workflow for every record related to the current record through a one-to-many relationship (for example, every contact of an account).

**Workflow Step Name:** Distribute Workflow (One To Many)

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Relationship Name | String | Yes | Schema name of the 1:N relationship, e.g. `contact_customer_accounts`. |
| Distributed Workflow | Lookup (Process) | Yes | The on-demand workflow to run for each related record. |

## Notes
- All related records are processed, page by page (no 5,000-record limit).
