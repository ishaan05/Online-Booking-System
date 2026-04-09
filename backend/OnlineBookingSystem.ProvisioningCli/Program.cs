using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OnlineBookingSystem.Shared.Data;
using OnlineBookingSystem.Shared.Models;
using OnlineBookingSystem.Shared.Security;

static void PrintUsage()
{
	Console.WriteLine("Mint a one-time Super Admin provisioning token (stored as SHA-256 only).");
	Console.WriteLine();
	Console.WriteLine("  dotnet run --project OnlineBookingSystem.ProvisioningCli -- mint [--ttl <minutes>] [--bind-ip <address>]");
	Console.WriteLine();
	Console.WriteLine("Connection string: uses OnlineBookingSystem.Api/appsettings.json next to this solution, or env ConnectionStrings__DefaultConnection.");
	Console.WriteLine("  --ttl        Token lifetime in minutes (default 20, min 5, max 120).");
	Console.WriteLine("  --bind-ip    Optional: only the given client IP may redeem the token (use with reverse-proxy awareness).");
}

if (args.Length == 0 || !string.Equals(args[0], "mint", StringComparison.OrdinalIgnoreCase))
{
	PrintUsage();
	return 1;
}

int ttlMinutes = 20;
string? bindIp = null;
for (int i = 1; i < args.Length; i++)
{
	if (string.Equals(args[i], "--ttl", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
	{
		_ = int.TryParse(args[++i], out ttlMinutes);
		continue;
	}

	if (string.Equals(args[i], "--bind-ip", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
	{
		bindIp = args[++i];
		continue;
	}
}

ttlMinutes = Math.Clamp(ttlMinutes, 5, 120);

string apiConfigDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "OnlineBookingSystem.Api"));
IConfigurationRoot cfg = new ConfigurationBuilder()
	.SetBasePath(apiConfigDir)
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
	.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
	.AddEnvironmentVariables()
	.Build();

string? cs = cfg.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cs))
{
	cs = $"Data Source={Path.Combine(apiConfigDir, "app.db")}";
}

DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
	.UseSqlite(cs)
	.Options;

await using AppDbContext db = new AppDbContext(options);
ProvisioningSchemaGuard.EnsureSuperAdminProvisioningToken(db);

string plaintext = ProvisioningCrypto.GenerateProvisioningToken();
byte[] hash = ProvisioningCrypto.HashToken(plaintext);
byte[]? ipFp = string.IsNullOrWhiteSpace(bindIp) ? null : ProvisioningCrypto.HashIpFingerprint(bindIp.Trim());
DateTime created = DateTime.UtcNow;
DateTime expires = created.AddMinutes(ttlMinutes);

db.SuperAdminProvisioningTokens.Add(new SuperAdminProvisioningTokenEntity
{
	TokenHash = hash,
	CreatedAtUtc = created,
	ExpiresAtUtc = expires,
	UsedAtUtc = null,
	BoundIpFingerprint = ipFp,
});

await db.SaveChangesAsync();

Console.WriteLine();
Console.WriteLine("Copy this token now. It is not stored in plain text and cannot be shown again.");
Console.WriteLine();
Console.WriteLine(plaintext);
Console.WriteLine();
Console.WriteLine($"Expires (UTC): {expires:O}");
if (!string.IsNullOrWhiteSpace(bindIp))
{
	Console.WriteLine($"IP binding:    {bindIp.Trim()}");
}

Console.WriteLine();
Console.WriteLine("Send it in header X-Provisioning-Token when calling POST /api/SystemProvisioning/bootstrap-super-admin");
Console.WriteLine("Alternatively, use POST /api/SystemProvisioning/mint-token with X-Provisioning-Mint-Key if configured.");
return 0;
