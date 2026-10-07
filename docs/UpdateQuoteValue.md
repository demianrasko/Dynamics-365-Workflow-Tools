This action sets a money field on a quote, typically the quote discount.

**Workflow Step Name:** Update Quote Value

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Quote | Lookup (Quote) | Yes | The quote to update. |
| Discount Amount | Decimal | Yes | The amount to set. |
| Field name to update (discountamount) | String | Yes | Logical name of the money field, usually `discountamount`. |

## Notes
- Requires Dynamics 365 Sales; not part of the Power Platform version.
