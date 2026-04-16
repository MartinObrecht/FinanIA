# FinanIA Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-16

## Active Technologies
- SQLite (desenvolvimento local) via EF Core; migration `AddTransaction` adicionada (002-transactions-balance)
- C# 13 / .NET 10 + `Microsoft.Extensions.AI` 10.* (IChatClient — provedor de IA plugável, agnóstico de provedor) · (003-ai-financial-assistant)
- SQLite (EF Core) — nenhuma migration nova; leitura via repositórios existentes (003-ai-financial-assistant)
- C# 13 / .NET 10 (LTS, lançado nov/2025) + ASP.NET Core Web API 10, EF Core 10 + SQLite (dev), Microsoft.AspNetCore.Authentication.JwtBearer 10, BCrypt.Net-Next 4.x, FluentValidation 11 (001-solution-foundation)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# 13 / .NET 10 (LTS, lançado nov/2025)

## Code Style

C# 13 / .NET 10 (LTS, lançado nov/2025): Follow standard conventions

## Recent Changes
- 004-ollama-llama3-docker: Added [if applicable, e.g., PostgreSQL, CoreData, files or N/A]
- 003-ai-financial-assistant: Added C# 13 / .NET 10 + `Microsoft.Extensions.AI` 10.* (IChatClient — provedor de IA plugável, agnóstico de provedor)
- 002-transactions-balance: Added C# 13 / .NET 10 (LTS, lançado nov/2025) + ASP.NET Core Web API 10, EF Core 10 + SQLite (dev),


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
