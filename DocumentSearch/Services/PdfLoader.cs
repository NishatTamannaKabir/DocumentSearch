using System;
using System.Collections.Generic;
using System.IO;
using DocumentSearch.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DocumentSearch.Services
{
    /// <summary>
    /// PdfLoader is responsible for opening the PDF story file, reading each page,
    /// and dividing the story text into small, readable chunks for our vector store.
    /// </summary>
    public static class PdfLoader
    {
        // We set the target chunk size to 150 words per chunk.
        private const int DefaultWordsPerChunk = 150;

        // We overlap 30 words between consecutive chunks so no sentence is cut in half.
        private const int DefaultOverlapWords = 30;

        /// <summary>
        /// Reads a PDF file from disk, extracts text page by page, and returns a list of DocumentChunks.
        /// </summary>
        /// <param name="filePath">The full or relative path to the PDF file.</param>
        /// <param name="wordsPerChunk">Target number of words per chunk.</param>
        /// <param name="overlapWords">Number of words to repeat in the next chunk.</param>
        /// <returns>A list of DocumentChunk objects ready to be embedded.</returns>
        public static List<DocumentChunk> LoadPdf(string filePath, int wordsPerChunk = DefaultWordsPerChunk, int overlapWords = DefaultOverlapWords)
        {
            List<DocumentChunk> chunks = new List<DocumentChunk>();

            // Step 1: Check if the file actually exists on disk.
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("PDF file not found at path: " + filePath);
            }

            string fileName = Path.GetFileName(filePath);

            // Step 2: Open the PDF document using UglyToad.PdfPig.
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                // Loop through every page in the PDF (page 1, page 2, etc.)
                foreach (Page page in document.GetPages())
                {
                    // ContentOrderTextExtractor reads the words in natural left-to-right, top-to-bottom reading order.
                    string pageText = ContentOrderTextExtractor.GetText(page);

                    if (string.IsNullOrWhiteSpace(pageText))
                    {
                        continue;
                    }

                    // Identify which chapter is on this page for clear source attribution.
                    string chapter = DetectChapter(pageText, page.Number);

                    // Step 3: Split the page text into word-based chunks with overlap.
                    List<string> textPieces = SplitIntoWordChunks(pageText, wordsPerChunk, overlapWords);

                    for (int i = 0; i < textPieces.Count; i++)
                    {
                        DocumentChunk chunk = new DocumentChunk
                        {
                            Id = fileName + "-p" + page.Number + "-c" + (i + 1),
                            FileName = fileName,
                            Chapter = chapter,
                            PageNumber = page.Number,
                            Text = textPieces[i]
                        };

                        chunks.Add(chunk);
                    }
                }
            }

            return chunks;
        }

        /// <summary>
        /// Inspects the page text to find the chapter heading, or provides a helpful default.
        /// </summary>
        private static string DetectChapter(string text, int pageNumber)
        {
            if (text.Contains("Chapter One") || text.Contains("The Storm"))
            {
                return "Chapter One — The Storm";
            }
            if (text.Contains("Chapter Two") || text.Contains("A Light in the Dark"))
            {
                return "Chapter Two — A Light in the Dark";
            }
            return "Page " + pageNumber;
        }

        /// <summary>
        /// Splits plain text into chunks based on word count with a sliding window overlap.
        /// Why overlap? If a sentence like "Mira took the oil lamp and ran upstairs" is split
        /// between two chunks, the overlap ensures the full thought is present in at least one chunk.
        /// </summary>
        private static List<string> SplitIntoWordChunks(string text, int wordsPerChunk, int overlapWords)
        {
            List<string> result = new List<string>();

            // Split text by common whitespace characters: space, newlines, carriage returns, tabs.
            char[] delimiters = new char[] { ' ', '\r', '\n', '\t' };
            string[] words = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return result;
            }

            int startIndex = 0;
            while (startIndex < words.Length)
            {
                // Calculate how many words to take in this chunk.
                int count = Math.Min(wordsPerChunk, words.Length - startIndex);
                string chunkText = string.Join(" ", words, startIndex, count);
                result.Add(chunkText);

                // If we reached the end of the text, stop.
                if (startIndex + count >= words.Length)
                {
                    break;
                }

                // Advance by (chunk size - overlap). For example: 150 - 30 = advance 120 words.
                startIndex += (wordsPerChunk - overlapWords);
            }

            return result;
        }
    }
}
