This action starts an on-demand workflow for every record returned by a FetchXML query.

**Workflow Step Name:** Execute Workflow For Records In Query

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Query | String | No | FetchXML query; when empty, the step does nothing. |
| Process | Lookup (Process) | When a query is given | The on-demand workflow to run for each record. |

## Notes
- All records the query returns are processed, page by page (no 5,000-record limit).
