# Ship Happens: GitHub Actions Beyond CI/CD

**45 minutes · level 200 · English · ExcuseOps**

Audience: developers using any language, OS, or cloud. C# is our specimen, not a
prerequisite. Goal: go from **zero active workflows** to one tested ZIP promoted
through TEST and a human-gated PRODUCTION, then show a genuinely independent
GitHub Agentic Workflow.

## Safety and delivery status

All cloud, GitHub configuration, commit, push, workflow-dispatch, and cleanup
commands below are **presenter instructions, NOT actions executed while preparing
these materials**. No repository was created, nothing was pushed or deployed, and
no remote run was started. Local validation is not evidence of a successful cloud
deployment. Rehearse with your own non-sensitive demo repository before the talk.

Initial delivery has **no active YAML under `.github/workflows`**. The local
composite action's `action.yml` is not a workflow. Snapshots under `workflows`
and compiler-produced backups under `agentic/compiled` are inert.

## Exact 45-minute rundown

| Time | Segment | Hard stop / fallback |
|---|---|---|
| 00–04 | Hook / story | Two audience excuses; show the app. |
| 04–07 | Mental model / runners | Draw event → workflow → jobs → steps. |
| 07–17 | Demo 1: build, test, ZIP artifact | By 15:00 show the artifact or local backup. Composite included; explain matrix as the next snapshot enhancement. |
| 17–26 | Demo 2: TEST, matrix, OIDC | By 24:00 show TEST or explain the rehearsed run; do not debug Azure live. |
| 26–33 | Demo 3: PRODUCTION approval | By 31:00 show the gate. Do not wait past 32:00 for deployment. |
| 33–36 | Beyond CI/CD / security | Three practical patterns and a security checklist. |
| 36–42 | Demo 4: Agentic Workflows | Dispatch at 37:30; show a rehearsed staged preview by 40:00 if necessary. |
| 42–45 | Takeaways / questions | Three takeaways; end at 45:00. |

These are wall-clock boundaries, not estimated execution times. Keep the browser
and editor at 150–175% zoom. Pre-open the repository Actions tab, both apps, the
production environment settings, a successful rehearsed run, and a staged agentic
preview. Use a visible timer. Never show `.azure`, authentication dialogs, keys,
shell history containing secrets, or App Service configuration values on screen.

## Files and conceptual map

```text
ExcuseOps.slnx
global.json                          .NET 10 stable only; never .NET 11 RC
src/ExcuseOps.Web/                    minimal API + static English UI
tests/ExcuseOps.Tests/                domain + in-process HTTP integration tests
azure.yaml                           one azd service: web / dotnet / appservice
infra/main.bicep                      subscription entry point; RG per azd environment
infra/web.bicep                       B1 Linux App Service + app-scoped OIDC identity
infra/main.parameters.json           environment substitutions; secure key input
workflows/01-ci.yml                   simple single-job CI
workflows/02-test.yml                 Linux + Windows test matrix → ZIP → TEST
workflows/03-production.yml           same graph → human-gated PRODUCTION
.github/actions/release-summary/      real local composite action
agentic/repo-digest.md                manual-only Markdown agentic workflow source
agentic/compiled/                     actual compiler output, inactive offline backup
scripts/Set-DemoStage.ps1             activate exactly one CI/CD snapshot, or stage 0
scripts/Prepare-AgenticDemo.ps1       copy Markdown and optionally compile; never run remotely
scripts/Publish-Demo.ps1               package correct ZIP root, print SHA256
scripts/Assert-DeploymentConfig.ps1   fail fast, no secret values printed
scripts/Test-DeployedApp.ps1          health + public + anonymous/VIP smoke tests
```

API contract:

- `GET /health` → `healthy`, app name, deployment stage.
- `GET /api/excuses` → one funny excuse; optional `category=code|cloud|meetings`
  (case-insensitive) and integer `variant=0..1000` for repeatable results.
- Unknown categories / invalid variants → HTTP 400.
- `GET /api/vip-excuse` needs `X-Demo-Key` matching `Demo:ApiKey`.
  Missing/wrong keys → 401; unconfigured service → 503; correct key → VIP excuse.
- No real key is embedded in HTML, code, artifact, or output. Test fixture strings
  are explicitly non-credential values. This header is an intentionally tiny
  secret demo, **not** a production identity design or substitute for OAuth.

## Before the conference: local prerequisites

Use PowerShell **7+**, .NET **10 stable SDK**, Git, Azure CLI with Bicep, Azure
Developer CLI (`azd`), GitHub CLI (`gh`), and the `github/gh-aw` extension. Hosted
Linux and Windows runners already have PowerShell 7. Adapt shell syntax for other
platforms; the application and workflows are cross-platform.

Use this `demo` directory as the **root** of your eventual demo repository, not
the parent conference materials directory. Do not publish unrelated decks or
sibling event directories.

```powershell
Set-Location -LiteralPath 'C:\Users\mabonann\OneDrive\Documents\Tecnici\Sessioni Eventi\2026\20261107 - DevFest - Pescara\Materiale\Scout\ExcuseOps-GitHub-Actions-2026\demo'
$PSVersionTable.PSVersion
dotnet --version
azd version
az bicep version
gh --version
gh aw --version
```

The original machine had .NET 6, 9, and **11 RC**, but not 10. The required first
`dotnet test` correctly failed SDK selection. Official SDK **10.0.402** was then
installed session-locally, without changing the global installation:

```powershell
$dotnet = 'C:\Users\mabonann\.scout\copilot\session-state\e5c75acd-4ad4-42a4-adfb-4f260522f527\files\dotnet\dotnet.exe'
& $dotnet --version
& $dotnet test ExcuseOps.slnx --configuration Release --logger trx
.\scripts\Publish-Demo.ps1 -Dotnet $dotnet
```

On your own machine with .NET 10 installed, use `$dotnet = 'dotnet'` instead.
`global.json` starts at `10.0.100`, rolls within stable .NET 10 feature bands,
and disables prerelease selection. Actions installs `10.0.x` explicitly.

Run locally (keep this terminal running; use a second terminal for smoke tests):

```powershell
$env:Demo__ApiKey = 'local-demo-fixture-not-a-deployment-key'
$env:DeploymentStage = 'local'
& $dotnet run --project .\src\ExcuseOps.Web --urls http://localhost:5180
```

Browse `http://localhost:5180`. Ask for a category. Show the deterministic API
with `http://localhost:5180/api/excuses?category=cloud&variant=0`.
In the second terminal:

```powershell
$env:APP_URL = 'http://localhost:5180'
$env:DEMO_API_KEY = 'local-demo-fixture-not-a-deployment-key'
$env:EXPECTED_STAGE = 'local'
.\scripts\Test-DeployedApp.ps1 -AllowLocalHttp
Remove-Item Env:DEMO_API_KEY
```

Useful preflight compilation; **no deployment**:

```powershell
az bicep build --file .\infra\main.bicep --outfile .\infra\main.json
```

Local preparation validation: 17 tests passed, publish ZIP created and extracted,
extracted application served HTTP/UI, and the smoke script passed. All three
snapshots passed actionlint 1.7.12. Bicep compiled with 0.47.16. Agentic source
compiled in strict mode with installed `gh aw` **v0.89.21**; no model was called.
Windows validation was executed locally; the hosted Linux/Windows matrix and
Azure login/deploy still require the presenter's remote rehearsal.

Compatibility note: actionlint 1.7.12 understands all three ordinary CI/CD
snapshots, but reports the compiler-generated `concurrency.queue` and
`copilot-requests` fields as unknown. These are emitted by gh-aw, not handwritten
YAML. Do not remove them to satisfy an older linter. The generated workflow was
checked by the version-matched compiler; rehearse it on your GitHub tenant, whose
Agentic Workflows/inference support is a separate prerequisite.

## Before the conference: Azure provisioning (presenter only; NOT executed)

You need a subscription allowed to create Linux App Service B1 plans, resources,
user-assigned managed identities, federated identity credentials, and app-scoped
role assignments. The provisioning principal needs resource-creation privileges
plus role-assignment rights (for example Contributor plus Role Based Access
Control Administrator on the intended demo scope). Do **not** give the eventual
GitHub deployment identities these provisioning rights.

Use a region supporting App Service Linux .NET 10. Check before provisioning:

```powershell
# Read-only discovery after you sign in; do not show account details on stage.
az webapp list-runtimes --os linux --output tsv | Select-String 'DOTNETCORE:10.0'
```

There are two **separate apps / B1 plans**, not slots (B1 does not provide staging
slots). Reuse the same template for two azd environments:

| azd environment | `DEPLOYMENT_STAGE` | GitHub environment | OIDC subject |
|---|---|---|---|
| `excuseops-test` | `test` | `test` | `repo:OWNER@OWNER_ID/REPO@REPO_ID:environment:test` |
| `excuseops-prod` | `production` | `production` | `repo:OWNER@OWNER_ID/REPO@REPO_ID:environment:production` |

`production` is deliberately spelled out everywhere except the azd environment
suffix `-prod`. Case in `OWNER/REPO` must match the actual GitHub OIDC subject.
The immutable subject retains the names and appends the numeric owner and
repository IDs. `OWNER_ID` identifies the repository's owning user or organization,
not the workflow actor. Retrieve both IDs with `gh api repos/OWNER/REPO --jq '{ownerId: .owner.id, repositoryId: .id}'`.
The templates require `githubOwnerId` and `githubRepositoryId`, mapped from
`GITHUB_REPOSITORY_OWNER_ID` and `GITHUB_REPOSITORY_ID` in the azd environment.

GitHub must emit the immutable subject: opt existing repositories into this format
in their GitHub OIDC settings before running deployment workflows. See the
[Microsoft Entra migration guide](https://learn.microsoft.com/en-us/entra/workload-id/workload-identities-github-immutable-subjects).
Reprovisioning with these templates replaces the existing stage credential's
subject; it does not retain a parallel legacy credential. Coordinate this change
with the GitHub OIDC setting off stage to avoid failed logins during migration.

**Run only yourself, off stage, after replacing the placeholders:**

```powershell
azd auth login
az login
$subscriptionId = 'YOUR-AZURE-SUBSCRIPTION-GUID'
$repo = 'OWNER/REPO'
$githubOwnerId = 'OWNER_ID'
$githubRepositoryId = 'REPO_ID'
$location = 'westeurope'

azd env new excuseops-test --subscription $subscriptionId --location $location
azd env set DEPLOYMENT_STAGE test
azd env set GITHUB_REPOSITORY $repo
azd env set GITHUB_REPOSITORY_OWNER_ID $githubOwnerId
azd env set GITHUB_REPOSITORY_ID $githubRepositoryId

# A protected password prompt; not a shell literal or azd env set.
$secureKey = Read-Host 'TEST demo key (24+ characters; save securely)' -AsSecureString
$env:DEMO_API_KEY = [System.Net.NetworkCredential]::new('', $secureKey).Password
azd provision
if ($LASTEXITCODE -ne 0) { throw 'TEST provisioning failed; do not continue.' }
# Keep the exact same key in your password manager for the GitHub test environment.
Remove-Item Env:DEMO_API_KEY
$secureKey.Dispose()

azd env new excuseops-prod --subscription $subscriptionId --location $location
azd env set DEPLOYMENT_STAGE production
azd env set GITHUB_REPOSITORY $repo
azd env set GITHUB_REPOSITORY_OWNER_ID $githubOwnerId
azd env set GITHUB_REPOSITORY_ID $githubRepositoryId
$secureKey = Read-Host 'PRODUCTION demo key (different, 24+ characters)' -AsSecureString
$env:DEMO_API_KEY = [System.Net.NetworkCredential]::new('', $secureKey).Password
azd provision
if ($LASTEXITCODE -ne 0) { throw 'PRODUCTION provisioning failed; do not continue.' }
Remove-Item Env:DEMO_API_KEY
$secureKey.Dispose()
```

PowerShell process environment temporarily holds plaintext because azd must pass
the secure deployment parameter. Never print it or enable verbose shell tracing.
The Bicep parameter is `@secure()`, mapped to server setting `Demo__ApiKey`, and
never returned as an output. `.azure` is ignored but may contain sensitive local
state: do not share it or show `azd env get-values` in a recording. Re-enter the
matching key when reprovisioning; changing it requires updating the matching
GitHub environment secret too.

**Provision vs deploy:** `azd provision` creates infrastructure and settings.
`azd deploy web` packages and ships local application code; `azd up` does both.
This session deliberately provisions **before** the event and lets **Actions**
perform code deployment. Do not run `azd deploy` during promotion: it would bypass
the tested artifact and approval narrative.

If you need an off-stage initial smoke app, the manual command is `azd deploy web`
after selecting the intended azd environment; clearly separate this bootstrap
from the CI/CD demonstration. It is not required by the walkthrough.

### OIDC and least privilege

The template creates `id-excuseops-test-github` and
`id-excuseops-prod-github`. Neither is attached to the web app as a runtime
identity: each exists solely for deployment. Each federated credential trusts
issuer `https://token.actions.githubusercontent.com`, audience
`api://AzureADTokenExchange`, and **only its own environment subject**.

Each identity has built-in **Website Contributor**
(`de139f84-1756-47ae-9be6-808fbbe84772`) scoped to **its one web app**, not the
subscription, resource group, or plan. This includes the SCM deployment
permissions `Microsoft.Web/sites/publish/action` and config access required by
the deployment action. The action receives `resource-group-name` explicitly,
avoiding subscription-wide app discovery. Website Contributor can read/change
that app's configuration: least privilege here means **deployment capability
on one app**, not an identity incapable of seeing that app's settings.

HTTPS and TLS 1.2 are required. FTP and SCM basic publishing authentication are
disabled. Azure login uses a short-lived Entra token, not a publish profile or a
stored Azure password. Do not "fix" a 403 by enabling basic auth or granting
subscription Contributor. Check the scoped role, app name/resource-group input,
OIDC subject, policy, and RBAC propagation instead.

### GitHub environments, variables, secrets, and reviewers

Use a **public repository containing only non-sensitive demo content** for the
smoothest session. On GitHub Free, Pro, and Team, **required reviewers are
available for public repositories only**. A private repository needs a suitable
Enterprise plan for this approval scenario. Check current account/organization
policy; YAML cannot enable this feature or configure a reviewer.

In repository **Settings → Environments**, create **`test`** and **`production`**
before running the deployment snapshots. For **both**, configure deployment
branches/tags → selected branches → `main` only. This complements, rather than
replaces, the YAML trusted-event guard.

For each azd environment, view the non-secret values in Azure Portal or obtain
only the selected outputs below, privately:

```powershell
azd env select excuseops-test
foreach ($name in 'AZURE_CLIENT_ID','AZURE_TENANT_ID','AZURE_SUBSCRIPTION_ID','AZURE_WEBAPP_NAME','AZURE_RESOURCE_GROUP','APP_URL') {
    Write-Host "$name = $(azd env get-value $name)"
}
# Repeat with: azd env select excuseops-prod
```

Under each GitHub environment → **Environment variables**, add:

| Name | Value |
|---|---|
| `AZURE_CLIENT_ID` | that stage's deployment managed identity client ID |
| `AZURE_TENANT_ID` | tenant GUID |
| `AZURE_SUBSCRIPTION_ID` | subscription GUID |
| `AZURE_WEBAPP_NAME` | that stage's app name from Bicep output |
| `AZURE_RESOURCE_GROUP` | `rg-excuseops-test` or `rg-excuseops-prod` |
| `APP_URL` | that app's `https://...azurewebsites.net` origin URL |

IDs and endpoint names are configuration, **not passwords**. Use environment
variables, not repository secrets, for these. The application URI check prevents
sending a demo key to an arbitrary endpoint.

Under each environment → **Environment secrets**, create **`DEMO_API_KEY`** with
the exact corresponding key used for provisioning. It is not an Azure login
credential. The post-deployment VIP request is what genuinely exercises secrets.
Do not also put this key in repository variables or browser storage.

For **production** → **Deployment protection rules**:

1. Enable **Required reviewers**, add a trusted reviewer, and save.
2. If **Prevent self-review** is enabled, use a **different eligible person**
   to approve the run. Invite that reviewer before the session and rehearse.
3. Disable administrator bypass if your policy requires it; demonstrate the
   actual behavior, not an implied guarantee.
4. Show that `environment: production` merely selects this configured gate.
   It does **not** create a human approval rule by itself.

`permissions: contents: read` is the default token boundary. Only the deployment
jobs add `id-token: write` to request OIDC tokens. That permission does not grant
Azure access by itself: federation and scoped Azure RBAC do. PRs run tests and
package only; no deployment job executes for PR events, forks, or manual runs on
other branches. Do not change to `pull_request_target` to expose credentials.

All CI/CD snapshots use the same workflow-level concurrency group for each ref
and `cancel-in-progress: false`. The **whole promotion** is serialized, including
the approval wait; a newer TEST cannot overtake an older run waiting for PROD.
Actions concurrency keeps at most one pending run and can replace an older
pending run; it is not a FIFO release queue. Avoid repeated pushes during a demo.
Queued newer main runs intentionally wait for approval/rejection of the current
promotion. Do not use separate per-environment locks as a substitute for this
whole-release serialization.

## Before the conference: agentic setup (presenter only; NOT executed remotely)

This is **GitHub Agentic Workflows (`gh-aw`)**, not a chat agent writing ordinary
CI YAML. The Markdown source contains frontmatter policy plus a bounded natural
language task. `gh aw compile` produces the real Actions **`.lock.yml`**.

The delivered source was locally compiled with **v0.89.21**, already installed on
this machine. A newer official release **v0.92.0** was available when researched;
it was not silently substituted. For a reproducible off-stage setup on a new
machine:

```powershell
# Install only if gh aw is missing. This installs a local CLI extension,
# not a repository or workflow, and does not start a remote run.
gh extension install github/gh-aw --pin v0.89.21
gh aw --version
```

If your organization uses another supported version, recompile and rehearse with
that version; do not hand-edit a lock file to appease the compiler.

The current source uses **`engine: copilot`** and
**`copilot-requests: write`**. The compiled workflow passes its short-lived
`github.token` to Copilot inference; the permission is for inference, not
repository mutation. Verify Agentic Workflows availability, Copilot entitlement,
selected model availability, organization policies, and billing **before** the
talk. If this inference-auth path is unavailable in your organization, follow
the official engine/auth guide for an approved provider/token, update the source,
and recompile/rehearse. Do not improvise long-lived tokens in front of an audience.
Other engines have their own provider credentials, policy, and charges.

Repository tools have `contents/issues/pull-requests: read`; only the four
allow-listed listing/searching tools are exposed, each capped at two calls and
restricted to `${{ github.repository }}`. No checkout, shell, web browsing, Azure
identity, deployment environment, or source editing is provided. The prompt
limits selection to five issues/five PRs updated in seven days. Untrusted issue
or PR text is data, not authority to change instructions.

`safe-outputs.create-issue.max: 1` and **`safe-outputs.staged: true`** produce a
preview rather than an issue. Safe outputs are a separate permission-controlled
job, not direct agent writes; generated supporting jobs may request `issues:
write`, but staged mode skips publishing. Staging is not a substitute for
reviewing the generated workflow and policy. Do not remove it for this talk.

The installed compiler accepted `timeout-minutes: 5` and `max-turns: 12`.
These are runtime/turn controls, **not a guaranteed currency cap**. Check actual
provider billing. If there is no recent data, "No recent issues or pull requests
found" is a valid, honest outcome. Do not seed fake activity and pretend it is
real project history. Public content from external contributors can be filtered
by gh-aw's default integrity controls; use a few approved, harmless rehearsal
issues/PRs if you want a predictable populated digest.

Compile off stage in your **local demo Git repository**:

```powershell
.\scripts\Prepare-AgenticDemo.ps1 -Compile -WhatIf
.\scripts\Prepare-AgenticDemo.ps1 -Compile
# Review the Markdown, .lock.yml, generated .github/aw pins and any .gitattributes.
# DO NOT run gh aw run, gh workflow run, or push as part of local preparation.
```

The compiler may resolve/download public action/container metadata. Local compile
does not call a model and does not execute Actions. Commit the source, generated
lock, and relevant compiler-generated pin/config files together when you
intentionally enable it. The inactive `agentic/compiled` backup is real local
compiler output; it is for inspection/offline explanation, not a fabricated run.

Return to the clean starting point before the session:

```powershell
.\scripts\Set-DemoStage.ps1 -Stage 0 -IncludeAgentic -WhatIf
.\scripts\Set-DemoStage.ps1 -Stage 0 -IncludeAgentic
# This removes only excuseops.yml, repo-digest.md, repo-digest.lock.yml
# from .github/workflows. It preserves all unrelated files.
Get-ChildItem .\.github\workflows -File -ErrorAction SilentlyContinue
```

If you have already published rehearsal workflows to your demo repository, commit
and push that intentional removal before the audience arrives. Do not rewrite
history or use force pushes. Historical runs will remain; say so honestly.

## Repo initialization / off-stage rehearsal

Use a repository you independently created or were given. No repository-creation
command is needed here. Never accidentally initialize the parent materials
directory. The following commands are **manual presenter actions**, not automated
by the stage scripts:

```powershell
# Only if this demo folder is not already its own Git repository:
git init -b main
# Review ignored files and every staged path before the first commit.
git status --short
git add .gitignore global.json ExcuseOps.slnx azure.yaml src tests infra scripts workflows agentic .github/actions DEMO-SCRIPT.md README.txt
git diff --cached --stat
git commit -m "Start ExcuseOps with no active workflows"
git remote add origin https://github.com/OWNER/REPO.git
git push -u origin main
```

Use the actual remote, never the placeholder. Follow your normal authorship and
commit policy. Rehearse all stages beforehand; then restore stage 0 with an
ordinary commit. Save successful run URLs, app URLs, and an agentic staged preview
in your private presenter notes. This implementation does not invent screenshots
or claim remote successes.

## 00–04 — Hook / story

**Say:** "Who has said 'it works on my machine' this month? Keep your hand up if
your machine is now the production plan."

Take two audience excuses, maximum 30 seconds each. Open the local app. Choose
**The cloud**, click **Generate a better excuse**, and read one line.

**Say:** "This is ExcuseOps. The app has no AI, no external services, and
absolutely no accountability. Today we automate the boring parts without
automating away our judgment."

Show the repository tree and the empty workflow directory.
**Say:** "Our code works. Our automation does not exist. Let's earn each box in
the pipeline, one commit at a time."

## 04–07 — Mental model / runners

Draw / point to:

```text
event → workflow → jobs (independent machines) → ordered steps
                         needs: creates dependency edges
```

**Say:** "`on` is the doorbell. A workflow is a YAML file in
`.github/workflows`. A job gets a runner; steps in that job share its workspace.
Another job gets another machine. Your `bin` directory does not teleport."

Ask: "If TEST needs the build output from another runner, do we share a folder?"
Wait for "artifact." Contrast `uses:` (run an action) with `run:` (run a shell
command). `ubuntu-latest` and `windows-latest` are hosted runner labels, not
deployment targets. The cloud destination does not dictate your runner OS.
Mention self-hosted runners: patching, isolation, and credentials are your job;
do not use a privileged persistent self-hosted runner for untrusted fork PR code.

## 07–17 — Demo 1: code → tests → publish ZIP artifact

### 07–09 — Read the contract

Show `Program.cs`, one domain test, and the API key success/rejection tests. Do
not walk every line. **Say:** "Even our excuses have a contract. Invalid category?
400. Wrong VIP key? 401. That is more accountability than most incident updates."

Run the local tests if desired (already restored):

```powershell
& $dotnet test ExcuseOps.slnx --configuration Release --no-restore
```

### 09–11 — Add automation

```powershell
.\scripts\Set-DemoStage.ps1 -Stage 1 -WhatIf
.\scripts\Set-DemoStage.ps1 -Stage 1
git add .github/workflows/excuseops.yml
git diff --cached -- .github/workflows/excuseops.yml
git commit -m "Demo 1: build, test and keep a deployment ZIP"
git push
```

Presenter intentionally executes the commit/push after reviewing the change.
The script itself never commits, pushes, resets, or starts remote runs.

### 11–12:30 — Read the run, not all the YAML

Browser: **repository → Actions → ExcuseOps delivery → latest main run**.
Point out event, commit, runner, checkout, setup, test and package steps.

- `on`: push main, PR targeting main, or explicit manual dispatch.
- `jobs.build.runs-on`: Linux today; C# is not Windows-only.
- `uses: actions/setup-dotnet@<SHA>` versus `run: dotnet test`.
- Predefined `GITHUB_SHA` / `GITHUB_RUN_NUMBER` versus custom `DOTNET_VERSION`.
- Context `${{ github.run_id }}` names `ARTIFACT_NAME`; `${{ env.DOTNET_VERSION }}`
  is evaluated by Actions, `$env:NAME` is read by PowerShell on the runner.
- A context is structured workflow data, not an interchangeable synonym for a
  shell variable. Never splice untrusted PR titles directly into a `run` script.
- Root `permissions: contents: read`; step/job timeouts bound failures.

### 12:30–14 — Keep the evidence

Expected logs: tests pass, publish succeeds, ZIP SHA256 printed, artifact upload
completes. Run summary: the custom **release receipt** and one artifact named
`excuseops-<run_id>-<run_attempt>`.

Download the artifact in the browser. GitHub's downloaded artifact container ZIP
contains **`excuseops.zip`**. That inner ZIP is the deployable one: DLL and
`wwwroot` at its root, not nested under `publish/`. Open the ZIP structure; do not
rebuild it during deployment.

### 14–15:30 — Marketplace and a real custom action

Open `.github/actions/release-summary/action.yml`, then its step summary output.
**Say:** "Marketplace actions are dependencies, not magic. Check the publisher,
source, permissions, release history, and maintenance. A verified badge is not a
security review. We pin full commit SHAs and keep version comments; updates still
need review."

Action types: **JavaScript** for portable Node-based actions; **Docker** for a
containerized Linux toolchain; **composite** to combine existing steps. The one
on screen is a working composite. Local `uses: ./.github/actions/...` needs
checkout first. Inputs go through environment variables, never source-code
interpolation. Summary text is encoded before rendering.

Ask: "Should this pass on Windows too?" Show the matrix diff in snapshot 2;
make the change as part of the next demo, not an extra live branch.

### 15:30–17 — A deliberately red test, then repair

Use a local targeted test to avoid waiting for two more remote runs. In
`tests/ExcuseOps.Tests/ExcuseTests.cs`, find `VariantIsDeterministicAndWraps`.
Temporarily change only the **first equality** from
`ExcuseCatalog.Pick("code", 2)` to `ExcuseCatalog.Pick("code", 1)`.
Leave the subsequent `Assert.NotEqual` unchanged.

```powershell
& $dotnet test .\tests\ExcuseOps.Tests\ExcuseOps.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~CatalogTests.VariantIsDeterministicAndWraps'
```

Expected: the equality assertion fails. **Say:** "This is an intentional teaching
change, not a bug we secretly shipped." Undo that exact edit in the editor,
restore variant **2**, and rerun the same command: expected pass.
**Do not commit or push the deliberate break.** Check `git diff` confirms the
test file has no remaining change before continuing.

If available, show a **real rehearsed** failed Actions run to demonstrate that a
failed test skips publish/upload and dependent deployment. Clearly distinguish
that historical run from the local test just executed. Without a recording,
point to the dependency graph and explain the expected behavior; do not claim
remote failure was observed. Skip the live edit if behind schedule.

**Offline cutoff:** if Actions is slow, open `out/excuseops.zip`, show the local
test result and the snapshot. Say "Local validation, not a remote successful run."
Do not spend the next segment debugging runner queues.

## 17–26 — Demo 2: matrix → dependent TEST deployment

### 17–19 — Extend, don't replace the story

```powershell
.\scripts\Set-DemoStage.ps1 -Stage 2
git diff -- .github/workflows/excuseops.yml
git add .github/workflows/excuseops.yml
git commit -m "Demo 2: test on Linux and Windows, then deploy TEST with OIDC"
git push
```

Show matrix `[ubuntu-latest, windows-latest]`, then `package.needs: test`.
**Say:** "Two independent runners prove the same source works. Only one package
job uploads one uniquely named artifact, after both pass. We avoid two jobs
colliding on the same immutable upload-artifact name."

### 19–22 — Follow the dependency and identity

Graph: both tests → package → Deploy TEST. Open the download step. **Say:** "New
runner, empty disk, explicit download. Dependencies are not shared storage."

Show environment `test`, environment variables, and the **name only** of secret
`DEMO_API_KEY`. Never click reveal. Explain custom `vars`, `env`, `secrets`, and
structured `github` contexts using the actual YAML.

Trace OIDC:

1. The trusted main deployment job requests an OIDC token (`id-token: write`).
2. Azure matches repo + environment subject against the TEST identity's trust.
3. Azure issues a short-lived token with app-scoped RBAC.
4. The deploy action sends the downloaded ZIP; no long-lived Azure credential.

**Ask:** "Is a client ID a password?" Answer: no. Show no real authentication
values, only the variable names and the template's subject.

### 22–26 — Show TEST and a genuine secret use

Open the TEST URL from the environment link. UI says `Environment: test`.
Open `/health`. Generate an excuse.

Open the smoke step: health, normal API, anonymous VIP denial, correct-key VIP
success. Key goes via the header from step-scoped `env`; the script prints only
success/status, never the key or full request. It disables redirects to avoid
sending the header to a different host.

**Say:** "Secret masking is a last line of defense, not permission to print
secrets. Our app receives its key at provisioning time; Actions uses the matching
GitHub environment secret to prove protected behavior after deployment."

Show PR deployment guard. Fork PRs may test but do not get this environment or
OIDC deployment job. No `pull_request_target`.

**Hard cutoff 24:00:** if Azure is not ready, use a real rehearsed run/recording.
If none exists, show the local smoke outcome and explain what remains unverified.
At 26:00 move on regardless.

## 26–33 — Demo 3: promote the same ZIP, human approval

### 26–28 — Add the final edge

```powershell
.\scripts\Set-DemoStage.ps1 -Stage 3
git diff -- .github/workflows/excuseops.yml
git add .github/workflows/excuseops.yml
git commit -m "Demo 3: promote the tested ZIP behind production approval"
git push
```

Show `deploy_production.needs: [package, deploy_test]`, environment `production`,
same artifact name and path. No second `dotnet publish`. The PROD job gets its
own client ID, app name, endpoint, and key from its environment, not TEST's.

**Say:** "Promotion changes the destination, not the bits. We rebuild neither
the application nor our confidence after the TEST check."

### 28–31 — Make the gate visible

Browser: **Settings → Environments → production → Required reviewers**.
**Say:** "This checkbox is the approval mechanism. YAML merely asks to enter
the environment. If I forget this setting, there is no human gate."

Return to the run. After TEST succeeds, PRODUCTION should display **Waiting**
and **Review deployments**. Before approval, production deployment steps and its
environment secrets are not released to that job. Ask the audience: "Would you
approve this release? What evidence would you want?"

The designated eligible reviewer clicks **Review deployments → production →
Approve and deploy**. If self-review prevention is enabled, the initiating
presenter cannot approve their own run; use the rehearsed second person.

### 31–33 — Read the receipt

Show PROD UI with `Environment: production`, `/health`, and green VIP smoke test.
Compare the two download step artifact names in the **same run**.

If asked about rollback: retain the prior known-good artifact and deliberately
redeploy it through the same controlled process. This sample does not implement
one-click rollback; retention is seven days, and App Service B1 has no slots.
Do not rerun only a failed deployment job across changed attempts casually:
artifact naming includes `run_attempt`. Re-run **all jobs** to produce one
coherent fresh graph and re-approve. Never paper over a failed TEST check.

**Offline fallback:** show a real recorded approval wait; without a recording,
walk the graph and the configured gate, explicitly labeling it unexecuted.
Do not describe the inactive template as proof the approval worked.

## 33–36 — Beyond CI/CD and security

Three non-deployment examples: scheduled maintenance/reporting, issue/PR
triage, and release/documentation chores. Deterministic tasks still belong in
ordinary Actions; adding an LLM does not improve a reliable `dotnet test`.

Rapid checklist, with the audience finishing each sentence:

- "Each job gets… an isolated runner."
- "A dependency edge does not transfer… files."
- "Secrets are not… configuration IDs."
- "Production approval is configured in… the environment settings."
- "The default token should have… the least permissions it needs."

Cost honesty: standard hosted runners are free for public repositories;
private repositories have plan quotas and overages. Artifact storage, larger
runners, Azure resources, and AI inference have separate billing. Free public
Actions does **not** mean free App Service or free AI. Check current pricing.

## 36–42 — Demo 4: an independent GitHub Agentic Workflow

### 36–37:30 — Source → compiler → Actions

Open `agentic/repo-digest.md`.
**Say:** "Normal Actions tells the computer exactly how. Here we bound the tools
and outcomes, then ask an agent to reason about noisy repository information.
This is GitHub Agentic Workflows, not 'Copilot wrote my YAML.'"

Point to manual trigger, read-only repository tools, same-repository allow list,
no checkout/shell, twelve-turn cap, five-minute agent timeout, and staged max-one
issue proposal. The natural-language instructions treat issue/PR text as
untrusted data and require linked evidence and explicit uncertainty.

```powershell
.\scripts\Prepare-AgenticDemo.ps1 -Compile
git status --short
# Review all generated files first. Explicit paths only:
git add .github/workflows/repo-digest.md .github/workflows/repo-digest.lock.yml
# If generated by your compiler, also review and stage .github/aw/actions-lock.json
# and .gitattributes; do not indiscriminately stage local credentials or artifacts.
if (Test-Path .github/aw/actions-lock.json) { git add .github/aw/actions-lock.json }
if (Test-Path .gitattributes) { git add .gitattributes }
git commit -m "Demo 4: add a manual staged repository digest"
git push
```

The generated lock is standard Actions YAML and is the executable workflow.
Markdown is the authored source of truth; always recompile source changes.
The regular CI workflow also reacts to this push; the agentic workflow itself is
independent and **manual only**. It has no `needs` edge into deployment.

### 37:30–40 — Start once, then inspect a preview

Browser: **Actions → ExcuseOps repository digest (preview only) → Run workflow →
main → Run workflow**. This is an explicit, intentional **presenter** action.
The setup scripts never dispatch it.

While it runs, ask: "Would you let an issue body tell your release agent to
ignore policy?" Explain instructions vs data, tool restrictions, and why read
access plus a safe-output boundary matters. Do not promise perfect AI safety.

Expected run: activation / agent / supporting validation and safe-output jobs.
Open the summary's **staged preview** of a proposed issue, with at most three
evidence-linked findings and at most two human suggestions. Show the Issues tab:
no issue should have been published. Empty recent activity is acceptable.
Review whether the reasoning is actually supported; a green run is not proof
that the model is correct.

### 40–42 — Explain the boundary honestly

**Say:** "The deterministic pipeline promotes the exact tested ZIP. The agentic
workflow drafts an interpretation. It has no cloud identity and cannot deploy.
Its output is a proposal, not a fact, and a human reviews it."

Provider authentication, availability, organization policy, and billing differ
from `GITHUB_TOKEN` repository permissions. Supported versions and preview
features move quickly: pin, compile, inspect, and rehearse.

**Offline fallback:** open `agentic/compiled/repo-digest.lock.yml` beside its
source and a genuine saved staged-run recording if available. A local compile
is demonstrable without cloud access; inference and preview execution are not.
Never type a made-up lock file or fabricated agent response as a live result.

## 42–45 — Takeaways / questions

**Say:**

1. "A workflow is a graph of isolated jobs, not one long shared terminal."
2. "Build once; move an artifact; protect identity and production separately."
3. "Use deterministic automation for rules and bounded agents for interpretation.
   Keep people accountable for consequential outputs."

Final callback: "Ship happens. At least now we know which commit did it."
Take questions until 44:45; at 45:00 give the repository/reference location.
For learning paths and certification, point to GH-200 and GitHub Actions training,
not to a claim this 45-minute session covers the whole exam.

## Recovery cheatsheet

| Symptom | Off-stage diagnosis / safe on-stage response |
|---|---|
| .NET SDK not found / .NET 11 RC selected elsewhere | Run from demo root; use the explicit local .NET 10 executable. Do not loosen global.json to .NET 11. |
| Restore fails | Check NuGet connectivity/proxy; use the previously validated local build and explain it is a backup. |
| Workflow not visible | YAML must be under `.github/workflows` in the repo root; snapshots are intentionally inactive. Manual workflows need to be on the default branch. |
| No run after push | Check branch `main`, repository Actions policies/allowed pinned actions, and whether the commit actually changed the active workflow path. |
| Local composite action missing | Checkout must precede local `uses`. GitHub sees the demo folder as repository root. |
| Artifact not found | Check exact run ID/attempt name, retention, successful upload; re-run all jobs, not only deployment with a new attempt name. |
| Deploy ZIP has no app DLL at root | Use Publish-Demo.ps1; do not deploy the browser artifact wrapper ZIP or zip the parent `publish` folder. |
| OIDC login fails | Exact owner/repo case, environment `test`/`production`, issuer/audience, IDs, tenant, branch restriction, and `id-token: write`. |
| Azure 403 on deploy | Wait for RBAC propagation; check app-scoped Website Contributor and explicit resource-group-name. Do not enable publish profiles or broaden to subscription Contributor. |
| Deployment succeeds but app startup fails | Verify region runtime .NET 10, startup command, app root ZIP, bounded startup delay and App Service logs privately. Never print app settings. |
| VIP 401 | GitHub environment key differs from provisioned `Demo__ApiKey`; replace through approved secret handling, not YAML literals. |
| VIP 503 | App setting missing; provision with secure DEMO_API_KEY. Anonymous VIP should be 401 only once configured. |
| PROD does not wait | Stop: configure the actual production required-reviewer rule and confirm plan/repository visibility support. A YAML environment alone is insufficient. |
| Reviewer cannot approve | Check eligibility and prevent-self-review; use the prearranged second reviewer, not an emergency bypass. |
| New run queued behind approval | Intended whole-promotion concurrency. Finish or reject current promotion deliberately; don't repeatedly push. |
| Agentic compile fails | Check installed gh-aw version, strict diagnostics, source schema and public metadata access. No manually fabricated lock. |
| Agentic run lacks inference auth/model | Check entitlement/policy/billing before retry; fall back to source + real compiled backup, not invented output. |
| Agent digest empty or partially filtered | Honest result; recent activity/integrity filters can hide items. Explain scope rather than broaden access live. |
| Staged summary looks inaccurate | Discuss the unsupported inference and reject it. Green workflow status is not human approval. |

## Cleanup and cost warning (presenter only; NOT executed)

**Two B1 plans continue charging even when nobody is using the apps. Stopping the
web apps does not delete the plans or stop the plan cost.** Delete demo resources
after the event. Review the environment and subscription carefully first:

```powershell
azd env select excuseops-test
azd down
azd env select excuseops-prod
azd down
```

Read the interactive resource/deletion prompts. Do not use force flags in a
shared subscription. Confirm `rg-excuseops-test` / `rg-excuseops-prod` contain only
the expected disposable resources. Resource-group deletion also removes the
stage identities, federated credentials, and app-scoped assignments. Verify
deleted resources in Azure Portal. Remove obsolete environment variables/secrets
and Actions artifacts when appropriate, and disable/remove the active workflows
with an ordinary commit if the public demo repository should stay dormant.

Locally: stop only the application process you started (Ctrl+C in its terminal),
clear process secret variables, and remove disposable `out`, test results, or
`.azure` state only when you no longer need it. Do not delete sibling materials.

## References (reviewed 21 October 2026)

- [GitHub Actions documentation](https://docs.github.com/en/actions)
- [Workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax)
- [Deployment environments / protection rules](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments)
- [Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)
- [Azure App Service + GitHub Actions / OIDC](https://learn.microsoft.com/en-us/azure/app-service/deploy-github-actions)
- [azd project schema](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/azd-schema)
- [Agentic Workflows: creating workflows](https://github.github.com/gh-aw/setup/creating-workflows/)
- [Frontmatter](https://github.github.com/gh-aw/reference/frontmatter/)
- [Safe outputs / staged mode](https://github.github.com/gh-aw/reference/safe-outputs/)
- [Engine authentication and policy](https://github.github.com/gh-aw/reference/engines/)
- [gh-aw v0.89.21 source / version-matched docs](https://github.com/github/gh-aw/tree/v0.89.21)
- [GH-200 study guide](https://learn.microsoft.com/en-us/credentials/certifications/resources/study-guides/gh-200)
- [GitHub Actions training](https://learn.microsoft.com/en-us/training/paths/github-actions/)
- [More GitHub Actions training](https://learn.microsoft.com/en-us/training/paths/github-actions-2/)
