using System;

namespace DocumentSearch.Models
{
    /// <summary>
    /// DocumentChunk represents a single piece of text extracted from the PDF document.
    /// In RAG (Retrieval-Augmented Generation), we break large documents into smaller
    /// chunks so that we can find the most relevant paragraphs when answering questions.
    /// </summary>
    public class DocumentChunk
    {
        /// <summary>
        /// A unique identifier for this chunk (for example: "page-1-chunk-1").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the file this chunk came from (e.g., "The_Little_Lantern.pdf").
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The chapter name where this text appears (e.g., "Chapter One — The Storm").
        /// </summary>
        public string Chapter { get; set; }

        /// <summary>
        /// The PDF page number where this chunk was found (1 or 2).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// The actual text content of this chunk.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The embedding vector: a list of numbers (floats) that captures the mathematical
        /// meaning of the text. For Ollama's nomic-embed-text, this has 768 numbers.
        /// Similar sentences have numbers that are close to each other in direction.
        /// </summary>
        public float[] Vector { get; set; }
    }
}
