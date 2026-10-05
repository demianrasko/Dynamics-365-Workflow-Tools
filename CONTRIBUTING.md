This is a Community Solution, so please share your ideas, code, issues, etc.
If you have any new thing to be included, please open an issue to track it.
If you have developed some new features, please create a pull request, and I'll check it to merge in the main branch.

Thanks for your contribution!!

Demian.

## Creating a new activity

Every activity follows the same pattern: the activity class only reads and checks its inputs, and the work is done by a method in the shared Common project, where it can be unit tested. The [templates/WorkflowActivity](templates/WorkflowActivity) folder has a starting point for each piece.

### Where the code goes

| What | Where |
| --- | --- |
| The activity: inputs, outputs and input checks | `msdyncrmWorkflowTools\msdyncrmWorkflowTools\Class\` |
| Code that calls Dataverse | `Common\CommonClass\Common.<Area>.cs` (pick the file that fits, e.g. `Common.Records.cs`, `Common.Email.cs`) |
| Code that doesn't call Dataverse (parsing, formatting, calculations) | `Common\UtilityClass\Utility.<Area>.cs` |
| `QueryExpression` builders | A public static `<Name>Query` method in the `Common.<Area>.cs` file that uses it, so it can be tested without Dataverse |
| A new area for Common, Utility or the tests | A new `partial class` file in the same folder, linked into `msdyncrmWorkflowTools.csproj` like the others (Common and Utility only) |
| Small classes the shared code returns (e.g. `RecordUrl`), one per file | `Common\UtilityClass\` (link new files into `msdyncrmWorkflowTools.csproj` too) |
| Table and column names | `Common\EntityNames.cs` and `Common\AttributeNames.cs` |
| Tests | `msdyncrmWorkflowTools_Tests\` (`Common_Tests.<Area>.cs`, `Utility_Tests.<Area>.cs`) |

### Steps

1. **Create the activity class.** Copy [Activity.cs](templates/WorkflowActivity/Activity.cs) into the `Class` folder, rename the file, and replace `$safeitemname$` with the class name. Add the file to `msdyncrmWorkflowTools.csproj` (Visual Studio does this when you use Add > Existing Item).
   - Inherit from `WorkflowActivityBase` and override `ExecuteActivity`. The base class creates `Common`, traces the start and end, and turns exceptions into readable errors, so don't add your own try/catch.
   - Throw `InvalidPluginExecutionException` for messages the user should see as written, such as a missing input.
   - Use `EntityNames` constants in `[ReferenceTarget]` and anywhere else a table name is needed.
   - Give the class an `[ActivityName("...")]` attribute with the name people see in the workflow designer, e.g. `[ActivityName("Add Role To Team")]`. Every activity needs one and the names must be unique (a test checks); the solution build registers the activity under it.
2. **Add the Common method.** Paste [CommonMethod.cs.txt](templates/WorkflowActivity/CommonMethod.cs.txt) into the `Common.<Area>.cs` file that fits and rename it. Use `Service` for Dataverse calls, `Trace` for the trace log, and `AttributeNames` constants for column names. Logic that doesn't need Dataverse goes in `Utility` as a static method.
3. **Add tests.** Paste [CommonTest.cs.txt](templates/WorkflowActivity/CommonTest.cs.txt) into the `Common_Tests.<Area>.cs` file that fits. `FakeOrganizationService` records every request, so you can check what was sent without a Dataverse environment.
4. **Document it.** Copy [Docs.md](templates/WorkflowActivity/Docs.md) into the `docs` folder with the activity's display name (for example `docs\My Activity.md`), add a numbered link to it at the end of the list in `README.md`, and add the page and any screenshots to the solution's Solution Items > docs (> images) folders.
5. **Build and test** both versions (see below).

### Visual Studio item template

To get the activity as an Add > New Item choice in Visual Studio, zip the two files `Activity.cs` and `WorkflowActivity.vstemplate` from `templates\WorkflowActivity` (the files themselves, not the folder) and copy the zip to `Documents\Visual Studio 2022\Templates\ItemTemplates\Visual C#`. After restarting Visual Studio, "Workflow Tools Activity" appears in the Add New Item dialog and fills in the class name for you.

### Keeping existing workflows working

Workflows store the activity's class name and the names and types of its inputs and outputs. Once an activity has been released:

- Never rename the class, its namespace, or any input or output property, even to fix a typo.
- Never change an input or output's type, or remove one.
- Adding a new optional input (without `[RequiredArgument]`) is fine. Steps saved before the input existed may read it as the type's default (empty, 0 or No) rather than the `[Default]` value, so choose a meaning where that keeps the old behavior, and test it against an existing installation.
- Keep the assembly version at 1.0.x so existing installs upgrade in place; changing the major or minor version registers a second assembly.

### Dynamics 365 tables and the Power Platform version

The Power Platform version (`/p:PowerPlatform=true`) must import into Dataverse environments without the Dynamics 365 apps. If the activity needs a Dynamics 365 table (for example lead, list, salesliterature, opportunity, quote, product, uom or incident), wrap the whole activity file like this:

```csharp
// Not in the Power Platform build: it needs Dynamics 365 tables (quote).
#if !POWERPLATFORM
...
#endif
```

Then add the activity to `Dynamics365Activities` in `PowerPlatformBuild_Tests.cs` and to the list at the end of `README.md`.

### Building and testing

```
msbuild msdyncrmWorkflowTools\msdyncrmWorkflowTools.sln /t:Restore /p:RestorePackagesConfig=true
msbuild msdyncrmWorkflowTools\msdyncrmWorkflowTools.sln /p:Configuration=Release
msbuild msdyncrmWorkflowTools\msdyncrmWorkflowTools\msdyncrmWorkflowTools.csproj /p:Configuration=Release /p:PowerPlatform=true
```

Then run the tests from Test Explorer. Build the Power Platform version before running `PowerPlatformBuild_Tests`, because those tests compare the two DLLs.

### Building the solution files

`tools\Build-Solutions.ps1` builds both assemblies and packs the solutions people import, managed and unmanaged, into `dist\` (it needs the [Power Platform CLI](https://learn.microsoft.com/power-platform/developer/cli/introduction)):

- `Dynamics365WorkflowTools_<version>.zip` / `_managed.zip` with `msdyncrmWorkflowTools.dll`
- `PowerPlatformWorkflowTools_<version>.zip` / `_managed.zip` with `powerplatformWorkflowTools.dll`

Each activity is registered under its `[ActivityName]` (the name in the workflow designer). The version is the assembly version in `Properties\AssemblyInfo.cs`; keep it 1.0.x so imports upgrade the installed solution. The `solution` folder keeps each solution's identity: its name, the publisher, and the ids of the assembly and of every activity. Those ids must never change, or an import would add a second assembly instead of upgrading the first. A new activity gets an id the first time the script runs, written to the identity file, so commit that change. The script stops if an activity in the identity file is missing from the assembly, because removing it from the solution would break the workflows that use it.

The **Packaging** integration test imports the built unmanaged Power Platform solution into the Power Platform test environment, to prove Dataverse accepts it.

### Integration tests (real Dataverse environments)

Unit tests (`msdyncrmWorkflowTools_Tests`) use a fake organization service and run everywhere. The integration tests are a separate project, `msdyncrmWorkflowTools_IntegrationTests`, that runs `Common` against real environments: every test in `IntegrationTestBase` runs once against a Dynamics 365 environment and once against a plain Dataverse (Power Platform) environment. Without the environments set up they are inconclusive.

The goal is an integration test for every `Common` method that talks to Dataverse, so problems show up before an activity is tried in a workflow.

To set them up, use **test environments only** (the tests create and delete records named `WFT-Test ...`):

1. Create an app registration (client ID and secret) and add it to each environment as an application user with a security role (System Administrator is simplest in a test environment).
2. Run `tools\Set-DataverseTestConnections.ps1`. It checks each connection with `tools\Test-DataverseAppUser.ps1` and saves `DATAVERSE_CONNECTION` (Dynamics 365) and `DATAVERSE_CONNECTION_PP` (Power Platform) as user environment variables.
3. Restart Visual Studio, then run the `msdyncrmWorkflowTools_IntegrationTests` project in Test Explorer.

Writing tests:

- Add them to `IntegrationTestBase` (in a partial file per area, like `IntegrationTestBase.Records.cs`) so they run against both environments. Tests that need Dynamics 365 tables (quotes, leads, marketing lists, cases) go in `Dynamics365_IntegrationTests`.
- Create test data with `Create(...)`, or call `DeleteAfterTest(...)` for records the code creates, so it is deleted when the test ends, even if it fails.

The same project has more groups (Test Explorer > group by Traits); **Packaging** is described above:

- **Deployment**: registers the built Power Platform assembly (`/p:PowerPlatform=true`) in the Power Platform environment, the way the Plugin Registration Tool does, and checks that it loads in the sandbox, that every activity registers and that the designer sees every input and output. The assembly stays registered, so real test workflows can use it. The Dynamics 365 build isn't deployed this way while the Dynamics 365 test environment has the managed workflow tools solution installed (the manual upgrade test uses it).
- **LiveServices**: the outside services, for real. Currency conversion (Frankfurter) needs no key; geocoding needs `AZURE_MAPS_KEY` and translation `TRANSLATOR_KEY` (plus `TRANSLATOR_REGION` for a regional resource). Save them with `tools\Set-ServiceKeys.ps1`; without a key the test is inconclusive.

In GitHub, the **Integration tests** workflow (`.github\workflows\integration-tests.yml`) runs the same tests. It only runs when started by hand from the Actions tab, so pull requests from forks can't reach the environments. It needs a GitHub environment named `dataverse-test` (repository Settings > Environments) with the secrets `DATAVERSE_CONNECTION` and `DATAVERSE_CONNECTION_PP` (and optionally `AZURE_MAPS_KEY`, `TRANSLATOR_KEY` and `TRANSLATOR_REGION`). Adding yourself as a required reviewer there makes every run wait for your approval. The **Build and test** workflow runs only the unit tests.
### Code style

- Braces on every `if` and `else`, even for one line.
- At most one blank line in a row, and none directly after `{` or before `}`.
- `var`, string interpolation (not `string.Format` or `+`), `string.IsNullOrEmpty`, `string.Empty`, and C# keywords (`string`, not `String`).
- `using` directives in plain alphabetical order.
- Name Dataverse requests `request` and responses `response`.
