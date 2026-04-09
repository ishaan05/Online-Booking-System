using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OnlineBookingSystem.Shared.Data;

/// <summary>
/// Keeps <c>VenueMaster</c> writable when the table has extra columns (added manually) that are NOT NULL
/// without defaults — EF only inserts mapped columns, so SQL Server rejects the row. Also ensures at least
/// one <c>VenueType</c> row exists for FK from <c>VenueMaster.VenueTypeID</c>.
/// </summary>
public static class VenueMasterWriteGuard
{
	private static readonly HashSet<string> CoreColumnsKeepNotNull = new(StringComparer.OrdinalIgnoreCase)
	{
		"VenueID", "VenueTypeID", "VenueName", "VenueCode", "Address", "City", "Division", "IsActive", "CreatedAt"
	};

	public static void EnsureSqlServerVenueWritesWork(AppDbContext db, ILogger? log = null)
	{
		if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
		{
			return;
		}

		db.Database.OpenConnection();
		try
		{
			DbConnection conn = db.Database.GetDbConnection();
			using DbCommand seed = conn.CreateCommand();
			seed.CommandText = """
				IF NOT EXISTS (SELECT 1 FROM dbo.VenueType)
					INSERT INTO dbo.VenueType (TypeName, IsActive) VALUES (N'Community Hall', 1);
				""";
			seed.ExecuteNonQuery();

			using DbCommand list = conn.CreateCommand();
			list.CommandText = """
				SELECT c.name, t.name, c.max_length, c.precision, c.scale
				FROM sys.columns c
				INNER JOIN sys.tables tb ON c.object_id = tb.object_id
				INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
				LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
				WHERE SCHEMA_NAME(tb.schema_id) = N'dbo' AND tb.name = N'VenueMaster'
				  AND c.is_identity = 0 AND c.is_computed = 0 AND c.is_nullable = 0
				  AND dc.object_id IS NULL;
				""";

			var toFix = new List<(string Name, string TypeDef)>();
			using (DbDataReader r = list.ExecuteReader())
			{
				while (r.Read())
				{
					string colName = r.GetString(0);
					if (CoreColumnsKeepNotNull.Contains(colName))
					{
						continue;
					}

					string typeName = r.GetString(1);
					short maxLen = r.IsDBNull(2) ? (short)0 : r.GetInt16(2);
					byte prec = r.IsDBNull(3) ? (byte)0 : r.GetByte(3);
					byte scale = r.IsDBNull(4) ? (byte)0 : r.GetByte(4);
					string typeDef = BuildSqlTypeDefinition(typeName, maxLen, prec, scale);
					toFix.Add((colName, typeDef));
				}
			}

			foreach ((string name, string typeDef) in toFix)
			{
				string safeName = name.Replace("]", "]]", StringComparison.Ordinal);
				string alter = $"ALTER TABLE dbo.VenueMaster ALTER COLUMN [{safeName}] {typeDef} NULL;";
				try
				{
					using DbCommand alt = conn.CreateCommand();
					alt.CommandText = alter;
					alt.ExecuteNonQuery();
					log?.LogInformation("VenueMaster: column {Column} altered to NULL (extra column; app does not set it on insert).", name);
				}
				catch (Exception ex)
				{
					log?.LogWarning(ex,
						"VenueMaster: could not ALTER COLUMN {Column} to NULL. If saves still fail, make the column nullable or add a DEFAULT in SQL Server.",
						name);
				}
			}
		}
		finally
		{
			db.Database.CloseConnection();
		}
	}

	private static string BuildSqlTypeDefinition(string typeName, short maxLength, byte precision, byte scale)
	{
		string t = typeName.ToLowerInvariant();
		return t switch
		{
			"nvarchar" or "varchar" or "nchar" or "char" => maxLength < 0
				? $"{t}(max)"
				: $"{t}({(t is "nvarchar" or "nchar" ? maxLength / 2 : maxLength)})",
			"varbinary" or "binary" => maxLength < 0
				? $"{t}(max)"
				: $"{t}({maxLength})",
			"decimal" or "numeric" => $"{t}({precision},{scale})",
			"datetime2" or "time" or "datetimeoffset" => scale > 0 ? $"{t}({scale})" : t,
			_ => t
		};
	}
}
