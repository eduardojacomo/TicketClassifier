# AI Ticket Classifier

Classifica **tipo** e **prioridade** de tickets de suporte a partir de um CSV usando um LLM,
gera estatísticas e exporta um CSV enriquecido.

- **Backend:** .NET 8 Web API + EF Core
- **Frontend:** Vue 3 + Vite
- **Banco:** PostgreSQL
- **Orquestração:** Docker Compose
- **IA:** Anthropic (Claude), Google Gemini ou modo `mock` (heurística local, roda sem chave)

## Arquitetura (padrão em camadas)

```
Controller → Service → Repository → DbContext
                 └→ Gateways (estratégias de IA) via Factory
```

- **Controllers** finos: só orquestram.
- **Services** (`ITicketService`): regra de negócio (parse, classificar, estatísticas).
- **Repositories** (`ITicketRepository`): acesso a dados.
- **Gateways** + **Factory** + **Strategy**: `IClassificacaoGateway` com implementações
  `AnthropicGateway`, `GeminiGateway`, `MockGateway`; a `ClassificacaoGatewayFactory`
  seleciona a estratégia por `Llm:Provider`.
- **Mappers** (`TicketMapper`): entidade ↔ DTO.
- **DTOs** de entrada (`Dtos/Input`) e saída (`Dtos/Output`).

## Como rodar (Docker)

```bash
cd c:\dev\TicketClassifier
copy .env.example .env      # (opcional) ajuste o provedor de IA
docker compose up --build
```

- Frontend: http://localhost:5174
- API (Swagger): http://localhost:8080/swagger
- Postgres: localhost:5433

Suba um CSV (use o `sample-tickets.csv` incluso), veja as estatísticas e baixe o CSV enriquecido.

## IA: mock / Anthropic / Gemini

Por padrão roda em `mock` (heurística por palavras-chave) — funciona sem chave.
Para usar um LLM, no `.env`:

```
# Claude
LLM_PROVIDER=anthropic
ANTHROPIC_API_KEY=sk-ant-...
ANTHROPIC_MODEL=claude-haiku-4-5-20251001

# ou Gemini
LLM_PROVIDER=gemini
GEMINI_API_KEY=...
GEMINI_MODEL=gemini-2.5-flash
```

Se a chave do provedor escolhido não estiver setada, a factory faz fallback para `mock`.

## Formato do CSV de entrada

Colunas reconhecidas (case-insensitive, detecta o delimitador):
- `subject` / `assunto` / `title`
- `description` / `descricao` / `body` / `message` / `text`
- `id` (opcional)

Se não houver coluna de descrição, o texto de todas as colunas é usado.

## CSV enriquecido (saída)

`Id, Assunto, Descricao, Tipo, Prioridade, Confianca`

- **Tipo:** Bug | Dúvida | Financeiro | Solicitação | Reclamação | Outros
- **Prioridade:** Baixa | Média | Alta | Urgente

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/tickets/upload` | Envia CSV, classifica e persiste o lote |
| GET  | `/api/tickets/batches` | Lista lotes |
| GET  | `/api/tickets/batches/{id}` | Lote + tickets + estatísticas |
| GET  | `/api/tickets/batches/{id}/export` | Baixa o CSV enriquecido |

## Rodar sem Docker (dev)

**Backend:**
```bash
cd backend/TicketClassifier.Api
# Postgres em localhost:5433 (ou ajuste a connection string em appsettings.json)
dotnet run
```

**Frontend:**
```bash
cd frontend
npm install
npm run dev
```
