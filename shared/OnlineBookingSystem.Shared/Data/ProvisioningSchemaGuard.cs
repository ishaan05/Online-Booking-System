using Microsoft.EntityFrameworkCore;

namespace OnlineBookingSystem.Shared.Data;

/// <summary>Ensures <see cref="Models.SuperAdminProvisioningTokenEntity"/> table exists for secure first Super Admin flow.</summary>
public static class ProvisioningSchemaGuard
{
	public static void EnsureSuperAdminProvisioningToken(AppDbContext db)
	{
		// Schema is created by EF migrations in the API project when using SQLite.
		if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
		{
			return;
		}

		db.Database.ExecuteSqlRaw(Sql);
	}

	private const string Sql = """
IF OBJECT_ID(N'dbo.SuperAdminProvisioningToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SuperAdminProvisioningToken (
        TokenId              INT              NOT NULL CONSTRAINT PK_SuperAdminProvisioningToken PRIMARY KEY IDENTITY(1,1),
        TokenHash            VARBINARY(32)    NOT NULL,
        CreatedAtUtc         DATETIME2        NOT NULL,
        ExpiresAtUtc         DATETIME2        NOT NULL,
        UsedAtUtc            DATETIME2        NULL,
        BoundIpFingerprint   VARBINARY(32)    NULL,
        CONSTRAINT UQ_SuperAdminProvisioningToken_Hash UNIQUE (TokenHash)
    );
    CREATE NONCLUSTERED INDEX IX_SuperAdminProvisioningToken_Expiry
        ON dbo.SuperAdminProvisioningToken (ExpiresAtUtc)
        WHERE UsedAtUtc IS NULL;
END
""";
}
