# AI Sentiment Detect

**Assembly:** `powerplatformWorkflowTools`

**Dataverse Reference:** [AISentiment](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/aisentiment?view=dataverse-latest)

## Purpose
- Determine the sentiment (positive, negative, neutral, or mixed) of customer-facing content inside a workflow.

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Text To Analyze Sentiment | String | Yes | Content whose sentiment should be analyzed. |

## Outputs
| Name | Type | Description |
| --- | --- | --- |
| Sentiment | String | Sentiment label returned by Dataverse AI. |
| Failed | Boolean | `true` when sentiment detection fails. |
| Failure Message | String | Diagnostic details when `Failed` is `true`. |

## Usage Notes
- Only available in Power Platform online environments; not compiled into the on-premises solution.
- Use the **Sentiment** output with your workflow branching logic to escalate negative interactions or acknowledge positive ones.
- For longer messages, consider trimming irrelevant content before calling the activity to reduce noise.

## Example Workflow
1. Insert **AI Sentiment Detect** after retrieving an incoming email or case description.
2. Map the relevant text column to **Text To Analyze Sentiment**.
3. Route the record based on the **Sentiment** output (for example, escalate when the value equals `negative`).

## Error Handling
- Empty input text results in **Failed = true** with the message `Text is empty.`
- Missing `AnalyzedSentiment` values from the service response also set **Failed = true** and expose the issue through **Failure Message**.
