# Stack Tecnológica do FinanIA

O FinanIA utilizará um backend **ASP.NET Core Web API** com **Clean Architecture** e um
assistente de IA com suporte a múltiplos provedores: **Google Gemini** (produção) e
**Ollama/Llama 3** (desenvolvimento local e cenários sensíveis a custo). Esta combinação
permite desenvolvimento rápido do MVP enquanto suporta evolução para um produto pronto para
produção sem lock-in de provedor.

## Por que ASP.NET Core + Clean Architecture + Multi-Provider AI?

Construir um assistente financeiro pessoal com **ASP.NET Core Web API**, **Clean Architecture**
e suporte a múltiplos provedores de IA (Gemini, Ollama) oferece diversas vantagens:

1. **Domínio Isolado e Testável**: A Clean Architecture isola as regras de negócio financeiro
   de frameworks, banco de dados e IA, tornando o domínio 100% testável sem dependências externas.

2. **Troca de Provedor de IA sem Custo**: Alternar entre Gemini (cloud) e Ollama/Llama 3
   (local) é feito via configuração — o domínio e a aplicação não são afetados. Isto elimina
   lock-in e permite controle de custos de inferência.

3. **Segurança por Design**: A separação em camadas facilita garantir que toda query ao banco de
   dados filtre por `UserId` e que prompts enviados à IA nunca contenham dados de outros usuários.

4. **Conformidade com LGPD**: A arquitetura em camadas permite implementar direito ao
   esquecimento e exportação de dados pessoais como operações de infraestrutura, sem contaminar
   o domínio.

5. **Escalabilidade Incremental**: Começa com SQLite para desenvolvimento local, mas a abstração
   de repositórios permite migrar para PostgreSQL ou SQL Server sem reescrita.

6. **Plataforma Cruzada**: ASP.NET Core roda em Windows, macOS e Linux, facilitando
   desenvolvimento e CI/CD.

## Responsabilidades por camada

Seguindo a estrutura de projetos C# da solução (`FinanIA.Domain`, `FinanIA.Application`,
`FinanIA.Infrastructure`, `FinanIA.Api`, `FinanIA.Web`), todos residindo sob `src/`:

### FinanIA.Domain

- Entidades: `Transaction`, `User`, `Category`, `ConversationMessage`
- Interfaces de repositório: `ITransactionRepository`, `IUserRepository`
- Interfaces de serviço de IA: `IFinancialAssistant`
- Regras de negócio puras (sem dependência de frameworks externos)

### FinanIA.Application

- Casos de uso / command handlers: `RegisterTransactionCommand`, `GetBalanceQuery`,
  `AskAssistantCommand`
- DTOs de entrada e saída
- Orquestração entre repositórios e serviço de IA
- Validações de negócio (ex.: valor da transação deve ser positivo)

### FinanIA.Infrastructure

- Implementação dos repositórios com EF Core + SQLite (desenvolvimento) / PostgreSQL (produção)
- Implementações do `IFinancialAssistant`:
  - `GeminiFinancialAssistant`: provedor cloud via `Mscc.GenerativeAI.Microsoft` (produção)
  - `OllamaFinancialAssistant`: provedor local via `OllamaSharp` + `Microsoft.Extensions.AI`
    (desenvolvimento e cenários sensíveis a custo)
- Seleção de provedor por configuração (`AI:Provider` em `appsettings.json` /
  variáveis de ambiente); padrão em `Development`: Ollama; padrão em `Production`: Gemini
- Construção e sanitização de prompts enviados à IA
- Configuração de autenticação JWT
- Migrations versionadas do banco de dados

### FinanIA.Api

- Controllers REST (endpoints HTTP)
- Configuração de middlewares (autenticação, CORS, logging)
- Injeção de dependências
- Configuração de ambiente (leitura de variáveis de ambiente / Azure Key Vault)

### FinanIA.Web

- Interface Blazor WebAssembly (camada de apresentação)
- Páginas: registro de transação, visualização de saldo, chat com a IA
- Consome apenas DTOs e contratos expostos pelo `FinanIA.Api`; sem acesso direto a entidades de domínio
- Configuração da URL do backend em `wwwroot/appsettings.json` (nunca hardcoded)

## Abordagem MVP-first

Para entregar o MVP rapidamente:

**MVP (autenticação + transações + IA básica):**

- **Banco de dados**: SQLite com EF Core. Simples, sem servidor, ideal para desenvolvimento local
- **Autenticação**: JWT simples gerado internamente; sem OAuth externo no MVP
- **IA**: Ollama/Llama 3 (local) no ambiente de desenvolvimento; Gemini API na produção;
  prompt construído a partir das transações do usuário autenticado; provedor selecionável
  por configuração
- **Frontend**: Interface web mínima (Blazor WebAssembly) focada nas três
  funcionalidades: registrar transação, ver saldo, conversar com a IA
- **Sem**: categorias, filtros, gráficos, exportação, recorrências

**Extended-MVP (adicionar categorias, filtros e gráficos):**

- **Categorias**: Adicionar entidade `Category` com migração versionada
- **Queries analíticas**: Agregações por categoria e período, sempre filtradas por `UserId`
- **Gráficos**: Biblioteca de visualização leve no frontend
- **Histórico de chat**: Persistência das mensagens da conversa, isolada por usuário e
  criptografada em repouso

## Segurança e conformidade

- **JWT**: Expiração curta (≤ 1h) com refresh token rotativo; suporte a revogação (logout forçado)
- **Autorização**: Toda query ao banco filtra por `UserId` do token — sem exceções
- **Prompt injection**: Todo input do usuário é sanitizado antes de ser incluído no prompt da IA
- **Segredos**: API keys, connection strings e chaves JWT NUNCA são versionados; gerenciados via
  variáveis de ambiente ou Azure Key Vault
- **Logs**: Logs nunca contêm dados financeiros identificáveis em texto claro (mascaramento obrigatório)
- **Dependências**: Pacotes NuGet auditados regularmente (`dotnet list package --vulnerable`)

## Desenvolvimento local

### Configuração inicial do banco de dados

```bash
# Aplicar todas as migrations na base local (SQLite)
dotnet ef database update --project src/FinanIA.Infrastructure --startup-project src/FinanIA.Api
```

### Configuração de portas

O backend API e o frontend rodam em portas localhost separadas. **Consistência de portas é
crítica** — as portas devem estar coordenadas em três lugares:

1. **Porta do backend** (definida em `src/FinanIA.Api/Properties/launchSettings.json`):
   - Padrão: `http://localhost:5200`
   - É onde a API escuta por requisições

2. **Porta do frontend** (definida nas configurações do projeto de UI):
   - Padrão: `http://localhost:5201`
   - É onde o app frontend roda

3. **CORS** (configurado em `src/FinanIA.Api/Program.cs`):
   - Deve permitir a origem do frontend
   - Exemplo: `.WithOrigins("http://localhost:5201")`

### Configuração de variáveis de ambiente

O arquivo `appsettings.Development.json` **não** deve conter segredos reais. Use `dotnet
user-secrets` para desenvolvimento local:

```bash
dotnet user-secrets set "Gemini:ApiKey" "sua-chave-aqui" --project src/FinanIA.Api
dotnet user-secrets set "Jwt:Secret" "sua-chave-jwt-secreta" --project src/FinanIA.Api
```

### Boas práticas de configuração

- **Leia configurações do ambiente**, nunca hardcode:

  ```csharp
  var geminiApiKey = builder.Configuration["Gemini:ApiKey"]
      ?? throw new InvalidOperationException("Gemini:ApiKey not configured");
  ```

- **Teste de pré-voo antes do desenvolvimento**:
  1. Backend roda sem erros na porta configurada
  2. Endpoint de health check responde: `GET /health`
  3. Endpoint de autenticação funciona: `POST /api/auth/register` e `POST /api/auth/login`
  4. CORS permite a origem do frontend
  5. Console do navegador (F12) não mostra erros de conexão

## Melhorias futuras (pós-MVP)

Quando o projeto evoluir além da demonstração básica, esta arquitetura suporta:

- **Banco de dados relacional completo**: Migrar de SQLite para PostgreSQL via troca de provider
  no EF Core, sem alterar repositórios ou domínio
- **OAuth 2.0 externo**: Adicionar login social (Google, Microsoft) como provedor adicional
- **Background jobs**: Implementar `BackgroundService` para processar recorrências ou gerar
  resumos periódicos
- **Cache**: Adicionar Redis para cachear resumos financeiros e reduzir chamadas ao banco
- **Testes de integração**: Ampliar cobertura com testes end-to-end usando `WebApplicationFactory`
- **Observabilidade**: Adicionar OpenTelemetry para rastreamento distribuído e métricas
- **Deploy containerizado**: Dockerizar a API e o frontend para deploy em qualquer cloud

## Resumo

ASP.NET Core com Clean Architecture e Google Gemini API fornece um caminho direto para construir
o assistente financeiro de forma incremental:

- **MVP**: Autenticação + CRUD de transações + chat IA baseado nos dados do usuário — simples
  e focado na proposta de valor central
- **Extended-MVP**: Categorias, filtros e gráficos — adiciona valor analítico sem reescrever a base
- **Futuro**: Persistência avançada, processamento em background, OAuth e features de produto
  completo

A arquitetura é intencionalmente minimalista para permitir desenvolvimento ágil, enquanto as
escolhas tecnológicas suportam a adição de funcionalidades prontas para produção posteriormente
sem exigir uma refatoração completa.
