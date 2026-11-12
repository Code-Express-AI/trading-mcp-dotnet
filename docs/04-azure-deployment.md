# 4. Optional: deploy to Azure

> **Note:** We haven't tested this guide end to end yet. If a step doesn't work, please [open an issue](https://github.com/Code-Express-AI/trading-mcp-dotnet/issues).

This guide moves the database to Azure SQL Database, deploys the MCP server to Azure App Service, and replaces the local bearer tokens with Microsoft Entra ID. The [Entra setup guide](entra-setup.md) has the detailed app-registration and token steps.

Everything here fits Azure's free tiers: the [Azure SQL Database free offer](https://learn.microsoft.com/azure/azure-sql/database/free-offer) and the App Service **Free (F1)** plan. The free database is serverless and pauses when idle, so the first connection after a pause can take longer than the default 15 seconds. If you see timeouts, add `Connect Timeout=60` to the connection string.

## 1. Create and prepare Azure SQL

1. Create an Azure SQL logical server and an empty database. Add a firewall rule for your own machine's IP address while you set it up.
2. Connect to the database as its administrator and run `sql/01_schema.sql`, `sql/02_security.sql` and `sql/03_procedures.sql`, in that order. Skip `00_create_database.sql`. In `02_security.sql`, replace the password placeholder with a strong password for `mcp_reader`.
3. Load the data from your machine, connecting as the administrator. Keep the single quotes, otherwise your shell can change characters such as `$` in the password:
   ```sh
   dotnet run --project DataGenerator -- 'Server=tcp:<server>.database.windows.net,1433;Database=<database>;User ID=<admin>;Password=<admin-password>;Encrypt=True' --reset
   ```
4. Turn on auditing for the logical server, under **Security** → **Auditing** in the Azure portal, sending the records to a Log Analytics workspace or a storage account. Azure SQL then records every query and procedure call itself, whoever runs it. The project's own audit log only covers the stored procedures; this also records direct queries and administrator activity. Either destination can cost a little beyond the free tiers.

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

If you add a custom domain, add it to `AllowedHosts` too, separating host names with `;`. A request for a host name that isn't listed gets a 400 response, and the log shows `The host '<name>' does not match an allowed host.`

Under **Configuration** → **General settings**, set **HTTPS Only** to **On** and **Minimum inbound TLS version** to **1.2**. Without HTTPS Only, a client that connects with `http://` sends its bearer token unencrypted. Redirecting to HTTPS doesn't help: the token has already been sent by the time the redirect comes back.

App Service runs the server in Production, so it never reads `.env`, and `appsettings.Production.json` turns on `Audit:FailClosed`: if a tool call can't be written to the audit log, its results aren't returned.

Treat the connection string as a secret. For anything beyond a short-lived demo, keep it in Azure Key Vault and use a Key Vault reference in the App Service settings. Let App Service reach Azure SQL through private networking rather than broad firewall rules.

## 3. Configure Entra ID and test

Follow [Entra ID authentication for the MCP server](entra-setup.md) to register the API, expose the `mcp.read` scope, set up a client and get a token. The server only validates Entra tokens; it never issues them.

Then give each Entra user or app the clients it may see. Row-level security keys an Entra caller on its object ID, and a caller with no rows in `trading.CallerClient` sees no clients at all. Get the object ID:

```sh
az ad signed-in-user show --query id -o tsv
az ad sp show --id <daemon-client-id> --query id -o tsv
```

The first command gives your own user's object ID, the second the service client's from [section 2B of the Entra guide](entra-setup.md#2-register-a-client). Connect as the administrator and add the rows. This gives the caller every client; pick a subset to restrict it:

```sql
INSERT INTO trading.CallerClient (CallerKey, ClientId)
SELECT N'<object-id>', ClientId FROM trading.Client;
```

Loading the data again with `--reset` deletes these rows, so add them again afterwards. The `local`, `copilot`, `claude` and `inspector` rows from the data generator are for static tokens and do nothing in Entra mode.

Then check the deployment:

1. Open `https://<app-name>.azurewebsites.net/health`. It should return `{"status":"ok"}`.
2. Call a tool with [MCP Inspector](02-run-server-and-inspector.md#2-test-it-with-mcp-inspector), using the Entra token:
   ```sh
   npx @modelcontextprotocol/inspector --cli https://<app-name>.azurewebsites.net/mcp --header "Authorization: Bearer <entra-token>" --method tools/call --tool-name get_dataset_info
   ```
3. Check the audit log. The `Caller` column now shows the Entra identity instead of a token name. The `CallerKey` column shows your object ID, and Azure SQL's own audit records, from step 4 of section 1, show the same call made by `mcp_reader`.
4. Turn on automatic digest storage for the ledger, under the database's **Security** → **Ledger** settings in the Azure portal. Azure SQL then saves the audit log's hash chain to immutable storage. Without a saved digest, someone with full control of the database server can drop the database, rebuild it and replay a doctored history, and nothing detects it.

Not every interactive MCP client can complete Entra's sign-in flow, so test your chosen client before relying on it. See the [limitations in the Entra guide](entra-setup.md#5-what-works-today-and-what-doesnt).

## Before you go to production

This project is a demo. Before putting an MCP server like this in front of a real database, work through this list:

- **Map real users to their clients.** Row-level security is in place, but the demo's mapping is fixed by the data generator. In production, keep `trading.CallerClient` in step with your source of truth, such as the CRM's client assignments. The database trusts the server to set the caller, so anyone with the `mcp_reader` password can claim to be anyone. To have the database check the user's identity itself, connect as the user with the Entra on-behalf-of flow.
- **Decide what data may reach the model.** Tool results go to whichever model the host uses. With a cloud model, client names and holdings leave your boundary. Mask or pseudonymise personal data, leave it out of tool results, or use a local model.
- **Remove the database password.** Connect to Azure SQL with a managed identity, and keep any remaining secrets in Key Vault.
- **Keep model traffic off the system the business runs on.** Point the server at a separate copy of the data. A read-only replica won't work as it stands: each read procedure writes its own row to `trading.AuditLog`, and a replica refuses writes. Either keep a writable reporting copy, or send that row somewhere else.
- **Lock down the database network.** Disable public access and connect from App Service through a private endpoint. This needs VNet integration, which requires the Basic plan or above. If the database stays public, allow only the App Service's outbound IP addresses and your admin machine. Don't turn on **Allow Azure services and resources to access this server**: it lets in every Azure customer's resources, not just your app.
- **Lock down who can reach the server.** If your users work from known networks, allow only those addresses with App Service access restrictions. If they're all in a few countries, a geo-filter on Azure Front Door's WAF, or Cloudflare, blocks the rest of the world before requests reach the app. Treat it as a noise filter, not a security control: a VPN gets around it, and it blocks your own users when they travel.
- **Ship logs and audit data off the server.** Send logs to Application Insights or Log Analytics. Remember that the audit log records the arguments, such as names searched for, so treat it as sensitive.
- **Use short-lived tokens.** The local static tokens never expire. Use Entra ID, so tokens expire and access can be revoked centrally.
