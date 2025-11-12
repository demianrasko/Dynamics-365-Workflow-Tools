This action summarizes a Dataverse record via Copilot using the record URL and optional context so agents stay informed.

**Workflow Step Name:** AI Summarize Record

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Record URL (Dynamic) | String | Yes | Standard Dynamics record URL that contains the entity type code (`etc`) and record identifier (`id`). |
| Additional Record Context JSON | String | No | JSON payload with extra signals for Copilot (for example, `{ "channel": "email" }`). |
| Include Catchup Changes (Lead/Opportunity only) | Boolean | No | When `true`, includes catch-up insights alongside the summary for supported entities. |

## Outputs
| Name | Type | Description |
| --- | --- | --- |
| Summary Text | String | Summary returned by Dataverse AI. |
| Failed | Boolean | `true` when the operation fails (invalid URL, metadata issues, or service errors). |
| Failure Message | String | Detailed error information when `Failed` is `true`. |

## Usage Notes
- The activity parses the provided record URL to determine entity logical name and ID. Ensure the URL has both `etc` and `id` parameters.
- When **Include Catchup Changes** is enabled, Dataverse merges recent timeline insights with the summary for leads and opportunities.
- Use **Additional Record Context JSON** to pass optional metadata required by your Copilot configuration.

## Example Workflow
1. Insert **AI Summarize Record** into a workflow triggered on leads, opportunities, or other supported entities.
2. Map the current record’s URL (via the **Record URL (Dynamic)** workflow parameter) to **Record URL (Dynamic)**.
3. Optionally populate **Additional Record Context JSON** and toggle **Include Catchup Changes** where appropriate.
4. Store the **Summary Text** output in a note, email notification, or custom field for agents.

## Error Handling
- Missing or malformed URLs cause **Failed = true** with the message `Record URL is required.` or a parsing failure message.
- If metadata lookup cannot resolve the logical name (for example, unsupported entity), the activity fails with `Unable to resolve entity logical name from Record URL.`
- Service errors from Dataverse (including missing `SummarizedText`) propagate to **Failure Message** for easier troubleshooting.

## Dataverse Reference
- [AISummarizeRecord](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/aisummarizerecord?view=dataverse-latest)
