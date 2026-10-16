# Secure, read-only MCP server for SQL Server (C# / .NET)

System which allows an AI assistant to query a SQL Server database **without being able to change the database or any of the underlying data**. 
- A minimal API hosts an MCP server using .NET minimal api
- MCP server calls stored procedures via Dapper
- Database login has been granted the least set of priviliges in order to perform its tasks
- MCP server secured using bearer tokens (development)
- MCP server secured using Entra Id (production). Entra Id was formely known as Azure Active Directory.
- Passwords and access keys stored in environment variables (not in code) 

<!-- Add a short GIF of the finished demo here -->
<!--![VS Code Copilot calling MCP server](demo.gif)-->

> **All data is synthetic.** The companies, clients and prices are invented. The project is for educational purposes only. It is not a production-ready system.

## High Level Architecture

```mermaid
flowchart LR
    U([You]) --> H["Host<br/>(VS Code or similar)<br/>+ LLM"]
    H -- "MCP over HTTP<br/>+ bearer token" --> S["MCP server<br/>(.NET minimal API)<br/>auth, audit, rate limit"]
    S -- "Dapper:<br/>EXECUTE only" --> D[("SQL Server<br/>stored procedures")]
```

The model decides which tool to call; the host makes the call and returns the result. The server only ever runs
stored procedures, with a login that has no table permissions (uses the principle of least privilege - PoLP).

## Episodes

| # | Video | Git tag | Folders |
|---|-------|---------|---------|
| 1 | Introduction and demo | `ep1-intro` | none |
| 2 | The database and its security | `ep2-database` | `sql/`, `DataGenerator/`, `docker/` |
| 3 | Building the MCP server | `ep3-server` | `McpServer/` |
| 4 | Audit, rate limiting and a real host with a local LLM | `ep4-audit-llm` | `McpServer/` |
| 5 | Deploying to Azure's free tiers (plus the optional Entra ID mode) | `ep5-azure` | `McpServer/`, `docs/` |

Video links: *add as each one is published.* To follow along with an episode's exact code:
`git checkout <tag>`.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 8 or later
- [Docker](https://www.docker.com/products/docker-desktop/)
- [VS Code](https://code.visualstudio.com/) (or another MCP-capable host)
- [Git](https://git-scm.com/)
- An Azure account, only for the deployment episode

While you don't need a deep understanding of .NET or SQL Server, some familiarity with stored procedures, minimal API and perhaps dapper would be useful. An introductory understanding of docker would also be useful.




## Quick start

```bash
git clone https://github.com/Code-Express-AI/trading-mcp-dotnet.git
cd trading-mcp-dotnet

cp .env.example .env                  # set both passwords
nano .env                             # open the .env file in a text editor and update the passwords. Save file
docker compose up -d
docker compose run --rm db-init

cd DataGenerator
dotnet run -- "Server=localhost,1433;Database=TradingDemo;User ID=sa;Password=<SA_PASSWORD>;Encrypt=True;TrustServerCertificate=True" --reset
```

Then configure and run the server: see [docs/setup.md](docs/setup.md).
The MCP server then connects as the read-only user:

```bash
cd McpServer
dotnet user-secrets set "ConnectionStrings:Trading" "Server=localhost,1433;Database=TradingDemo;User ID=mcp_reader;Password=<MCP_READER_PASSWORD>;Encrypt=True;TrustServerCertificate=True"

dotnet user-secrets set "Mcp:Tokens:inspector" "$(openssl rand -hex 32)"
dotnet run --urls http://localhost:5080
```

Test

http://localhost:5080/health  => OK.
http://localhost:5080/mcp => 401 (coz we have no token)

```bash
cd McpServer
dotnet user-secrets list


```

## Security design

- The MCP database user has **no table permissions**, only `EXECUTE` on a fixed set of stored procedures.
- Procedures return fixed columns with hard row limits and use no dynamic SQL.
- Sensitive columns are masked, and every tool call is written to an audit table.
- Authentication: named bearer tokens for local work, Microsoft Entra ID tokens for production
  ([details and known client limitations](docs/entra-setup.md)).

## Troubleshooting

# SQL Server on Mac
Microsoft does not provide a native ARM64 Docker image for SQL Server. The official images (mcr.microsoft.com/mssql/server) are built strictly for x86-64 (Intel/AMD) architectures.

However, you can easily run SQL Server on your ARM64 M5 Mac using Docker Desktop by leveraging macOS's built-in Rosetta 2 emulation layer. Historically, x86 emulation caused SQL Server to crash on startup, but Docker Desktop's modern Rosetta integration fully resolves this.

I tested SQL Server running on Docker on both intel-based macs and ARM64 macs and it worked fine. 

# .NET Version
Both the DataGenerator.csproj file and the McpServer.csproj file target .NET 10.0. I've tested the code on .NET 8.0 so you can change the  <TargetFramework>net8.0</TargetFramework> if required.

# Registering MCP with Copilot and VSCode
Biggest issue I found was trying to get VS Code and Copilot to successfully call into the MCP server. After registering the mcp server with VS Code (using the /.vscode/mcp.jsom file) I manually started the mcp server using VS Code. This was a mistake. You only need to register the mcp server. Once you open Copilot chat, it will decide if it needs to start the mcp server or not.

## Tested with

| Component | Version | Date checked |
|-----------|---------|--------------|
| .NET SDK | 8.0.x | 15-Oct-2026 |
| ModelContextProtocol.AspNetCore | 2.1.0 | 15-Oct-2026 |
| SQL Server container | 2022-latest | 15-Oct-2026|
| Host / model | VS Code / Copilot | 15-Oct-2026 |


The MCP SDK and its authorization support are changing quickly, so check the repo's issues for known problems.

## Licence

MIT. See [LICENSE](LICENSE).

## ToDo
[![build](https://github.com/Code-Express-AI/trading-mcp-dotnet/actions/workflows/build.yml/badge.svg)](https://github.com/Code-Express-AI/trading-mcp-dotnet/actions/workflows/build.yml)
