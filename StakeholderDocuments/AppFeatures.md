# Funcionalidades do Aplicativo

O FinanIA demonstra o gerenciamento de finanças pessoais com assistência de inteligência
artificial como fundação de um aplicativo de controle financeiro.

## Escopo do MVP (versão prova de conceito)

O MVP demonstra a funcionalidade mínima viável: registro de transações e consulta ao
assistente IA sobre os próprios dados financeiros.

Para o MVP, o aplicativo DEVE:

- Permitir que o usuário se registre e faça login
- Registrar uma transação financeira informando: descrição, valor, data e tipo (receita/despesa)
- Exibir o saldo atual (total de receitas − total de despesas)
- Exibir uma listagem das transações registradas
- Permitir que o usuário converse com o assistente IA, que responde com base nos dados
  financeiros do próprio usuário autenticado
- Incluir aviso claro nas respostas da IA de que não substituem aconselhamento financeiro
  profissional

Para o MVP, o aplicativo PODE:

- Armazenar dados em SQLite (banco leve para desenvolvimento local)
- Aceitar qualquer string como descrição de transação sem validações complexas
- Exibir transações em lista simples, sem ordenação ou filtros avançados
- Limitar o histórico de conversas com a IA a uma sessão por vez (sem persistência entre sessões)

## Comportamento do MVP

O MVP segue regras simples:

- Usuários se autenticam com e-mail e senha; o token JWT expira em 1 hora
- Transações são associadas exclusivamente ao usuário autenticado — nenhum dado de outro usuário
  é acessível
- O saldo atualiza imediatamente após registrar uma transação
- O assistente IA responde apenas com base nos dados do usuário; especulações ou dados externos
  são proibidos
- Toda resposta da IA inclui o aviso: *"Esta resposta não substitui aconselhamento financeiro
  profissional."*
- Sem fetch de dados externos, integração bancária ou parsing de extratos no MVP

## Funcionalidades do Extended-MVP

Após o MVP básico (autenticação + transações + IA) estar funcionando, o Extended-MVP adiciona
capacidade analítica e de organização:

- **Categorias**: Associar cada transação a uma categoria (ex.: alimentação, transporte, lazer)
- **Filtros**: Filtrar transações por período (mês/ano) e por categoria na listagem
- **Resumo por categoria**: Exibir totais de gastos por categoria no período selecionado
- **Gráfico simples**: Gráfico de barras ou pizza mostrando distribuição de despesas por categoria
- **Histórico de conversa**: Manter histórico da sessão de chat com a IA, isolado por usuário

## Funcionalidades pós-MVP

Após desenvolver um Extended-MVP bem-sucedido, as seguintes funcionalidades podem ser
consideradas para versões futuras:

### Melhorias essenciais

- **Metas financeiras**: Definir objetivos de economia (ex.: "economizar R$ 5.000 até dezembro")
  e acompanhar progresso percentual
- **Orçamentos por categoria**: Estabelecer limite mensal de gastos por categoria com alerta ao se
  aproximar do limite
- **Remoção de transações**: Permitir que o usuário exclua ou edite transações registradas
- **Ordenação inteligente**: Exibir transações da mais recente para a mais antiga por padrão

### Capacidades adicionais

- **Recorrências**: Registrar despesas e receitas fixas recorrentes (aluguel, salário, assinaturas)
- **Multi-conta**: Suporte a múltiplas contas (conta corrente, poupança, cartão de crédito)
- **Exportação**: Baixar extrato completo em CSV ou PDF
- **Importação de extrato**: Upload de arquivo OFX/CSV de extratos bancários para registro em lote
- **Notificações**: Alertas ao atingir limite de orçamento ou ao receber nova receita
- **Dashboard avançado**: Gráficos de evolução patrimonial, comparativos mensais e tendências
- **Modo escuro / acessibilidade**: Suporte a tema escuro e conformidade com WCAG 2.1 AA

### Conformidade e privacidade

- **Direito ao esquecimento (LGPD)**: Fluxo de exclusão de conta com remoção completa de dados
  pessoais e financeiros
- **Exportação de dados pessoais (LGPD)**: Download de todos os dados do usuário em formato
  estruturado (JSON/CSV)
- **Registro de consentimento**: Registrar data/hora do aceite dos termos de uso e política de
  privacidade

## Notas práticas para desenvolvedores

**Para o MVP (autenticação + transações + IA):**

- Use SQLite com EF Core para persistência (setup simples, sem servidor)
- Implemente autenticação JWT com expiração de 1h + refresh token rotativo
- Use a Gemini API com prompt construído a partir das transações do usuário; nunca inclua dados
  de outros usuários no contexto
- Sanitize o conteúdo recebido do usuário antes de incluir no prompt enviado à IA (prevenir
  prompt injection)
- Foque em CRUD simples de transações e no fluxo de chat; sem lógica de categorias ainda

**Para o Extended-MVP (adicionar categorias, filtros e gráficos):**

- Adicione a entidade `Category` com relação a `Transaction`
- Queries de resumo por categoria devem filtrar obrigatoriamente por `UserId`
- Para gráficos, prefira uma biblioteca leve compatível com o frontend escolhido
- Teste o histórico de conversa com múltiplos usuários simultâneos para garantir isolamento
