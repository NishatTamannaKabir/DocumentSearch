using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace DocumentSearch.Services
{
    /// <summary>
    /// AiModelService manages communication with AI models for both:
    ///   1. Embeddings (turning text into mathematical vectors)
    ///   2. Chat Completion (asking the LLM questions and receiving answers)
    /// 
    /// It supports two providers configured in appsettings.json:
    ///   - "Ollama" : runs 100% locally on your computer (free, private, no API key needed)
    ///   - "AzureOpenAI" : runs in Microsoft Azure cloud (requires Azure endpoint & API key)
    /// </summary>
    public class AiModelService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly bool _useOllama;

        // Model names and endpoints
        private readonly string _ollamaEndpoint;
        private readonly string _chatModel;
        private readonly string _embeddingModel;

        // Azure client (only initialized if using Azure)
        private readonly AzureOpenAIClient _azureClient;

        public AiModelService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;

            // Determine if the user configured Ollama or AzureOpenAI
            string provider = _config["ModelProvider"] ?? "Ollama";
            _useOllama = string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase);

            if (_useOllama)
            {
                _ollamaEndpoint = (_config["Ollama:Endpoint"] ?? "http://127.0.0.1:11434").TrimEnd('/');
                _chatModel = _config["Ollama:ChatModel"] ?? "llama3.2";
                _embeddingModel = _config["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
            }
            else
            {
                string endpoint = _config["AzureOpenAI:Endpoint"];
                string apiKey = _config["AzureOpenAI:ApiKey"];
                _chatModel = _config["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";
                _embeddingModel = _config["AzureOpenAI:EmbeddingDeploymentName"] ?? "text-embedding-3-small";

                if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
                {
                    _azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
                }
            }
        }

        public string ProviderName => _useOllama ? $"Ollama ({_chatModel} + {_embeddingModel})" : $"Azure OpenAI ({_chatModel})";

        /// <summary>
        /// Generates a vector embedding for a piece of text.
        /// An embedding is a list of floating-point numbers (e.g. 768 dimensions)
        /// that captures the semantic meaning of the words.
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (_useOllama)
            {
                // Ollama provides a simple POST /api/embeddings endpoint.
                // We send: { "model": "nomic-embed-text", "prompt": text }
                var payload = new
                {
                    model = _embeddingModel,
                    prompt = text
                };

                string json = JsonSerializer.Serialize(payload);
                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync($"{_ollamaEndpoint}/api/embeddings", content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseBody);

                // The response contains an "embedding" property: an array of numbers.
                JsonElement embeddingElement = doc.RootElement.GetProperty("embedding");
                float[] vector = new float[embeddingElement.GetArrayLength()];

                int index = 0;
                foreach (JsonElement number in embeddingElement.EnumerateArray())
                {
                    vector[index++] = (float)number.GetDouble();
                }

                return vector;
            }
            else
            {
                // Azure OpenAI path
                EmbeddingClient embeddingClient = _azureClient.GetEmbeddingClient(_embeddingModel);
                OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(text);
                ReadOnlyMemory<float> memory = embedding.ToFloats();
                return memory.ToArray();
            }
        }

        /// <summary>
        /// Sends the grounded prompt to the LLM and gets back the written answer.
        /// </summary>
        public async Task<string> GenerateAnswerAsync(string systemPrompt, string userPrompt)
        {
            if (_useOllama)
            {
                // Ollama provides a POST /api/chat endpoint.
                // We send system instructions and the user question with retrieved context.
                var payload = new
                {
                    model = _chatModel,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    stream = false,
                    options = new
                    {
                        temperature = 0.2 // Low temperature prevents hallucination and keeps answers factual
                    }
                };

                string json = JsonSerializer.Serialize(payload);
                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync($"{_ollamaEndpoint}/api/chat", content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseBody);

                // Ollama response format: { "message": { "role": "assistant", "content": "..." } }
                string answer = doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return answer?.Trim() ?? string.Empty;
            }
            else
            {
                // Azure OpenAI path
                ChatClient chatClient = _azureClient.GetChatClient(_chatModel);
                List<ChatMessage> messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                ChatCompletion completion = await chatClient.CompleteChatAsync(messages);
                return completion.Content[0].Text?.Trim() ?? string.Empty;
            }
        }
    }
}
