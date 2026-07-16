# HelpDesk AI — Ticket Classifier

Automatically classifies support tickets by **category**, **priority**, **department**, and **sentiment** using AI. Upload a CSV, get instant insights through an interactive dashboard, and export enriched results.

## Features

- **AI-powered classification** — category, priority, department, sentiment, summary, justification, and tags per ticket
- **Multiple LLM providers** — Google Gemini, Anthropic Claude, local Llama, or keyword-based mock (no API key required)
- **Batch processing** — parallel batches with throttling and real-time progress tracking
- **Interactive dashboard** — KPI cards, bar/doughnut charts, filterable data table with pagination
- **Duplicate detection** — warns before uploading a file that was already processed
- **Similar ticket detection** — automatically finds and links related tickets by shared tags
- **Reprocessing** — reprocess failed tickets or the entire batch with one click
- **CSV export** — download enriched results with customizable column selection
- **Classification rules** — manage keyword-based parameters that guide AI classification (categories, priorities, departments, questions, complaints, sentiments, tags)
- **Ticket editing** — manually edit any ticket's classification and track modifications
- **Dynamic AI prompts** — prompt builder uses database-driven rules and keyword indicators for context-aware classification
- **Login attempt limiting** — security feature to prevent brute-force attacks

## Architecture

```
Controller → Service → Repository → DbContext
                 └→ Gateways (AI strategies) via Factory
```

- **Controllers** — thin orchestration layer
- **Services** (`ITicketService`) — business logic: CSV parsing, classification, statistics
- **Repositories** (`ITicketRepository`) — data access via EF Core
- **Gateways** + **Factory** + **Strategy** — `IClassificationGateway` with `GeminiGateway`, `AnthropicGateway`, `LlamaGateway`; the `ClassificationGatewayFactory` selects the strategy based on `Llm:Provider` config
- **Mappers** (`TicketMapper`) — entity ↔ DTO conversion
- **DTOs** — separate input (`Dtos/Input`) and output (`Dtos/Output`) models

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 8 Web API, EF Core |
| Frontend | Vue 3, Vite, Tailwind CSS, Chart.js |
| Database | PostgreSQL |
| Orchestration | Docker Compose |
| AI | Gemini, Claude, Llama, or mock |

## Quick Start (Docker)

```bash
git clone https://github.com/eduardojacomo/TicketClassifier.git
cd TicketClassifier
cp .env.example .env       # optional: configure AI provider
docker compose up --build
```

- **Frontend:** http://localhost:5174
- **API (Swagger):** http://localhost:8080/swagger
- **PostgreSQL:** localhost:5433

Upload a CSV (use the included `sample-tickets-small.csv`), view the dashboard, and export enriched results.

## AI Providers

By default runs in `mock` mode (keyword heuristics) — works without any API key.
To use an LLM, set in `.env`:

```env
# Google Gemini
LLM_PROVIDER=gemini
GEMINI_API_KEY=your-key
GEMINI_MODEL=gemini-2.5-flash

# Anthropic Claude
LLM_PROVIDER=anthropic
ANTHROPIC_API_KEY=sk-ant-...
ANTHROPIC_MODEL=claude-haiku-4-5-20251001

# Local Llama
LLM_PROVIDER=llama
# Llama server runs as a Docker service on port 8081
```

If the chosen provider's key is not set, the factory falls back to `mock`.

## CSV Input Format

Recognized columns (case-insensitive, delimiter auto-detected):
- `subject` / `assunto` / `title`
- `description` / `descricao` / `body` / `message` / `text`
- `id` (optional)

If no description column is found, all column text is concatenated.

## Classification Output

Each ticket receives:

| Field | Values |
|-------|--------|
| Category | Bug, Question, Complaint, Login, Payment, Financial, Performance, Integration, Registration, Sales, Suggestion, Praise, Other |
| Priority | Low, Medium, High, Critical |
| Department | Support, Development, Financial, Sales, Product |
| Sentiment | positive, negative, neutral |
| Confidence | 0.0 – 1.0 |
| Summary | AI-generated one-line summary |
| Justification | AI reasoning for the classification |
| Tags | Relevant keywords extracted from the ticket |

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/tickets/upload` | Upload CSV, classify, and persist the batch |
| GET | `/api/tickets/batches` | List all batches |
| GET | `/api/tickets/batches/{id}` | Batch detail with tickets and statistics |
| POST | `/api/tickets/batches/{id}/reprocess` | Reprocess failed tickets |
| POST | `/api/tickets/batches/{id}/reprocess-all` | Reprocess all tickets in batch |
| GET | `/api/tickets/batches/{id}/export` | Download enriched CSV |
| GET | `/api/tickets/check-duplicate` | Check if a file was already uploaded |
| PATCH | `/api/tickets/{ticketId}` | Edit a ticket's classification |
| GET | `/api/tickets/{ticketId}/similar` | Find similar tickets |
| GET | `/api/tickets/progress/{jobId}` | Real-time processing progress |
| GET | `/api/parameters` | List classification parameters |
| POST | `/api/parameters` | Create a new parameter |
| PUT | `/api/parameters/{id}` | Update a parameter |
| DELETE | `/api/parameters/{id}` | Delete a parameter |
| GET | `/api/parameters/types` | List available parameter types |

## Development (without Docker)

**Backend:**
```bash
cd backend/TicketClassifier.Api
# PostgreSQL on localhost:5433 (or adjust connection string in appsettings.json)
dotnet run
```

**Frontend:**
```bash
cd frontend
npm install
npm run dev
```

**Tests:**
```bash
cd backend
dotnet test    # 54 tests
```
