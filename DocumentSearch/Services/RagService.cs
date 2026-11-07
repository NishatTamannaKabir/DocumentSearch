using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DocumentSearch.Services
{
    /// <summary>
    /// Represents the complete answer produced by the RAG pipeline.
    /// </summary>
    public class RagAnswer
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public List<ScoredChunk> RetrievedChunks { get; set; } = new List<ScoredChunk>();
        public List<string> Citations { get; set; } = new List<string>();
    }

    /// <summary>
    /// RagService implements Retrieval-Augmented Generation (RAG).
    /// 
    /// The 3 steps of RAG:
    ///   Step 1 (Retrieve): Search the VectorStore to find the most relevant chunks of the PDF.
    ///   Step 2 (Augment): Combine those chunks into a prompt that gives the LLM the exact facts.
    ///   Step 3 (Generate): Ask the LLM to write a concise, grounded answer citing chapter & page.
    /// </summary>
    public class RagService
    {
        private readonly VectorStore _vectorStore;
        private readonly AiModelService _aiService;
        private readonly int _topK;

        public RagService(VectorStore vectorStore, AiModelService aiService, IConfiguration config)
        {
            _vectorStore = vectorStore;
            _aiService = aiService;

            // Load topK from configuration (default is 3)
            int topK = 3;
            if (int.TryParse(config["RagSettings:TopK"], out int parsedTopK))
            {
                topK = parsedTopK;
            }
            _topK = topK;
        }

        /// <summary>
        /// Answers a question about "The Little Lantern" PDF using RAG.
        /// </summary>
        public async Task<RagAnswer> AskAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new RagAnswer
                {
                    Question = question,
                    Answer = "Please provide a valid question."
                };
            }

            // ==========================================
            // Step 1: RETRIEVAL
            // ==========================================
            // Find the top most relevant chunks from the story
            List<ScoredChunk> hits = await _vectorStore.SearchAsync(question, _aiService, _topK);

            if (hits.Count == 0)
            {
                return new RagAnswer
                {
                    Question = question,
                    Answer = "No matching passages were found in the document store."
                };
            }

            // ==========================================
            // Step 2: AUGMENTATION
            // ==========================================
            // Format the retrieved chunks into clean context for the AI prompt
            StringBuilder contextBuilder = new StringBuilder();
            List<string> citations = new List<string>();

            for (int i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                string citation = $"{hit.Chunk.Chapter} (Page {hit.Chunk.PageNumber})";
                if (!citations.Contains(citation))
                {
                    citations.Add(citation);
                }

                contextBuilder.AppendLine($"[Passage {i + 1}] Source: {citation}");
                contextBuilder.AppendLine(hit.Chunk.Text);
                contextBuilder.AppendLine("---");
            }

            // System instructions: enforce strict factual grounding and no hallucination
            string systemPrompt =
                "You are an accurate, helpful reading assistant for the story 'The Little Lantern'. " +
                "Answer the user's question using ONLY the provided story passages below. " +
                "Rules:\n" +
                "1. If the answer is not mentioned in the passages, reply: 'I could not find that information in the story.'\n" +
                "2. Quote names, events, and dialogue accurately from the passages.\n" +
                "3. End your response with a citation of the chapter and page number, e.g.: (Source: Chapter One — The Storm, Page 1).\n" +
                "4. Keep your answer clear, direct, and concise.";

            // User prompt: combines the user's question with the retrieved passages
            string userPrompt =
                $"Story Passages:\n{contextBuilder}\n\n" +
                $"User Question: {question}\n\n" +
                "Answer:";

            // ==========================================
            // Step 3: GENERATION
            // ==========================================
            // Ask the chat model (llama3.2 or Azure OpenAI) for the final answer
            string generatedAnswer = await _aiService.GenerateAnswerAsync(systemPrompt, userPrompt);

            return new RagAnswer
            {
                Question = question,
                Answer = generatedAnswer,
                RetrievedChunks = hits,
                Citations = citations
            };
        }
    }
}
