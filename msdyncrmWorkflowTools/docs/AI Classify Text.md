# AI Classify Text

**Assembly:** `powerplatformWorkflowTools`

## Purpose
- Route inbound messages or cases by comparing free-form text against a list of categories and returning the highest-confidence match.

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Text To Classify | String | Yes | Free-form content that needs to be categorized. |
| Categories (Comma Separated) | String | Yes | Comma-separated list of at least two candidate categories. Duplicates are removed automatically. |

## Outputs
| Name | Type | Description |
| --- | --- | --- |
| Classification | String | Category label returned by Dataverse AI based on the provided text. |
| Failed | Boolean | `true` when the request could not be completed. |
| Failure Message | String | Additional error details when `Failed` is `true`. |

## Usage Notes
- Only available in Power Platform (Dataverse online) deployments; the action is excluded from the on-premises build.
- Provide descriptive, mutually-exclusive categories. When fewer than two unique categories are supplied the activity exits with an error.
- Combine with a workflow `Switch` or conditional steps to branch on the returned `Classification` value.

## Example Workflow
1. Add **AI Classify Text** to your cloud workflow.
2. Map a description or email body to **Text To Classify**.
3. Enter a comma-separated list such as `Sales, Support, Billing` for **Categories (Comma Separated)**.
4. Use the **Classification** output in subsequent steps to assign queue, team, or routing logic.

## Error Handling
- Empty text or category inputs cause the activity to set **Failed = true** with a descriptive **Failure Message**.
- If Dataverse does not return a classification result, the activity flags failure and includes the original service error for troubleshooting.
