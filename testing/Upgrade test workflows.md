# Upgrade test workflows

Classic workflows to build in **tldsandbox** while it runs Demian's published **Dynamics365WorkflowTools 1.0.61.1**, so the upgrade to our build can be checked step by step. Together they use all 79 activities in 1.0.61.1. The activities added since 1.0.61.1 get their own set later.

**The plan:**

1. Create the test data below.
2. Build the 14 workflows below. All of them are **on-demand**, never automatic, and each one ends with a **log note**.
   01 to 03 were built in the designer. `tools\Publish-UpgradeTestWorkflows.ps1` builds 03b to 13, with every input filled in, as drafts (it needs Python 3); open each one in the designer to check it, then activate it.
3. Run each workflow on its test record and keep the log notes and system jobs: this is the 1.0.61.1 baseline. `tools\Invoke-UpgradeTestRun.ps1 -Label before -User <your email>` does it: it refreshes the test data, runs every workflow in turn, collects each one's log notes and checks the records it created or changed, cleans up after, and saves `results.json` and `summary.md` in `testing\results\<date> before\`.
4. Import `Dynamics365WorkflowTools_1_0_100_0_managed.zip` (from `tools\Build-Solutions.ps1`) as an **upgrade**.
5. Run the checks after the upgrade (see the end of this page). For step 4 there, run `tools\Invoke-UpgradeTestRun.ps1 -Label after -User <your email>`, then `tools\Compare-UpgradeTestRuns.ps1 -Before <the before folder> -After <the after folder>`; it writes `comparison.md` with every difference, ids and dates masked.

The script runs the workflows as its application user, so that user is the initiating user (07) and sends the emails (09). Run before and after the same way so the results compare.

Every name starts with **WFT** so the test data is easy to find and remove later.

## The log note

Each workflow ends with a **Create Record → Note** step:

- **Title:** `WFT nn <workflow name>`
- **Regarding:** the workflow's record
- **Description:** the outputs of the steps, one per line, each labelled (for example `Capitalized Text: {Capitalized Text(String Functions)}`).

Those notes are what you compare before and after the upgrade.

## Test data (create once)

`tools\New-UpgradeTestData.ps1 -User <your email>` creates all of this (in the environment in `DATAVERSE_CONNECTION`) and prints the record URLs the steps below ask you to paste. It's safe to run again: it only creates what's missing, including a new scratch account, case and qualify lead once the workflows have used them up. Run it before each test run.

| What | Details |
| --- | --- |
| Account **WFT Upgrade Account** | The main test record. Fill in **Name**, **Account Number** `WFT-001`, **Industry**, **Description** `wft upgrade test`, **Credit Limit**, **Main Phone** `555-0100`, and **Address 1** `1 Microsoft Way, Redmond, WA 98052`. |
| | Set **Primary Contact** to *WFT Contact 1* and **Originating Lead** to *WFT Lead*. |
| | Also set the test columns the integration tests created: **WFT test new_wfttestchoice** to *Two*, and **WFT test new_wfttestchoices** to *One* and *Three*. |
| Contacts **WFT Contact 1**, **WFT Contact 2**, **WFT Contact 3** | **Company Name** = WFT Upgrade Account; emails `wft-1@example.com`, `wft-2@example.com`, `wft-3@example.com`. Deactivate *WFT Contact 3* (for "update only active"). |
| Account **WFT Scratch Account** | Gets deactivated by workflow 04. Create a new one for each run. |
| Notes on WFT Upgrade Account | Two notes with attachments: `wft-a.txt` and `wft-b.pdf` (any small files). |
| Lead **WFT Lead** | Last name, company, email `wft-lead@example.com`. Create another *WFT Lead Qualify* for workflow 11b (it gets qualified). |
| Opportunity **WFT Opportunity** | Account WFT Upgrade Account, a price list, and one product line. |
| Case **WFT Case** | Customer WFT Upgrade Account. Create one per run (workflow 11c resolves it). |
| Team **WFT Team** | An owner team in the root business unit. Add yourself as a member. |
| Security role **WFT Role** | Copy *Basic User* or create an empty role. Assign it **only to yourself**: the email workflows send to everyone who has it. |
| Queue **WFT Queue** | Add 2 tasks (*WFT Task 1*, *WFT Task 2*) to it. |
| Marketing lists | **WFT List** (static, accounts), **WFT List 2** (static, accounts), **WFT Dynamic List** (dynamic, accounts, query: Name begins with "WFT"). |
| Campaign **WFT Campaign** | |
| Goal **WFT Goal** | Any goal metric. You as goal owner, this month. |
| Sales literature **WFT Literature** | One attachment, `wft-brochure.txt`. |
| Email template **WFT Template** | Type *User*, subject "WFT template test". |
| Business process flow **WFT Test BPF** (contact) | Already there. Note its two stage names. |
| Workflow **WFT Test** (account, on-demand) | Already there. ExecuteWorkflowByID runs it. |
| SharePoint | A document location on WFT Upgrade Account (open its Documents tab once). |

## The workflows

The **Inputs** column gives what to enter. *Record URL* means the record's **Record URL (Dynamic)** value from the dynamic values pane.

### 01 WFT Text and numbers — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | String Functions | Input Text = Account Name; Capitalize All Words = Yes; Pad Character `*`, Final Length 30, Pad on the Left = Yes; Replace Old `WFT`, New `Test`, Case Sensitive = No; Substring Start 0, Length 3; Regular Expression `^\w+` |
| 2 | Numeric Functions | Number 1 = 10, Number 2 = 4 |
| 3 | Date Functions | Date 1 = Created On, Date 2 = Modified On |
| 4 | Encrypt Text | Text = Account Number |
| 5 | Json Parser | JSON `{"a":{"b":"wft"}}`, JSON Path `a.b` |
| 6 | Entity Mobile Deep Link | Record URL |
| 7 | Log note | Every output, including the Regex outputs, Text Length and all 13 Date Functions outputs |

### 02 WFT Queries and rollups — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Query Values | EntityName `contact`; Attribute1 `firstname`, Attribute2 `emailaddress1`; FilterAttibute1 `lastname`, ValueAttribute1 `Contact 1`; FilterAttribute2 `emailaddress1`, ValueAttribute2 `wft-1@example.com`. Filter on text columns only: the values are sent as text, so a status, choice, number or lookup filter (e.g. `statecode` = `0`) fails in 1.0.61.1. |
| 2 | Rollup Functions | FetchXML: contacts with `parentcustomerid` = `{PARENT_GUID}`, attribute `numberofchildren` (give the contacts 1, 2 and 3 children) |
| 3 | Concatenate From Query | FetchXML: the same contacts, attribute `fullname`; Separator `, `; Top Record Count 0 |
| 4 | Count Child Entity Records | Child Entity Schema Name `contact`; Parent Lookup Field `parentcustomerid`; Record URL (Parent) = Record URL; FetchXML Filter `<filter><condition attribute="statecode" operator="eq" value="0" /></filter>` |
| 5 | Calculate Agregate Date | FetchXML: aggregate `max(createdon)` of the contacts, grouped by the account |
| 6 | Log note | All outputs |

### 03 WFT Records — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Get Record ID | Record URL |
| 2 | Execute Workflow By ID | Record ID = Record ID (step 1); Process = WFT Test |
| 3 | Get Option Set Value | Source Record URL = Record URL; Attribute `new_wfttestchoice` |
| 4 | Get Multi Select OptionSet | Source Record URL = Record URL; Attribute `new_wfttestchoices`; Retrieve Options Names = Yes |
| 5 | Get Initiating User | — |
| 6 | Get App Module ID | Application Unique Name `msdynce_saleshub` |
| 7 | Get App Record Url | Record URL; Application Unique Name `msdynce_saleshub` (the name, not the ID from step 6) |
| 8 | Log note | RecordID, SelectedValue, SelectedValues, SelectedNames, the initiating user's full name, AppModuleId, AppRecordUrl |

### 03b WFT Clone and child records — Account (run on WFT Upgrade Account)

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Clone Record | Clonning Record URL = Record URL; Prefix `COPY `; Fields to Ignore `accountnumber` |
| 2 | Clone Children | Source Record URL = Record URL; Target Record URL = build `https://<org>/main.aspx?etn=account&id={Cloned Guid}`; Relationship `contact_customer_accounts`; New Parent Field `parentcustomerid` |
| 3 | Update Child Records | Parent Record URL = Record URL; Relationship `contact_customer_accounts`; Value to Set `wft updated`; Child Field `description`; Update only Active = Yes |
| 4 | Update Child Records | The same, but copy Parent Field `telephone1` to Child Field `telephone2` |
| 5 | Set Lookup Field from Record URL | Record URL = WFT Contact 1's URL (pasted); Lookup Field Name `primarycontactid` |
| 6 | Delete Record Audit History | Record URL = the clone's URL (from step 2) |
| 7 | Delete Record | Delete Using Record URL = No; Entity Type Name `account`; Entity Guid = Cloned Guid. This also removes the cloned contacts' parent. Delete the cloned contacts afterwards. |
| 8 | Log note | Cloned Guid; then check the contacts' descriptions (*WFT Contact 3* is inactive and must be unchanged) |

### 04 WFT Status — Account (run on WFT Scratch Account)

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Set State | State 1, Status 2 (inactive) |
| 2 | Log note | — |

### 05 WFT Option sets — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Get Option Set Value | Source Record URL = Record URL; Attribute `new_wfttestchoice` |
| 2 | Get Multi Select OptionSet | Source Record URL; Attribute `new_wfttestchoices`; Retrieve Options Names = Yes |
| 3 | Set Multi Select OptionSet | Target Record URL; Attribute `new_wfttestchoices`; Values = the value of *Two*; Keep Existing Values = Yes |
| 4 | Map Multi Select OptionSet | Source = Record URL, Source Attributes `new_wfttestchoices`; Target = WFT Scratch Account's URL (pasted; a new scratch account needs the step updated, or the workflow built again), Target Attributes `new_wfttestchoices` |
| 5 | Insert Option Value | Global Option Set = No; Attribute `new_wfttestchoice`; Entity `account`; Text `WFT Extra`; Value 100000900; Language Code 1033 |
| 6 | Delete Option Value | The same attribute and entity; Value 100000900 |
| 7 | Log note | Value, Selected Values, Selected Names |

### 06 WFT Relationships — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Associate Entity | Record URL = the WFT Lead URL (pasted; the script prints it in the `etc=` form 1.0.61.1 needs: it reads the table from `etc=`, so an `etn=` URL fails); Relationship Name `accountleads_association`; Relationship Entity Name `accountleads` |
| 2 | Check Associate Entity | The same Record URL; Relationship Name `accountleads` (despite the label, it's the intersect table) |
| 3 | Disassociate Entity | The same Record URL and Relationship Name |
| 4 | Check Associate Entity | Again (`accountleads`): it should now say No |
| 5 | Log note | Both Result values (Yes, then No) |

### 07 WFT Users, teams and roles — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Get Initiating User | — |
| 2 | Retrieve User BU Default Team | User = Initiating User |
| 3 | Check User In Team | Team = WFT Team; User = Initiating User |
| 4 | Is Member Of Team | Team = WFT Team; User = Initiating User |
| 5 | Create Team | Team Name `WFT Created Team`; Type 0; Administrator = Initiating User; Business Unit = the root BU |
| 6 | Add User To Team | Team = Team (from step 5); User = Initiating User |
| 7 | Remove User From Team | The same team and user |
| 8 | Add Role To Team | Role = WFT Role; Team = WFT Team |
| 9 | Remove Role From Team | The same role and team |
| 10 | Check User In Role | Role = WFT Role; User = Initiating User |
| 11 | Add Role To User | Role = Basic User; User = Initiating User. You likely have it already: on 1.0.61.1 this step may fail with a duplicate error, while ours skips it. |
| 12 | Remove Role From User | Role = WFT Role; User = a test user, never yourself if it's your only admin role (the script uses its application user) |
| 13 | Set User Settings | User = Initiating User; PagingLimit 50; everything else at its default |
| 14 | Log note | Initiating User, DefaultTeam, isUserInTeam, Result, Team, isUserInRole. Afterwards delete *WFT Created Team* and set your paging back. |

### 08 WFT Sharing — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Share Record With Team | Sharing Record URL = Record URL; Team = WFT Team; Read and Write = Yes |
| 2 | Share Record With User | The same URL; User = a test user (the script uses its application user); Read = Yes |
| 3 | Share Secured Field | Record URL; Attribute `new_wfttestsecret`; Team = WFT Team; Allow Read = Yes; Allow Update = No |
| 4 | Unshare Record With Team | URL; WFT Team |
| 5 | Unshare Record With User | URL; the test user |
| 6 | Log note | — (check Share / Access on the account between runs) |

### 09 WFT Email — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Create Record: Email | Subject `WFT email test`; To = WFT Contact 1; Regarding = this account |
| 2 | Email To Team | Email = the email from step 1; Team = WFT Team |
| 3 | Entity Attachment To Email | Main Record URL = Record URL; File Name `*.txt`; Email = step 1; Retrieve ActivityMimeAttachment = No; Most Recent Distinct = No |
| 4 | Sales Literature To Email | Email = step 1; Sales Literature = WFT Literature; File Name `*` |
| 5 | Send Email | Email = step 1 |
| 6 | Create Record: Email | Subject `WFT role email` |
| 7 | Send Email To Users In Role | Email = step 6; Security Role = WFT Role |
| 8 | Send Email From Template To Users In Role | Template = WFT Template; Security Role = WFT Role |
| 9 | Log note | Email Subject. Check: step 1's email is sent to WFT Team's members and has `wft-a.txt` and `wft-brochure.txt` attached. |

### 10 WFT Processes and queues — Contact (run on WFT Contact 1)

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Set Process | Record URL = Record URL; Process = WFT Test BPF |
| 2 | Set Process Stage | Record URL; Process = WFT Test BPF; Process Stage Name = the second stage's name |
| 3 | Execute Workflow By ID | Process = WFT Test; Record ID = the Company Name account's ID (from Get Record ID, or paste the WFT Upgrade Account GUID) |
| 4 | Queue Item Count | Source Queue = WFT Queue; Count Only Unassigned = Yes |
| 5 | Pick From Queue | Source Queue = WFT Queue; Quantity 1; Remove Items = No |
| 6 | Queue Item Count | Again: one fewer unassigned |
| 7 | Log note | Both ItemsCount values; check the contact's BPF stage, and that WFT Test ran on the account |

### 11 WFT Sales and marketing

**11a: Account**

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Add To Marketing List | Marketing List = WFT List; Account = this account |
| 2 | Is Member Of Marketing List | Marketing List = WFT List (output: Yes) |
| 3 | Copy Marketing List Members | Source = WFT List; Target = WFT List 2 |
| 4 | Remove From Marketing List | WFT List; Account = this account |
| 5 | Add To Marketing List | Marketing List = WFT List; Account = this account (it's now in WFT List and WFT List 2) |
| 6 | Remove From All Marketing Lists | No inputs: takes this account off every list it's on |
| 7 | Is Member Of Marketing List | Marketing List = WFT List 2 (output: No) |
| 8 | Add Marketing List To Campaign | WFT List; WFT Campaign |
| 9 | Copy To Static List | WFT Dynamic List |
| 10 | Calculate Price | Target Record URL = the opportunity's Record URL (paste WFT Opportunity's URL) |
| 11 | Goal Recalculate | Goal = WFT Goal |
| 12 | Log note | Both IsMemberOfMarketingList values (Yes, then No) |

**11b: Lead (run on WFT Lead Qualify)**

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Qualify Lead | Lead = this lead; Create Account, Contact, Opportunity = Yes; LeadStatus 3 |

**11c: Case (run on a WFT Case)**

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Apply Routing Rule | Incident Record URL = Record URL. Without an active routing rule this fails; that's fine, as long as it fails the same way before and after. |
| 2 | Resolve Case | Case = this case; Case Resolution `WFT resolved`; Description `wft` |

### 12 WFT Settings, apps and SharePoint — Account

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | OrgDB Settings Retrieve | orgDBSetting `trackingprefix` (read only) |
| 2 | OrgDB Settings Update | orgDBSetting `trackingprefix`; Value = String Value from step 1 (writes back the same value) |
| 3 | Get App Module ID | Application Unique Name `msdynce_saleshub` (or any app's unique name) |
| 4 | Get App Record Url | Record URL; the same app name |
| 5 | Get Sharepoint Location URL | Record URL |
| 6 | Entity Json Serializer | Record URL |
| 7 | Calculate Rollup Field | Parent Record URL = Record URL; FieldName `opendeals` |
| 8 | Log note | String Value, App Module ID, the app record URL, SharepointLocationURL, the JSON |

### 13 WFT External services — Account (optional)

| # | Activity | Inputs |
| --- | --- | --- |
| 1 | Currency Convert | Amount 100, From `EUR`, To `USD`. **Expected to fail on 1.0.61.1**: it calls Google's retired service. Ours uses Frankfurter, so put it in its own workflow. |
| 2 | Translate Text | Text `Hola`; Language `pt`; Authentication key = a Translator key (only if you have one) |
| 3 | Geocode Address | Address = Address 1; Bing Maps Key = a key (Bing keys are being retired, so it may fail both before and after) |

## After the upgrade: what to check

1. **The import is an upgrade, not a new install.** Solution Dynamics365WorkflowTools shows **1.0.100.0**. There is one msdyncrmWorkflowTools assembly, still with ID `d0c76739-…`, now with 95 activities.
2. **Every WFT workflow is still activated.**
3. **Every WFT workflow opens in the designer** with all its steps and input values intact, and no "missing custom activity" warnings. The activity group and names now read "msdyncrmWorkflowTools (1.0.100.0)" / "Add Role To Team" instead of class names; that's expected.
4. **Run each workflow again on the same records**, recreating the scratch account, the case and the qualify lead. Compare the new log notes with the 1.0.61.1 ones, and check each system job succeeded.

### Differences that are expected (fixes)

- **Currency Convert** works (13).
- **Add Role To User** no longer fails when the user already has the role (07/11).
- **Associate Entity** used to hide errors; now a failing association fails the step (06 should pass both times).
- **Update Child Records** handles more field types and more than 5,000 children. Behaviour for text and Yes/No fields is unchanged (03b).
- **Rollup Functions Min** was 0 on 1.0.61.1 when values exist; now it's the real minimum (02).
- **Calculate Agregate Date** still returns 1753-01-01 with Ok = No when nothing is found (unchanged).
- **Set User Settings:** on steps saved before the upgrade, DefaultCalendarView and IsSendAsAllowed behave exactly as before. That's the SetUserSettings TODO to confirm here.
- **Clone Record:** the copy is still always created active (unchanged).
- **Currency Convert** working means 13 now reaches **Translate Text**, which fails with HTTP 401 without a Translator key.
- **Send Email To Users In Role** fails with a clear message when nobody has the role ("No enabled user has the security role …") instead of "The e-mail must have at least one recipient" (09).
- **Error messages** name the activity, e.g. "ApplyRoutingRule: Currently there's no active rule to route this case." (11c).
- **Entity Json Serializer** writes compact JSON, and a multi-select column as an array of values (`[100000000,100000002]`) instead of the type name `Microsoft.Xrm.Sdk.OptionSetValueCollection` (12).

Anything else that differs is a regression to look at.

### Results of the upgrade test (6 October 2026)

The first run after the upgrade found a regression: a naming cleanup had changed the case of six arguments (`RecordURL`, `ParentRecordURL`, `MD5HashValue`, `SHA512HashValue`), so 01, 02, 08 and 12 failed with "Value for a required activity argument 'RecordUrl' was not supplied". The names are restored, and the unit test `EveryArgumentOfThePublishedVersionIsUnchanged` now checks every argument of 1.0.61.1. With the fix, 11 of 16 workflows give the same results as before the upgrade and the other 5 differ only as listed above.
