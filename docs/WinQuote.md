This action closes a quote as won (WinQuote), creating the quote close activity.

**Workflow Step Name:** Win Quote

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Quote | Lookup (Quote) | Yes | The quote to win. |
| Message | String | No | Subject of the quote close activity. |

## Notes
- The quote must be active.
- Requires Dynamics 365 Sales; not part of the Power Platform version.
