using CodeReviewAgent.Configuration;
using CodeReviewAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// Load .env file into configuration (overrides appsettings.json)
var envFile = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFile))
{
    var envVars = File.ReadAllLines(envFile)
        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#'))
        .Select(line => line.Split('=', 2))
        .Where(parts => parts.Length == 2)
        .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

    builder.Configuration.AddInMemoryCollection(envVars!);
}

// Bind configuration
builder.Services.Configure<CodeReviewOptions>(
    builder.Configuration.GetSection(CodeReviewOptions.SectionName));
builder.Services.Configure<JiraOptions>(
    builder.Configuration.GetSection(JiraOptions.SectionName));
builder.Services.Configure<AzureDevOpsOptions>(
    builder.Configuration.GetSection(AzureDevOpsOptions.SectionName));
builder.Services.Configure<ClaudeOptions>(
    builder.Configuration.GetSection(ClaudeOptions.SectionName));
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<GitHubOptions>(
    builder.Configuration.GetSection(GitHubOptions.SectionName));

var codeReviewSection = builder.Configuration.GetSection(CodeReviewOptions.SectionName);

// Register work item service based on configured task system
var taskSystem = codeReviewSection.GetValue<TaskSystemType>(nameof(CodeReviewOptions.TaskSystem));
if (taskSystem == TaskSystemType.AzureDevOps)
    builder.Services.AddSingleton<IWorkItemService, AzureDevOpsWorkItemService>();
else
    builder.Services.AddSingleton<IWorkItemService, JiraWorkItemService>();

// Register review engine based on configuration (default: Claude)
var reviewEngine = codeReviewSection.GetValue<ReviewEngineType>(nameof(CodeReviewOptions.ReviewEngine));
if (reviewEngine == ReviewEngineType.Ollama)
    builder.Services.AddSingleton<IReviewEngineService, OllamaReviewEngine>();
else
    builder.Services.AddSingleton<IReviewEngineService, ClaudeReviewEngine>();

// Register remaining services
builder.Services.AddSingleton<IGitService, GitService>();
builder.Services.AddSingleton<IReviewStorageService, ReviewStorageService>();
builder.Services.AddSingleton<IReviewOrchestrator, ReviewOrchestrator>();

builder.Services.AddControllers().AddNewtonsoftJson();

var app = builder.Build();

app.Logger.LogInformation("Task system: {TaskSystem}, Review engine: {ReviewEngine}", taskSystem, reviewEngine);

app.UseRouting();
app.MapControllers();

app.Run();
