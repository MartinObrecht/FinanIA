<!--
SYNC IMPACT REPORT
==================
Version change: 1.2.0 → 1.3.0
Bump rationale: MINOR — Principle II reverted from multi-provider AI to Gemini-only;
  Ollama/Llama 3 local provider removed (feature 004-ollama-provider não adotada).
  Principles III and IV provider references updated accordingly.
  Dev Workflow prompt review line simplified.

Modified principles:
  - Principle II: Revertido para provedor único (Gemini); opção Ollama/Llama 3, política de
    seleção de provedor e diretriz de controle de custo removidas.
  - Principle III: Referência "(Gemini, Ollama)" revertida para "(Gemini)".
  - Principle IV: Exemplo de troca de provedor simplificado; referência a Ollama/Llama 3 removida.

Added sections: none
Removed sections: none

Templates alignment:
  ✅ plan-template.md     — Nenhuma alteração necessária; gate Constitution Check inalterado
  ✅ spec-template.md     — Nenhuma alteração necessária
  ✅ tasks-template.md    — Nenhuma alteração necessária
  ✅ .github/copilot-instructions.md — Entrada OllamaSharp / 004-ollama-provider removida
  ✅ StakeholderDocuments/TechStack.md — Revertido para provedor único (Gemini)

Deferred TODOs: none
-->

# FinanIA Constitution

## Core Principles

### I. Segurança e Privacidade pelo Design

Dados financeiros são altamente sensíveis e DEVEM ser protegidos em todos os níveis da stack.

- Todo acesso a dados DEVE ser autenticado (JWT / OAuth 2.0) e autorizado por escopo de usuário.
- Dados em repouso DEVEM ser criptografados; dados em trânsito DEVEM usar TLS 1.2+.
- Dados de um usuário NUNCA podem ser acessíveis por outro, mesmo em erros de lógica.
- A coleta, tratamento e armazenamento de dados pessoais DEVE estar em conformidade com a LGPD.
- Segredos (API keys, connection strings) NUNCA devem ser versionados no repositório; DEVEM ser
  gerenciados via variáveis de ambiente ou cofre de segredos (ex.: Azure Key Vault).

**Rationale**: Violações de privacidade em sistemas financeiros expõem usuários a fraudes e
destroem a confiança no produto de forma irreversível.

### II. IA Contextualizada e Responsável (NÃO NEGOCIÁVEL)

O assistente de IA DEVE operar exclusivamente sobre os dados financeiros do usuário autenticado,
independentemente do provedor de modelo utilizado.

- O modelo NUNCA recebe dados de outros usuários no contexto de uma requisição.
- Respostas da IA DEVEM ser fundamentadas nos dados reais do usuário; especulações ou
  conteúdo financeiro genérico não baseado nos dados do usuário são proibidos.
- Toda resposta DEVE incluir um aviso claro de que não substitui aconselhamento financeiro
  profissional.
- Prompts enviados ao provedor de IA DEVEM ser sanitizados para prevenir prompt injection.
- O histórico de conversas DEVE ser isolado por usuário e criptografado em repouso.
- O provedor de IA é **Gemini** (Google), via `Mscc.GenerativeAI.Microsoft`; futuras trocas
  de provedor NUNCA DEVEM exigir alterações no domínio ou na camada de aplicação.

**Rationale**: Dados financeiros incorretos ou de outros usuários podem causar decisões
prejudiciais; a responsabilidade ética e legal exige rastreabilidade e disclaimers.

### III. Test-First (NÃO NEGOCIÁVEL)

TDD é obrigatório em todo código de domínio e de integração.

- O ciclo DEVE ser: Testes escritos → Revisão/aprovação → Testes vermelhos → Implementação →
  Verde → Refatoração.
- Nenhuma funcionalidade de domínio PODE ser entregue sem cobertura de testes unitários.
- Integrações com o provedor de IA (Gemini) e banco de dados DEVEM ter testes de
  integração ou de contrato.
- A cobertura mínima de linhas no domínio DEVE ser ≥ 80 %.
- Testes DEVEM ser determinísticos; dependências externas DEVEM ser mockadas nos testes unitários.

**Rationale**: Finanças pessoais não admitem regressões silenciosas; TDD garante que o
comportamento esperado é documentado como código executável antes da entrega.

### IV. Arquitetura Limpa e Domínio Isolado

O domínio financeiro DEVE ser independente de frameworks, IA e infraestrutura (Clean Architecture).

- Camadas DEVEM respeitar a dependência invertida: Domínio → Aplicação → Infraestrutura/UI.
- Mudanças no provedor de IA ou no
  banco de dados NÃO DEVEM requerer alterações no domínio.
- Dependências externas (provedores de IA, EF Core, repositórios) DEVEM ser abstraídas por interfaces
  definidas no domínio.
- Projetos da solução C#/.NET DEVEM refletir as camadas: `FinanIA.Domain`, `FinanIA.Application`,
  `FinanIA.Infrastructure`, `FinanIA.Api`, `FinanIA.Web`.
- O projeto `FinanIA.Web` (Blazor WebAssembly) é a camada de apresentação; DEVE depender apenas
  de DTOs e contratos da camada `FinanIA.Application` — nunca de entidades de domínio diretamente.
- Todos os projetos DEVEM residir sob o diretório `src/` na raiz do repositório.

**Rationale**: Isolar o domínio garante que as regras de negócio financeiro permaneçam testáveis,
portáveis e livres de acoplamento acidental com tecnologias que podem mudar.

### V. Experiência do Usuário Clara e Acessível

Interfaces e respostas DEVEM ser compreensíveis para usuários não técnicos sem conhecimento
financeiro avançado.

- Respostas do assistente IA DEVEM usar linguagem natural simples; jargão técnico ou financeiro
  DEVE ser evitado ou explicado.
- Erros apresentados ao usuário DEVEM ter mensagens em português, claras e orientadas a ação;
  stack traces NUNCA devem ser expostos ao usuário final.
- Fluxos críticos (registro de transação, consulta de saldo, conversa com IA) DEVEM ser
  completados em no máximo 3 interações.
- A acessibilidade (WCAG 2.1 AA) DEVE ser considerada em qualquer interface web ou mobile.

**Rationale**: O produto serve pessoas físicas buscando controle financeiro; complexidade de
interface é uma barreira de adoção e um risco de abandono.

### VI. Simplicidade e YAGNI

Complexidade DEVE ser justificada por um requisito real, presente e documentado.

- Abstrações prematuras, generalizações e "flexibilidade para o futuro" são proibidas sem
  requisito concreto.
- Cada decisão de design arquitetural não trivial DEVE ser registrada em um ADR (Architecture
  Decision Record) dentro de `docs/adr/`.
- Dependências externas adicionadas ao projeto DEVEM ter sua necessidade justificada no PR.
- O modelo de dados DEVE começar mínimo e evoluir por migrações versionadas.

**Rationale**: Over-engineering em estágios iniciais desperdiça tempo, dificulta manutenção e
obscurece o domínio de negócio.

## Security & Compliance Requirements

- **Autenticação**: JWT com expiração curta (≤ 1h) + refresh token rotativo; DEVE suportar
  logout forçado via revogação de token.
- **Autorização**: Toda query ao banco DEVE filtrar pelo `UserId` do token; filtros NUNCA podem
  ser omitidos por conveniência.
- **LGPD**: O sistema DEVE implementar: direito ao esquecimento (exclusão de conta + dados),
  exportação de dados pessoais, e registro de consentimento.
- **Dependências**: Pacotes NuGet DEVEM ser auditados regularmente (`dotnet list package
  --vulnerable`); vulnerabilidades críticas DEVEM ser corrigidas antes do próximo release.
- **Logs**: Logs NUNCA devem conter dados financeiros identificáveis (valores, descrições de
  transações) em texto claro; usar mascaramento ou pseudonimização.

## Development Workflow

- **Branches**: `main` é protegida; toda alteração DEVE vir via Pull Request com ao menos 1
  aprovação.
- **TDD gate**: PRs com código de domínio sem testes correspondentes DEVEM ser rejeitados.
- **CI/CD**: O pipeline DEVE executar build, testes unitários e de integração, análise estática
  (ex.: SonarQube, Roslyn Analyzers) e auditoria de vulnerabilidades antes do merge.
- **Migrações**: Toda alteração de schema DEVE incluir migration versionada; rollback DEVE ser
  testado localmente antes do merge.
- **Revisão de IA prompts**: Mudanças nos prompts enviados ao provedor de IA (Gemini) DEVEM
  ser revisadas como código e testadas com cenários de prompt injection.
- **ADRs**: Decisões arquiteturais significativas DEVEM ser documentadas em `docs/adr/` antes
  da implementação.

## Governance

Esta constituição supersede todas as outras diretrizes e acordos de equipe. Em caso de conflito,
a constituição prevalece.

- **Emendas**: Qualquer alteração DEVE ser proposta via PR, revisada pelo time, aprovada por
  maioria simples, e versionada segundo semver:
  - MAJOR — remoção ou redefinição incompatível de princípio.
  - MINOR — adição de princípio ou seção; expansão material de diretriz.
  - PATCH — clarificações, correções ortográficas, refinamentos não semânticos.
- **Conformidade**: Todo PR DEVE verificar conformidade com os princípios I–VI antes do merge.
  A seção "Constitution Check" do `plan-template.md` é o gate formal.
- **Revisão periódica**: A constituição DEVE ser revisada a cada trimestre ou após mudança
  significativa de escopo do produto.
- **Guidance file**: O arquivo `.specify/memory/constitution.md` é o documento vigente; versões
  anteriores são preservadas no histórico do Git.

**Version**: 1.3.0 | **Ratified**: 2026-04-04 | **Last Amended**: 2026-04-05
