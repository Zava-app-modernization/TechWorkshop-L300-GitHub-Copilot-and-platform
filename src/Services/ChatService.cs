using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using ZavaStorefront.Models;

namespace ZavaStorefront.Services
{
    public class ChatService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;
        private readonly AzureOpenAIClient? _client;

        public ChatService(IConfiguration configuration, ILogger<ChatService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var endpoint = _configuration["AzureAI:Endpoint"];
            
            if (!string.IsNullOrEmpty(endpoint))
            {
                // Use managed identity for authentication (no API keys)
                _client = new AzureOpenAIClient(
                    new Uri(endpoint),
                    new DefaultAzureCredential());
            }
            else
            {
                _logger.LogWarning("AzureAI:Endpoint is not configured. Chat functionality will be limited.");
            }
        }

        public async Task<Models.ChatResponse> GetChatResponseAsync(string userMessage)
        {
            try
            {
                if (_client == null)
                {
                    return new Models.ChatResponse
                    {
                        Success = false,
                        Error = "Chat service is not configured. Please set the AzureAI:Endpoint configuration."
                    };
                }

                _logger.LogInformation("Processing chat request");

                var deploymentName = _configuration["AzureAI:DeploymentName"] ?? "gpt-4o";

                var chatClient = _client.GetChatClient(deploymentName);

                var messages = new List<OpenAI.Chat.ChatMessage>
                {
                    new SystemChatMessage("You are a helpful assistant for Zava Storefront. Help customers with product questions and shopping assistance."),
                    new UserChatMessage(userMessage)
                };

                var response = await chatClient.CompleteChatAsync(messages);

                var completion = response.Value.Content[0].Text;

                _logger.LogInformation("Chat response generated successfully");

                return new Models.ChatResponse
                {
                    Response = completion,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                return new Models.ChatResponse
                {
                    Success = false,
                    Error = "An error occurred while processing your request. Please try again."
                };
            }
        }
    }
}
