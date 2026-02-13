# CodeReview Agent

A webhook server that automatically performs AI code reviews using Claude Code CLI when pull requests are created in Azure DevOps.

## How It Works

```
Azure DevOps PR Created
        │
        ▼
┌───────────────────-──┐
│  Webhook Endpoint    │  POST /api/webhook/pull-request
│  (filter by author)  │  Returns 202 immediately
└────────┬─────────-───┘
         │  background
         ▼
┌───────────────────-──┐
│  Extract work item   │  feature/PROJ-123 → PROJ-123  (JIRA)
│  from branch name    │  feature/12345    → 12345     (Azure DevOps)
└────────┬────────-────┘
         ▼
┌─────────────────-────┐
│  Fetch work item     │  JIRA Cloud API or Azure DevOps Work Items API
│  details             │  (title, description, acceptance criteria)
└────────┬────────-────┘
         ▼
┌───────────────────-──┐
│  git checkout branch │  Clones on first use, fetches on subsequent runs
│  + get diff          │  Repos cached locally for speed
└────────┬──────────-──┘
         ▼
┌──────────────────-───┐
│  claude -p           │  Runs Claude Code CLI with structured review prompt
│  (code review)       │  Includes PR info + work item context + git diff
└────────┬──────────-──┘
         ▼
┌──────────────────-───┐
│  Save review         │  reviews/{date}_PR-{id}_{ticket}.md
└──────────────────-───┘
```

## Prerequisites

- .NET 8.0 SDK
- Git CLI
- Claude Code CLI (`claude`) installed and authenticated
- ngrok (or similar tunnel) to expose the local server to Azure DevOps

## Setup

1. **Clone and build**

```bash
cd /path/to/CodeReviewAgent
dotnet build
```

2. **Configure**

```bash
cp .env.example .env
```

Edit `.env` with your values. Key settings:

| Setting | Description |
|---------|-------------|
| `CodeReview__TaskSystem` | `Jira` or `AzureDevOps` |
| `CodeReview__AllowedAuthors__0` | PR author name to trigger reviews for |
| `Jira__Domain` / `Jira__ApiToken` | JIRA credentials (if using JIRA) |
| `AzureDevOps__BaseUrl` / `AzureDevOps__Pat` | Azure DevOps credentials |

See `.env.example` for all options with descriptions.

## Launch

1. **Start the server**

```bash
dotnet run --urls "http://localhost:5100"
```

2. **Expose via ngrok** (in a separate terminal)

```bash
ngrok http 5100
```

Copy the `https://xxxx.ngrok-free.app` URL from the ngrok output.

3. **Configure Azure DevOps webhook**

   - Go to your Azure DevOps project
   - **Project Settings** → **Service hooks** → **Create subscription**
   - Service: **Web Hooks**
   - Trigger: **Pull request created**
   - Filters: optionally restrict to a specific repository
   - URL: `https://xxxx.ngrok-free.app/api/webhook/pull-request`
   - Resource details to send: **All**
   - Click **Test** to verify connectivity, then **Finish**

4. **Create a PR** — the review will appear in the `reviews/` directory

## Project Structure

```
CodeReviewAgent/
├── Program.cs                          # Entry point, DI wiring, .env loader
├── Configuration/                      # Options classes + TaskSystemType enum
├── Controllers/
│   └── WebhookController.cs           # POST /api/webhook/pull-request
├── Models/
│   ├── AzureDevOps/                   # Webhook payload + work item DTOs
│   ├── Jira/                          # JIRA issue DTOs
│   └── Review/                        # Review result + Claude response DTOs
├── Services/
│   ├── IWorkItemService.cs            # Common interface for task systems
│   ├── JiraWorkItemService.cs         # JIRA Cloud implementation
│   ├── AzureDevOpsWorkItemService.cs  # Azure DevOps Work Items implementation
│   ├── GitService.cs                  # git clone/fetch/checkout/diff
│   ├── ClaudeReviewService.cs         # claude -p subprocess
│   ├── ReviewStorageService.cs        # Save .md review files
│   └── ReviewOrchestrator.cs          # Full pipeline orchestration
├── .env.example                       # Config template (committed)
├── .env                               # Actual config (gitignored)
└── reviews/                           # Review output (gitignored)
```
