This action translates text into a target language through Dataverse Copilot so workflows can deliver multilingual content automatically.

**Workflow Step Name:** AI Translate Text

## Inputs
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| Text To Translate | String | Yes | Content that needs to be translated. |
| Target Language (e.g. en, fr, es) | String | No | Two-letter language code. When omitted, Dataverse selects a best-fit translation target. |

## Outputs
| Name | Type | Description |
| --- | --- | --- |
| Translated Text | String | Translated version of the input text. |
| Failed | Boolean | `true` when the translation fails. |
| Failure Message | String | Error details when `Failed` is `true`. |

## Usage Notes
- Provide ISO language codes such as `en`, `fr`, or `es` in **Target Language** for deterministic results.
- Chain with **AI Summarize Text** or other workflow activities to present multilingual summaries or responses.

## Example Workflow
1. Add **AI Translate Text** to a workflow that receives customer emails.
2. Map the email body to **Text To Translate** and specify the agent’s preferred language in **Target Language**.
3. Use **Translated Text** to populate an email, note, or custom field.

## Error Handling
- Blank input text sets **Failed = true** with the message `Text is empty.`
- When Dataverse returns no `TranslatedText` value, the activity flags failure and records the service error in **Failure Message**.

## Dataverse Reference
- [AITranslate](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/aitranslate?view=dataverse-latest)
