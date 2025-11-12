This action summarizes long-form text with Dataverse Copilot so agents can review the highlights quickly.

**Workflow Step Name:** AI Summarize Text

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Text To Summarize | String | Yes | Long-form content that should be condensed into a summary. |

## Outputs
| Name | Type | Description |
| --- | --- | --- |
| Summary Text | String | Summarized version of the provided input. |
| Failed | Boolean | `true` when the activity cannot obtain a summary. |
| Failure Message | String | Details about the failure when `Failed` is `true`. |

## Usage Notes
- Store the **Summary Text** output in a note, description, or custom field to aid agents.
- You can combine this action with `AI Translate Text` to present summaries in the customer’s preferred language.

## Example Workflow
1. Add **AI Summarize Text** to a workflow that receives lengthy support ticket descriptions.
2. Map the original description to **Text To Summarize**.
3. Present the **Summary Text** to agents or include it in email notifications.

## Error Handling
- When the input text is blank the activity sets **Failed = true** with `Text is empty.`
- If the Dataverse service omits the `SummarizedText` field, the activity marks failure and records the service error in **Failure Message**.

## Dataverse Reference
- [AISummarize](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/aisummarize?view=dataverse-latest)
