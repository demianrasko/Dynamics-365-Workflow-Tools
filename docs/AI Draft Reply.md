This action generates a draft reply for customer communications through Dataverse Copilot so agents can respond faster.

**Workflow Step Name:** AI Draft Reply

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Text To Reply To | String | Yes | Message body or note content that should receive an automated reply suggestion. |

## Outputs
| Name | Type | Description |
| --- | --- | --- |
| Reply Text | String | Draft response returned by Dataverse AI. |
| Failed | Boolean | `true` when the service call fails or no response text is available. |
| Failure Message | String | Explanation of the failure condition when `Failed` is `true`. |

## Usage Notes

## Example Workflow
1. Add **AI Draft Reply** to a cloud workflow triggered from emails or timeline notes.
2. Map the original message body to **Text To Reply To**.
3. Store the **Reply Text** output in a new email activity draft or custom text field for user review.

## Error Handling
- Empty input text causes the activity to return **Failed = true** with the message `Text is empty.`
- When Dataverse does not return a `PreparedResponse`, the activity flags failure and surfaces the service error in **Failure Message** to aid troubleshooting.

## Dataverse Reference
- [AIReply](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/aireply?view=dataverse-latest)

Requires the Dataverse AI functions (AI Builder) to be available and enabled in the environment; calls use AI Builder credits.

Originally contributed by [rwilson504](https://github.com/rwilson504) (demianrasko/Dynamics-365-Workflow-Tools#297).
