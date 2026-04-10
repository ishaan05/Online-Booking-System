using System.Security.Cryptography;

namespace OnlineBookingSystem.Api.Configuration;

/// <summary>
/// Ensures <c>Jwt:Key</c> is set to a strong secret without requiring manual env vars on first deploy.
/// The signing key must stay the same across restarts (and must not rotate on every request), or every
/// issued bearer token would become invalid immediately.
/// </summary>
public static class JwtKeyBootstrap
{
	private const string PlaceholderToken = "CHANGE_ME";
	private const string KeyFileName = "jwt-signing.key";

	public static void EnsureSigningKey(WebApplicationBuilder builder)
	{
		string? raw = builder.Configuration["Jwt:Key"];
		string trimmed = (raw ?? string.Empty).Trim();
		if (trimmed.Length >= 32 && !trimmed.Contains(PlaceholderToken, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		string dir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
		Directory.CreateDirectory(dir);
		string path = Path.Combine(dir, KeyFileName);

		if (File.Exists(path))
		{
			string fromFile = File.ReadAllText(path).Trim();
			if (fromFile.Length >= 32)
			{
				builder.Configuration["Jwt:Key"] = fromFile;
				return;
			}
		}

		// UTF-8 secret; JwtKeyMaterial hashes with SHA256 to 256-bit signing material.
		string generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
		File.WriteAllText(path, generated);
		builder.Configuration["Jwt:Key"] = generated;
	}
}
