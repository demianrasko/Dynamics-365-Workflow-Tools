# AI Draft Reply

**Assembly:** `powerplatformWorkflowTools`

## Purpose
- Generate a suggested response for customer communications using Dataverse Copilot reply capabilities.

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
- Available only in Dataverse online environments; the action does not ship with the on-premises build.
- Combine the **Reply Text** output with a workflow step that creates an email activity or note so users can review the generated response before sending.
- The input should already be language-detected by Dataverse; provide localized text for the best results.

## Example Workflow
1. Add **AI Draft Reply** to a cloud workflow triggered from emails or timeline notes.
2. Map the original message body to **Text To Reply To**.
3. Store the **Reply Text** output in a new email activity draft or custom text field for user review.

## Error Handling
- Empty input text causes the activity to return **Failed = true** with the message `Text is empty.`
- When Dataverse does not return a `PreparedResponse`, the activity flags failure and surfaces the service error in **Failure Message** to aid troubleshooting.
