# 🔎 DocumentSearch — AI-Powered Semantic Document Search

> An AI-powered document search and question-answering application built with **ASP.NET Core .NET 8**, **Ollama**, **RAG**, **text embeddings**, and **cosine similarity**.

DocumentSearch allows users to interact with PDF documents using natural language. Instead of relying only on traditional keyword matching, the application converts document content and user queries into vector embeddings and identifies the most semantically relevant information.

---

## ✨ Overview

**DocumentSearch** is a practical implementation of a Retrieval-Augmented Generation (**RAG**) workflow.

The application takes a PDF document, extracts its text, divides the content into smaller chunks, generates vector embeddings for those chunks, and stores them for semantic retrieval.

When a user asks a question, the application:

1. Converts the question into an embedding.
2. Compares the question embedding with document chunk embeddings.
3. Uses **cosine similarity** to identify relevant content.
4. Retrieves the most relevant document chunks.
5. Sends the retrieved context to the local LLM.
6. Generates a context-aware response.

This approach helps the AI answer questions based on the content of the uploaded document rather than relying solely on its pre-trained knowledge.

---

## 🚀 Key Features

- 📄 **PDF Document Processing**
  - Extract text from PDF documents.

- ✂️ **Document Chunking**
  - Break large documents into smaller searchable chunks.

- 🧠 **Text Embeddings**
  - Generate numerical vector representations of document content.

- 🔍 **Semantic Search**
  - Search documents based on meaning rather than exact keywords.

- 📐 **Cosine Similarity**
  - Calculate similarity between user queries and document chunks.

- 🤖 **Local AI**
  - Uses Ollama for running AI models locally.

- 💬 **RAG-Based Question Answering**
  - Retrieves relevant document content before generating an answer.

- 🔒 **Local Processing**
  - Designed to work with locally running AI models without requiring a cloud AI API.

- 🌐 **ASP.NET Core Web Application**
  - Provides a web-based interface for interacting with the system.

---

## 🏗️ Architecture

```text
                    ┌──────────────────────┐
                    │      PDF Document    │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │     PDF Extraction   │
                    │       (PdfPig)       │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │   Text Chunking      │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │   Embedding Model    │
                    │  nomic-embed-text    │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │    Vector Storage    │
                    └──────────────────────┘


User Question
      │
      ▼
┌──────────────────────┐
│ Generate Query       │
│ Embedding            │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ Cosine Similarity    │
│ Comparison           │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ Relevant Chunks      │
│ Retrieved            │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ Ollama / Llama 3.2   │
│ Context + Question   │
└──────────┬───────────┘
           │
           ▼
      🤖 AI Answer
```

---

## 🧰 Tech Stack

| Technology | Purpose |
|---|---|
| **C#** | Application development |
| **ASP.NET Core** | Web application framework |
| **.NET 8** | Runtime and development platform |
| **Ollama** | Local AI model execution |
| **Llama 3.2** | Question-answering / generation |
| **nomic-embed-text** | Text embeddings |
| **PdfPig** | PDF text extraction |
| **HTML / CSS / JavaScript** | Front-end interface |
| **Git / GitHub** | Version control |

---

## 📂 Project Structure

```text
DocumentSearch/
│
├── Data/
│   └── The_Little_Lantern.pdf
│
├── Models/
│   └── DocumentChunk.cs
│
├── Services/
│   ├── AiModelService.cs
│   ├── PdfLoader.cs
│   ├── RagService.cs
│   └── VectorStore.cs
│
├── Properties/
│   └── launchSettings.json
│
├── wwwroot/
│   └── index.html
│
├── Program.cs
├── appsettings.json
├── DocumentSearch.csproj
└── DocumentSearch.slnx
```

---

## 🔄 RAG Workflow

The application follows a simplified Retrieval-Augmented Generation pipeline:

### 1. Document Loading

A PDF document is loaded into the application.

### 2. Text Extraction

Text is extracted from the PDF using **PdfPig**.

### 3. Chunking

The extracted text is divided into smaller chunks.

Chunking makes it easier to search specific sections of a large document.

### 4. Embedding Generation

Each chunk is converted into a numerical vector using:

```text
nomic-embed-text
```

These vectors represent the semantic meaning of the text.

### 5. Query Processing

When the user asks a question, the question is also converted into an embedding.

### 6. Similarity Search

The query embedding is compared with the document embeddings using **cosine similarity**.

The chunks with the highest similarity scores are considered the most relevant.

### 7. Context Retrieval

The most relevant chunks are retrieved and supplied as context.

### 8. Answer Generation

The retrieved context and the user's question are sent to **Llama 3.2** through Ollama.

The model then generates the final response.

---

## 🧠 Why RAG?

Traditional LLM applications can struggle when answering questions about private or newly provided documents.

RAG solves this by separating the process into two major stages:

```text
RETRIEVE → GENERATE
```

**Retrieve**

Find relevant information from the document.

**Generate**

Use the retrieved information as context for the AI response.

This allows the application to provide answers grounded in the document content.

---

## ⚙️ Prerequisites

Before running the project, install:

- .NET 8 SDK
- Visual Studio
- Ollama
- Git

Verify .NET:

```bash
dotnet --version
```

Verify Ollama:

```bash
ollama --version
```

---

## 🤖 Ollama Setup

Pull the required models:

```bash
ollama pull llama3.2
```

```bash
ollama pull nomic-embed-text
```

Make sure Ollama is running locally.

The application uses the local Ollama endpoint:

```text
http://localhost:11434
```

---

## ▶️ Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/NishatTamannaKabir/DocumentSearch.git
```

### 2. Navigate to the project

```bash
cd DocumentSearch
```

### 3. Restore dependencies

```bash
dotnet restore
```

### 4. Build the project

```bash
dotnet build
```

### 5. Run the application

```bash
dotnet run
```

Alternatively, open the solution in **Visual Studio** and run the project.

---

## 🔧 Configuration

The application uses local Ollama models.

Example configuration:

```json
{
  "Endpoint": "http://localhost:11434",
  "ChatModel": "llama3.2",
  "EmbeddingModel": "nomic-embed-text"
}
```

> **Note:** Do not commit API keys, passwords, tokens, connection strings, or other sensitive credentials to GitHub.

---

## 📊 Core Components

### `PdfLoader.cs`

Responsible for extracting text from PDF documents.

### `DocumentChunk.cs`

Represents individual pieces of document content used during retrieval.

### `VectorStore.cs`

Handles the storage and retrieval of document embeddings.

### `RagService.cs`

Coordinates the retrieval process and RAG workflow.

### `AiModelService.cs`

Communicates with the local Ollama models for embeddings and AI-generated responses.

### `Program.cs`

Configures and starts the ASP.NET Core application.

---

## 🎯 Learning Objectives

This project demonstrates practical understanding of:

- ASP.NET Core
- C#
- REST/API concepts
- PDF processing
- Text preprocessing
- Chunking strategies
- Vector embeddings
- Semantic search
- Cosine similarity
- Retrieval-Augmented Generation
- Local LLM integration
- Ollama
- AI application architecture

---

## 🔮 Future Improvements

Possible future enhancements include:

- [ ] Support multiple PDF documents
- [ ] Persistent vector database
- [ ] Improved chunking strategies
- [ ] Metadata-based filtering
- [ ] Source citations in responses
- [ ] Conversation history
- [ ] Streaming AI responses
- [ ] Document upload through the web interface
- [ ] Authentication and authorization
- [ ] Improved UI/UX
- [ ] Docker support
- [ ] Deployment to a cloud platform

---

## 🛡️ Security Considerations

This project is designed primarily for learning and demonstration purposes.

For production use, additional security measures should be implemented, including:

- Authentication
- Authorization
- Input validation
- File type and file size validation
- Secure configuration management
- Protection of sensitive credentials
- Logging and monitoring

---

## 📸 Screenshots

Add screenshots of the application here.

Example:

```text
docs/
├── home-page.png
├── document-search.png
└── rag-response.png
```

Then reference them:

```markdown
![DocumentSearch](docs/home-page.png)
```

---

## 📌 Project Status

**Status:** 🚧 Active Development

The current version demonstrates the core document ingestion, embedding, retrieval, and AI question-answering workflow.

---

## 👩‍💻 Author

**Nishat Tamanna Kabir**

B.Tech — Computer Science & Engineering

GitHub:  
https://github.com/NishatTamannaKabir

---

## ⭐ Support

If you find this project useful or interesting, consider giving the repository a ⭐.

---

## 📄 License

This project is intended for educational and portfolio purposes.
