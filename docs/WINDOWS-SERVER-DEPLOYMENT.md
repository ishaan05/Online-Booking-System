# Deploy on your own Windows Server (IIS + SQL Server)

This project is an **ASP.NET Core 8** API with an **Angular** SPA. You can host both on a Windows Server you already run 24/7, instead of a paid PaaS, as long as you install the right components and set **secrets via environment variables** on the server (never commit production secrets to Git).

---

## What you need on the server

| Component | Purpose |
|-------------|---------|
| **Windows Server** (2019/2022 or Windows 10/11 “server” role) | Hosts IIS and optionally SQL |
| **SQL Server** | Express or Standard — holds `CommunityHallBookingDB` (or your DB name) |
| **.NET 8 ASP.NET Core Hosting Bundle** | Lets IIS host the API (`dotnet` runtime + ANCM) |
| **IIS** with **CGI** / **ASP.NET** features | Web server |
| **URL Rewrite** (IIS extension, optional but useful) | SPA fallback to `index.html` |

Your **local/dev** workflow is unchanged if you only run the backup/package scripts as documented; they clean `wwwroot` after packaging so `ng serve` + API still work.

---

## Part A — Database on the same (or another) machine

1. **Install SQL Server** (Express is fine for many offices) and **SQL Server Management Studio (SSMS)** if you like GUI.
2. **Create or restore** your database (same schema you use today — scripts under `backend/` and `backend/OnlineBookingSystem.Api/Database/` if you use them).
3. **Create a SQL login** (optional) or use **Windows Authentication** for the app pool identity (advanced). Simplest for a first deploy: **SQL auth** user with `db_owner` on your DB (or least privilege you already use).
4. **Test connection** from SSMS on the server: `Server=.\SQLEXPRESS;Database=YourDb;User Id=...;Password=...;TrustServerCertificate=True` (adjust instance name).
5. **Firewall**: if SQL is only used on the same machine, you do **not** need to open port 1433 to the internet. If the API runs on another server, allow **1433** (or your custom port) **only** from that server’s IP.

---

## Part B — Build the deploy package (on your dev PC or CI)

From the **repository root** (use **Windows PowerShell** or **PowerShell 7** — `pwsh` only if you installed PowerShell 7):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\create-windows-server-package.ps1
```

This will:

1. Run `npm run build --configuration production` in `frontend/`.
2. Copy the built SPA into `backend/OnlineBookingSystem.Api/wwwroot` **temporarily**.
3. Run `dotnet publish -c Release` into `artifacts\windows-server-<date>\Api\`.
4. **Remove** the SPA files from `wwwroot` again (keeps `.gitkeep`) so your **localhost** setup is not left with a stale production build in the repo.

Copy the entire folder **`artifacts\windows-server-<date>\Api\`** to the server (USB, RDP copy, network share, zip).

---

## Part C — Install on Windows Server (IIS)

### 1. Install Hosting Bundle

Download **ASP.NET Core 8.0 Runtime – Windows Hosting Bundle** from Microsoft, install, **reboot** (or at least restart IIS: `iisreset`).

### 2. Create folders

Example:

- `C:\inetpub\CommunityHallBooking\Api\` ← contents of the published `Api` folder (all files from `artifacts\...\Api\`).

### 3. Application pool

- IIS Manager → **Application Pools** → **Add Application Pool**
- Name: `CommunityHallBooking`
- **.NET CLR version**: **No Managed Code**
- **Enable 32-Bit Applications**: **False**

### 4. Site

- **Sites** → **Add Website**
- **Physical path**: `C:\inetpub\CommunityHallBooking\Api`
- **Binding**: e.g. `https`, hostname `booking.yourdomain.com` (or IP + port for testing)
- **Application pool**: `CommunityHallBooking`

### 5. Environment variables (production secrets)

Set for the **application pool** or **site** (Configuration Editor / `web.config` / machine environment — pick one consistent approach):

| Variable | Example / note |
|----------|------------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Your SQL connection string (same as `appsettings` but **on the server only**) |
| `Jwt__Key` | Long random string, **32+ characters**, not `CHANGE_ME` |
| `Jwt__Issuer` / `Jwt__Audience` | Match what your Angular `environment.prod.ts` expects if you validate issuer/audience |
| `Cors__AllowedOrigins__0` | Your public site URL, e.g. `https://booking.yourdomain.com` (ASP.NET Core binds array from numbered env vars) |
| `Provisioning__MintKey` | Strong secret for provisioning mint if you use that feature |

After changing env vars, recycle the app pool.

### 6. SPA deep links (Angular routes)

If users open `https://yoursite/admin/...` directly, IIS must serve **`index.html`** for unknown paths. Options:

- Install **IIS URL Rewrite** and add a rule: non-file requests → `/index.html`, **or**
- Use **same-site** deployment where the API already serves `index.html` from `wwwroot` (your publish folder includes the Angular build — it does).

The published output already contains `index.html` and static assets; ensure **default document** includes `index.html` and **rewrite** rules match your Angular base `href` (if you use a subpath, set `--base-href` at build time — default is `/`).

### 7. HTTPS

Use a real certificate (internal CA, commercial, or Let’s Encrypt with a tool compatible with IIS). Bind it to the site on **443**.

### 8. Test

- Browse to `https://your-site/swagger` only if you enabled Swagger in production via config (default is dev-only).
- Hit a known API route from the browser network tab after login.

---

## Part D — Full source backup (no `node_modules`, etc.)

From repo root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\backup-project-source.ps1
```

Default: creates a folder next to the repo, e.g. `D:\OnlineHallBooking-source-backup-20260409-143022` with **everything except** excluded directories. Restore elsewhere with `npm ci` in `frontend` and `dotnet restore` on the solution.

Options:

- `-ExcludeGit` — smaller backup without `.git`
- `-Zip` — also creates `.zip` (can be large)

---

## Safety checklist (nothing breaks)

- **Localhost**: After `create-windows-server-package.ps1`, `wwwroot` is cleared except `.gitkeep`; run API + `ng serve` as before.
- **Production**: Never rely on `appsettings.json` in Git for production secrets; use **environment variables** on the server.
- **EF migrations**: This API is configured to run **`Migrate()` only in Development**; production uses your existing SQL schema — no duplicate-object errors from EF on startup.

---

## If something fails

- **500.30 / ANCM**: Hosting Bundle not installed or wrong bitness — reinstall Hosting Bundle, restart IIS.
- **502.5**: App crashed on startup — check **Event Viewer → Windows Logs → Application** and stdout logs in `logs` if enabled.
- **CORS**: Browser blocks API — set `Cors:AllowedOrigins` to the **exact** origin (scheme + host + port) the browser uses.

For changes to scripts or this doc, use Agent mode in the IDE or edit the files under `scripts/` and `docs/`.
