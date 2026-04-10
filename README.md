# Online-Booking-System

**Database:** Microsoft **SQL Server** only (local, Azure SQL Database, or SQL Server on a VM). See **[HOSTING.md](HOSTING.md)** for deployment and **[deploy/azure-sql-free-tier.md](deploy/azure-sql-free-tier.md)** for free Azure SQL.

**Windows Server (own machine):** step-by-step IIS + SQL + env vars → **[docs/WINDOWS-SERVER-DEPLOYMENT.md](docs/WINDOWS-SERVER-DEPLOYMENT.md)**.  
**Source backup (no `node_modules` / `dist` / build outputs):** `powershell -ExecutionPolicy Bypass -File scripts/backup-project-source.ps1`  
**IIS-ready publish folder:** `powershell -ExecutionPolicy Bypass -File scripts/create-windows-server-package.ps1` (output under `artifacts/`, not committed to Git).

**JWT signing key:** If `Jwt__Key` is unset or still the `CHANGE_ME` placeholder, the API generates a secret once and stores it in `App_Data/jwt-signing.key` (stable across restarts; do not rotate per request). See `docs/WINDOWS-SERVER-DEPLOYMENT.md`.