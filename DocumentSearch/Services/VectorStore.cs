using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentSearch.Models;

namespace DocumentSearch.Services
{
    /// <summary>
    /// Represents a search result containing the matching chunk and its similarity score.
    /// </summary>
    public class ScoredChunk
    {
        public DocumentChunk Chunk { get; set; }
        public double Score { get; set; }
    }

    /// <summary>
    /// VectorStore is an in-memory vector database.
    /// It stores our DocumentChunks along with their mathematical embedding vectors.
    /// 
    /// How it works:
    ///   1. Ingestion: For each chunk of text, we generate an embedding (a list of 768 numbers).
    ///   2. Search: When the user asks a question, we turn their question into an embedding too.
    ///   3. Cosine Similarity: We compare the question's numbers against every chunk's numbers.
    ///      The chunks with the highest similarity score are the most relevant to the question!
    /// </summary>
    public class VectorStore
    {
        private readonly List<DocumentChunk> _chunks = new List<DocumentChunk>();

        public int TotalChunks => _chunks.Count;

        public IReadOnlyList<DocumentChunk> AllChunks => _chunks.AsReadOnly();

        /// <summary>
        /// Clears existing data and populates the store with new chunks, computing embeddings for each one.
        /// </summary>
        public async Task IndexChunksAsync(List<DocumentChunk> chunks, AiModelService aiService)
        {
            _chunks.Clear();

            Console.WriteLine($"[VectorStore] Starting embedding generation for {chunks.Count} chunks...");

            for (int i = 0; i < chunks.Count; i++)
            {
                DocumentChunk chunk = chunks[i];
                Console.WriteLine($"  -> Embedding chunk {i + 1}/{chunks.Count} ({chunk.Chapter}, Page {chunk.PageNumber})...");

                // Generate the embedding vector using our AI model service
                chunk.Vector = await aiService.GenerateEmbeddingAsync(chunk.Text);
                _chunks.Add(chunk);
            }

            Console.WriteLine("[VectorStore] All chunks have been embedded and indexed in memory!");
        }

        /// <summary>
        /// Searches the in-memory vector store for the chunks most semantically similar to the question.
        /// </summary>
        /// <param name="question">The user's query or question.</param>
        /// <param name="aiService">The AI service to embed the question.</param>
        /// <param name="topK">The number of best matches to return (default: 3).</param>
        /// <returns>A list of ScoredChunk sorted from highest similarity to lowest.</returns>
        public async Task<List<ScoredChunk>> SearchAsync(string question, AiModelService aiService, int topK = 3)
        {
            List<ScoredChunk> results = new List<ScoredChunk>();

            if (_chunks.Count == 0 || string.IsNullOrWhiteSpace(question))
            {
                return results;
            }

            // Step 1: Turn the user's question into an embedding vector.
            float[] questionVector = await aiService.GenerateEmbeddingAsync(question);

            // Step 2: Compare the question vector against every chunk vector using Cosine Similarity.
            List<ScoredChunk> scoredList = new List<ScoredChunk>();
            foreach (DocumentChunk chunk in _chunks)
            {
                if (chunk.Vector == null || chunk.Vector.Length == 0)
                {
                    continue;
                }

                double score = CosineSimilarity(questionVector, chunk.Vector);
                scoredList.Add(new ScoredChunk
                {
                    Chunk = chunk,
                    Score = score
                });
            }

            // Step 3: Sort the chunks from highest similarity score to lowest.
            scoredList.Sort((a, b) => b.Score.CompareTo(a.Score));

            // Step 4: Take only the top K results.
            int countToTake = Math.Min(topK, scoredList.Count);
            for (int i = 0; i < countToTake; i++)
            {
                results.Add(scoredList[i]);
            }

            return results;
        }

        /// <summary>
        /// Cosine Similarity Formula:
        ///   CosineSimilarity = (A . B) / (|A| * |B|)
        /// 
        /// Explanation for beginners:
        ///   - A . B (Dot Product): Multiply each number in vector A with the corresponding number in vector B, then add them all together.
        ///   - |A| (Magnitude / Length): Square each number, sum them up, and take the square root.
        ///   - Score of 1.0 means the vectors point in the exact same direction (same meaning).
        ///   - Score of 0.0 means the vectors are perpendicular (unrelated).
        ///   - Score of -1.0 means opposite meaning.
        /// </summary>
        private static double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
            {
                throw new ArgumentException("Vectors must have the same dimension length for similarity comparison.");
            }

            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            if (magnitudeA <= 0.0 || magnitudeB <= 0.0)
            {
                return 0.0;
            }

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }
    }
}
