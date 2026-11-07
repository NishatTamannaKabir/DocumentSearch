using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocumentSearch.Models;
using DocumentSearch.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocumentSearch
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("===============================================================");
            Console.WriteLine("  The Little Lantern — Document Search & RAG (ASP.NET Core)    ");
            Console.WriteLine("===============================================================");

            // Step 1: Create the web application builder
            var builder = WebApplication.CreateBuilder(args);

            // Register standard HTTP client and our custom RAG services
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<AiModelService>();
            builder.Services.AddSingleton<VectorStore>();
            builder.Services.AddSingleton<RagService>();

            var app = builder.Build();

            // Step 2: Enable serving static files (index.html from wwwroot)
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Step 3: Ingest the PDF at application startup
            var config = app.Services.GetRequiredService<IConfiguration>();
            var aiService = app.Services.GetRequiredService<AiModelService>();
            var vectorStore = app.Services.GetRequiredService<VectorStore>();

            string relativePdfPath = config["RagSettings:PdfFilePath"] ?? "Data/The_Little_Lantern.pdf";
            string fullPdfPath = Path.Combine(app.Environment.ContentRootPath, relativePdfPath);

            // If not found in ContentRootPath, check Data directory
            if (!File.Exists(fullPdfPath))
            {
                fullPdfPath = Path.Combine(AppContext.BaseDirectory, relativePdfPath);
            }

            Console.WriteLine($"[Startup] Provider : {aiService.ProviderName}");
            Console.WriteLine($"[Startup] PDF Path : {fullPdfPath}");

            // Load and chunk the PDF
            int chunkSize = int.TryParse(config["RagSettings:ChunkSize"], out int cs) ? cs : 150;
            int overlap = int.TryParse(config["RagSettings:ChunkOverlap"], out int ov) ? ov : 30;

            List<DocumentChunk> chunks = PdfLoader.LoadPdf(fullPdfPath, chunkSize, overlap);
            Console.WriteLine($"[Startup] Extracted {chunks.Count} chunks from PDF.");

            // Index chunks into the in-memory vector store (computes 768-dim embeddings)
            await vectorStore.IndexChunksAsync(chunks, aiService);

            // ==========================================
            // REST API Endpoints
            // ==========================================

            // Endpoint 1: Document & System Information
            app.MapGet("/api/info", () =>
            {
                return Results.Ok(new
                {
                    documentName = Path.GetFileName(fullPdfPath),
                    totalPages = 2,
                    totalChunks = vectorStore.TotalChunks,
                    provider = aiService.ProviderName
                });
            });

            // Endpoint 2: Semantic Vector Search Only (returns raw matching passages)
            app.MapPost("/api/search", async (SearchRequest req, VectorStore store, AiModelService ai) =>
            {
                if (string.IsNullOrWhiteSpace(req?.Query))
                {
                    return Results.BadRequest(new { error = "Query is required" });
                }

                var hits = await store.SearchAsync(req.Query, ai, topK: 3);
                return Results.Ok(hits);
            });

            // Endpoint 3: Full RAG Question Answering (Retrieval + Augmentation + Generation)
            app.MapPost("/api/ask", async (AskRequest req, RagService rag) =>
            {
                if (string.IsNullOrWhiteSpace(req?.Question))
                {
                    return Results.BadRequest(new { error = "Question is required" });
                }

                var response = await rag.AskAsync(req.Question);
                return Results.Ok(response);
            });

            Console.WriteLine("\n[Ready] Application initialized successfully!");
            Console.WriteLine("        Open http://localhost:5000 in your browser to interact with the assistant.\n");

            await app.RunAsync();
        }
    }

    // Helper request models for the minimal API endpoints
    public class SearchRequest
    {
        public string Query { get; set; }
    }

    public class AskRequest
    {
        public string Question { get; set; }
    }
}
