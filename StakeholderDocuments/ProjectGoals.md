# Objetivos do Projeto

Construir um assistente financeiro pessoal com IA. O objetivo é capacitar qualquer pessoa a
ter controle sobre suas finanças, consultando saldos, registrando transações e obtendo
insights personalizados, tudo em linguagem natural e sem exigir conhecimento financeiro avançado.

## Propósito

O aplicativo existe para dar ao usuário um panorama claro e atualizado das suas finanças
pessoais, com o auxílio de um assistente de IA que responde perguntas baseadas
exclusivamente nos dados reais do próprio usuário.

## Escopo alvo (MVP)

Este é um aplicativo de prova de conceito para uso individual. Roda localmente e é projetado
para ser desenvolvido e testado no Windows, macOS ou Linux.

O MVP inclui apenas:

- Registro de transações financeiras (receitas e despesas) com descrição, valor, data e tipo
- Consulta do saldo atual e resumo de entradas e saídas
- Conversa com o assistente IA sobre os dados financeiros do usuário autenticado

Todos os demais recursos (categorias, filtros, gráficos, metas, exportação, etc.) são diferidos
para o Extended-MVP ou versões futuras.

## Abordagem de entrega

O foco é no desenvolvimento rápido da funcionalidade MVP. Construir a funcionalidade mínima
primeiro:

- Registrar uma transação (receita ou despesa).
- Visualizar saldo atual e totais de entradas/saídas.
- Fazer uma pergunta ao assistente IA e receber uma resposta baseada nos dados do usuário.

Para manter o desenvolvimento ágil:

- Sem categorias, tags ou filtros avançados no MVP
- Armazenamento em banco de dados relacional desde o início (SQLite para desenvolvimento local)
- Autenticação JWT mínima (sem OAuth externo no MVP)
- UI simples e funcional — sem polimento visual excessivo

## O que significa "MVP funcionando"

O MVP está completo quando:

1. Um usuário consegue se registrar e autenticar.
2. O usuário consegue registrar uma transação de receita ou despesa.
3. O usuário consegue visualizar seu saldo atual.
4. O usuário consegue fazer uma pergunta ao assistente IA e receber uma resposta baseada nos
   seus dados financeiros.

Gráficos, categorias e exportação de dados **não são** requisitos para o MVP.

## Extended-MVP (próxima fase)

Após o MVP básico estar funcionando, o Extended-MVP adiciona:

1. Categorização de transações (ex.: alimentação, transporte, lazer).
2. Filtros por período e categoria na listagem de transações.
3. Gráfico de pizza/barra com distribuição de gastos por categoria.

### Checklist de desenvolvimento local

Antes de testar o MVP, verifique:

- [ ] Backend roda sem erros e escuta na porta configurada
- [ ] Frontend roda sem erros e carrega no navegador
- [ ] Frontend está com a URL do backend correta nas configurações
- [ ] CORS do backend permite a origem do frontend
- [ ] Autenticação JWT está funcionando (login retorna token, rotas protegidas rejeitam sem token)
- [ ] DevTools do navegador não mostra erros de conexão ao carregar a página

## Melhorias futuras (pós-MVP)

Uma vez que o Extended-MVP estiver funcionando, estes recursos podem ser adicionados:

- **Metas financeiras**: Definir objetivos de economia e acompanhar progresso
- **Orçamentos por categoria**: Estabelecer limites mensais de gasto por categoria
- **Recorrências**: Registrar transações recorrentes (aluguel, assinaturas)
- **Exportação**: Baixar extrato em CSV/PDF
- **Multi-conta**: Suporte a múltiplas contas (corrente, poupança, cartão)
- **Conformidade LGPD ampliada**: Exportação completa de dados pessoais e direito ao
  esquecimento com fluxo de UI dedicado
- **Notificações**: Alertas ao aproximar do limite de orçamento

## Nota sobre escolha tecnológica

As escolhas tecnológicas (ASP.NET Core + Clean Architecture + IA plugável via `IChatClient`) devem suportar
recursos futuros prontos para produção sem exigir uma reescrita completa. A arquitetura
permite adicionar persistência avançada, processamento em background e capacidades de UI
aprimoradas conforme necessário.

## Como este documento se relaciona com os demais

- [AppFeatures.md](AppFeatures.md) descreve as funcionalidades específicas voltadas ao usuário para o MVP
- [TechStack.md](TechStack.md) explica as escolhas tecnológicas e como elas suportam os objetivos do MVP
