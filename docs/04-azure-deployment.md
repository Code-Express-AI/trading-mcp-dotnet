# 4. Optional: deploy to Azure

This guide outlines the Azure path: host the database in Azure SQL, deploy the MCP server to Azure App Service, and replace local bearer tokens with Microsoft Entra ID authentication. The [Entra setup guide](entra-setup.md) has the detailed app-registration and token steps.

## Create and prepare Azure SQL

1. Create an Azure SQL logical server and an empty Azure SQL Database. Configure a firewall rule that allows your development machine to connect while you set it up.
2. Connect to the new database as its administrator and run `sql/01_schema.sql`, then `sql/02_security.sql`. In `02_security.sql`, replace the password placeholder with a strong password for the contained `mcp_reader` user.
3. Generate the synthetic data from your development machine, connecting `DataGenerator` as a database administrator. The machine's IP must be allowed by the SQL firewall:
   ```sh
   cd DataGenerator
   dotnet run -- "Server=tcp:<server>.database.windows.net,1433;Database=<database>;User ID=<admin>;Password=<admin-password>;Encrypt=True;TrustServerCertificate=False" --reset
   cd ..
   ```
4. Run `sql/03_procedures.sql` as an administrator connected to the Azure SQL database.

Use the same `mcp_reader` password in the server's database connection string. Do not use `TrustServerCertificate=True` with Azure SQL.

## Deploy the MCP server

Create a Linux App Service using the .NET 10 runtime, then publish the server:

```sh
dotnet publish McpServer/McpServer.csproj --configuration Release --output ./publish
```

Deploy the published output using your preferred App Service deployment method, such as the VS Code Azure App Service extension or a CI/CD pipeline. Configure the App Service settings under **Configuration**:

| Setting | Value |
|---------|-------|
| `Auth__Mode` | `Entra` |
| `Entra__TenantId` | Your Entra tenant ID |
| `Entra__ClientId` | The client ID of the server API registration |
| `ConnectionStrings__Trading` | Azure SQL connection string for `mcp_reader` |

Treat the database connection string as a secret. For anything beyond a short-lived demo, store it in Azure Key Vault and use a Key Vault reference from App Service configuration. Allow the App Service to reach Azure SQL, preferably using private networking for production; avoid leaving broad SQL firewall access enabled.

## Configure Entra ID and test

Follow [Entra ID authentication for the MCP server](entra-setup.md) to register the API, expose the `mcp.read` scope, configure a client, and obtain a token. The server validates Entra-issued tokens; it does not issue tokens itself.

After deployment:

- Check `https://<app-name>.azurewebsites.net/health`.
- Use an MCP client that can send an `Authorization: Bearer <token>` header to connect to `https://<app-name>.azurewebsites.net/mcp`.
- Try a read-only tool call and verify the caller appears in the database audit log.

Test your chosen interactive client before relying on it: Entra's OAuth registration flow is not supported by every MCP client. See the limitations and troubleshooting in the [Entra guide](entra-setup.md).
