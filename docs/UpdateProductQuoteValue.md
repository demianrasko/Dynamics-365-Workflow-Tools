This action sets a money field on a quote product (quote line), typically the manual discount.

**Workflow Step Name:** Update Product Quote Value

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Quote Product | Lookup (Quote Product) | Yes | The quote product to update. |
| Discount Amount | Decimal | Yes | The amount to set. |
| Field name to update (manualdiscountamount) | String | Yes | Logical name of the money field, usually `manualdiscountamount`. |

## Notes
- Requires Dynamics 365 Sales; not part of the Power Platform version.
