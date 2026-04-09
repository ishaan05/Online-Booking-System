using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineBookingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class VenueMasterPhysicalColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: databases may already have these columns from manual ALTER.
            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.VenueMaster', N'Capacity') IS NULL
    ALTER TABLE dbo.VenueMaster ADD Capacity INT NULL;
IF COL_LENGTH(N'dbo.VenueMaster', N'AreaSqMt') IS NULL
    ALTER TABLE dbo.VenueMaster ADD AreaSqMt DECIMAL(10,2) NULL;
IF COL_LENGTH(N'dbo.VenueMaster', N'NoOfRooms') IS NULL
    ALTER TABLE dbo.VenueMaster ADD NoOfRooms INT NULL;
IF COL_LENGTH(N'dbo.VenueMaster', N'NoOfKitchen') IS NULL
    ALTER TABLE dbo.VenueMaster ADD NoOfKitchen INT NULL;
IF COL_LENGTH(N'dbo.VenueMaster', N'NoOfToilet') IS NULL
    ALTER TABLE dbo.VenueMaster ADD NoOfToilet INT NULL;
IF COL_LENGTH(N'dbo.VenueMaster', N'NoOfBathroom') IS NULL
    ALTER TABLE dbo.VenueMaster ADD NoOfBathroom INT NULL;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.VenueMaster', N'NoOfBathroom') IS NOT NULL
    ALTER TABLE dbo.VenueMaster DROP COLUMN NoOfBathroom;
IF COL_LENGTH(N'dbo.VenueMaster', N'NoOfToilet') IS NOT NULL
    ALTER TABLE dbo.VenueMaster DROP COLUMN NoOfToilet;
IF COL_LENGTH(N'dbo.VenueMaster', N'NoOfKitchen') IS NOT NULL
    ALTER TABLE dbo.VenueMaster DROP COLUMN NoOfKitchen;
IF COL_LENGTH(N'dbo.VenueMaster', N'NoOfRooms') IS NOT NULL
    ALTER TABLE dbo.VenueMaster DROP COLUMN NoOfRooms;
IF COL_LENGTH(N'dbo.VenueMaster', N'AreaSqMt') IS NOT NULL
    ALTER TABLE dbo.VenueMaster DROP COLUMN AreaSqMt;
IF COL_LENGTH(N'dbo.VenueMaster', N'Capacity') IS NOT NULL
    ALTER TABLE dbo.VenueMaster DROP COLUMN Capacity;
""");
        }
    }
}
