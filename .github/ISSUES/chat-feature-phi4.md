# GitHub Issue: Add Chat Page with Phi4 Model Integration

**Title:** Add Chat Page with Phi4 Model Integration

**Labels:** enhancement, ai-integration

---

## Feature Description

Add a simple chat functionality as a separate page in the ZavaStorefront application that integrates with the Microsoft Foundry Phi4 endpoint.

## Requirements

### 1. Chat Page UI
- [ ] Create a new "Chat" page accessible from the main navigation
- [ ] Add a text input area for user messages
- [ ] Add a text area to display conversation history (user messages and AI responses)
- [ ] Add a "Send" button to submit messages
- [ ] Show loading indicator while waiting for AI response

### 2. Backend Integration
- [ ] Create a new `ChatController` to handle chat requests
- [ ] Create a `ChatService` to communicate with the Microsoft Foundry Phi4 endpoint
- [ ] Use the deployed AI Services endpoint (`ais-zavastore-dev-*`) with Phi4 model
- [ ] Configure managed identity authentication (no API keys in code)

### 3. Infrastructure Updates
- [ ] Add Phi4 model deployment to `infra/main.bicep`
- [ ] Add AI Services endpoint URL to App Service configuration
- [ ] Ensure Web App has appropriate RBAC permissions to access AI Services

### 4. Configuration
- [ ] Add `Azure:AIServices:Endpoint` configuration setting
- [ ] Add `Azure:AIServices:ModelDeploymentName` for Phi4

## Technical Notes

- The AI Services resource is already deployed: `ais-zavastore-dev-{uniqueSuffix}`
- Currently deploys GPT-4o; need to add Phi4 deployment
- Use Azure.AI.OpenAI SDK for .NET
- Use DefaultAzureCredential for managed identity auth

## Acceptance Criteria

1. User can navigate to a "/Chat" page from the main menu
2. User can type a message and send it to the Phi4 model
3. AI response appears in the conversation text area
4. No secrets or API keys are stored in code or config files
5. Chat works when deployed to Azure App Service with managed identity

---

## Implementation Guide

### Step 1: Update Infrastructure (infra/main.bicep)

Add Phi4 model deployment to the AI Services:

```bicep
deployments: [
  {
    name: 'gpt-4o'
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
    sku: {
      name: 'GlobalStandard'
      capacity: 10
    }
  }
  {
    name: 'phi4'
    model: {
      format: 'OpenAI'
      name: 'Phi-4'
      version: '1'
    }
    sku: {
      name: 'GlobalStandard'
      capacity: 10
    }
  }
]
```

Add App Settings for AI Services:
```bicep
{
  name: 'Azure__AIServices__Endpoint'
  value: aiServices.outputs.endpoint
}
{
  name: 'Azure__AIServices__ModelDeploymentName'
  value: 'phi4'
}
```

### Step 2: Add NuGet Package

Add to `ZavaStorefront.csproj`:
```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.0.0" />
<PackageReference Include="Azure.Identity" Version="1.12.0" />
```

### Step 3: Create ChatService

Create `Services/ChatService.cs`:
```csharp
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;

public interface IChatService
{
    Task<string> SendMessageAsync(string message);
}

public class ChatService : IChatService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;

    public ChatService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:AIServices:Endpoint"];
        _deploymentName = configuration["Azure:AIServices:ModelDeploymentName"] ?? "phi4";
        
        _client = new AzureOpenAIClient(
            new Uri(endpoint),
            new DefaultAzureCredential());
    }

    public async Task<string> SendMessageAsync(string message)
    {
        var chatClient = _client.GetChatClient(_deploymentName);
        var response = await chatClient.CompleteChatAsync(message);
        return response.Value.Content[0].Text;
    }
}
```

### Step 4: Create ChatController

Create `Controllers/ChatController.cs`:
```csharp
public class ChatController : Controller
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        var response = await _chatService.SendMessageAsync(request.Message);
        return Json(new { response });
    }
}

public class ChatRequest
{
    public string Message { get; set; }
}
```

### Step 5: Create Chat View

Create `Views/Chat/Index.cshtml` with a simple chat interface.

### Step 6: Register Services

In `Program.cs`:
```csharp
builder.Services.AddSingleton<IChatService, ChatService>();
```

### Step 7: Update Navigation

Add Chat link to `Views/Shared/_Layout.cshtml`.
