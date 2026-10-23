# 4. Optional: deploy to Azure

> **Note:** We haven't tested this guide end to end yet. If a step doesn't work, please [open an issue](https://github.com/Code-Express-AI/trading-mcp-dotnet/issues).

This guide moves the database to Azure SQL Database, deploys the MCP server to Azure App Service, and replaces the local bearer tokens with Microsoft Entra ID. The [Entra setup guide](entra-setup.md) has the detailed app-registration and token steps.

Everything here fits Azure's free tiers: the [Azure SQL Database free offer](https://learn.microsoft.com/azure/azure-sql/database/free-offer) and the App Service **Free (F1)** plan. The free database is serverless and pauses when idle, so the first connection after a pause can take longer than the default 15 seconds. If you see timeouts, add `Connect Timeout=60` to the connection string.

## 1. Create and prepare Azure SQL

1. Create an Azure SQL logical server and an empty database. Add a firewall rule for your own machine's IP address while you set it up.
2. Connect to the database as its administrator and run `sql/01_schema.sql`, `sql/02_security.sql` and `sql/03_procedures.sql`, in that order. Skip `00_create_database.sql`. In `02_security.sql`, replace the password placeholder with a strong password for `mcp_reader`.
3. Load the data from your machine, connecting as the administrator:
   ```sh
   dotnet run --project DataGenerator -- "Server=tcp:<server>.database.windows.net,1433;Database=<database>;User ID=<admin>;Password=<admin-password>;Encrypt=True" --reset
   ```

Never use `TrustServerCertificate=True` with Azure SQL: Azure has a proper certificate, so leave certificate checking on.

## 2. Deploy the MCP server

Create a Linux App Service using the .NET 10 runtime, then publish the server:

```sh
dotnet publish McpServer/McpServer.csproj --configuration Release --output ./publish
```

Deploy the `publish` folder with your preferred method, such as the VS Code Azure App Service extension or a CI/CD pipeline. Then add these settings under the App Service's **Configuration** (or **Environment variables**):

| Setting | Value |
|---|---|
| `Auth__Mode` | `Entra` |
| `Entra__TenantId` | Your Entra tenant ID |
| `Entra__ClientId` | The client ID of the server's API registration |
| `ConnectionStrings__Trading` | The Azure SQL connection string for `mcp_reader` |
| `AllowedHosts` | `<app-name>.azurewebsites.net` (requests for any other host name are rejected) |

App Service runs the server in Production, so it never reads `.env`, and `appsettings.Production.json` turns on `Audit:FailClosed`: if a tool call can't be written to the audit log, its results aren't returned.

Treat the connection string as a secret. For anything beyond a short-lived demo, keep it in Azure Key Vault and use a Key Vault reference in the App Service settings. Let App Service reach Azure SQL through private networking rather than broad firewall rules.

## 3. Configure Entra ID and test

Follow [Entra ID authentication for the MCP server](entra-setup.md) to register the API, expose the `mcp.read` scope, set up a client and get a token. The server only validates Entra tokens; it never issues them.

Then check the deployment:

1. Open `https://<app-name>.azurewebsites.net/health`. It should return `{"status":"ok"}`.
2. Call a tool with MCP Inspector, using the Entra token:
   ```sh
   npx @modelcontextprotocol/inspector --cli https://<app-name>.azurewebsites.net/mcp --header "Authorization: Bearer <entra-token>" --method tools/call --tool-name get_dataset_info
   ```
3. Check the audit log. The `Caller` column now shows the Entra identity instead of a token name.
4. Optionally, turn on automatic digest storage for the ledger, under the database's **Ledger** settings in the Azure portal. Azure SQL then saves the audit log's hash chain to immutable storage, so even someone with full control of the database server can't quietly rewrite it.

Not every interactive MCP client can complete Entra's sign-in flow, so test your chosen client before relying on it. See the [limitations in the Entra guide](entra-setup.md#5-what-works-today-and-what-doesnt).

## Before you go to production

This project is a demo. Before putting an MCP server like this in front of a real database, work through this list:

- **Limit which rows each caller can see.** Every authenticated caller can currently read every client. Use SQL Server row-level security, with the server setting the caller's identity in `SESSION_CONTEXT`, so each user only sees their own clients.
- **Decide what data may reach the model.** Tool results go to whichever model the host uses. With a cloud model, client names and holdings leave your boundary. Mask or pseudonymise personal data, leave it out of tool results, or use a local model.
- **Remove the database password.** Connect to Azure SQL with a managed identity, and keep any remaining secrets in Key Vault.
- **Use a read-only replica.** Point the server at a readable secondary (`ApplicationIntent=ReadOnly`), so model traffic can't slow down the system the business runs on.
- **Lock down the database network.** Disable public access and connect from App Service through a private endpoint. This needs VNet integration, which requires the Basic plan or above. If the database stays public, allow only the App Service's outbound IP addresses and your admin machine. Don't turn on **Allow Azure services and resources to access this server**: it lets in every Azure customer's resources, not just your app.
- **Lock down who can reach the server.** HTTPS only. If your users work from known networks, allow only those addresses with App Service access restrictions. If they're all in a few countries, a geo-filter on Azure Front Door's WAF, or Cloudflare, blocks the rest of the world before requests reach the app. Treat it as a noise filter, not a security control: a VPN gets around it, and it blocks your own users when they travel.
- **Ship logs and audit data off the server.** Send logs to Application Insights or Log Analytics. Remember that the audit log records the arguments, such as names searched for, so treat it as sensitive.
- **Use short-lived tokens.** The local static tokens never expire. Use Entra ID, so tokens expire and access can be revoked centrally.
