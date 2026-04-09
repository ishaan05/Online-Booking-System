using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineBookingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Advertisement",
                columns: table => new
                {
                    AdID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AdImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Advertisement", x => x.AdID);
                });

            migrationBuilder.CreateTable(
                name: "BankAccountDetail",
                columns: table => new
                {
                    BankId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IFSCCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Place = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ChequeInFavour = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountDetail", x => x.BankId);
                });

            migrationBuilder.CreateTable(
                name: "BookingCategory",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IdentityLabel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IdentityFormat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingCategory", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "BookingPurpose",
                columns: table => new
                {
                    PurposeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurposeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MaxDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPurpose", x => x.PurposeID);
                });

            migrationBuilder.CreateTable(
                name: "ImageBanner",
                columns: table => new
                {
                    ImgId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImgPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImgURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageBanner", x => x.ImgId);
                });

            migrationBuilder.CreateTable(
                name: "OfficeUserRole",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeUserRole", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "OTPLog",
                columns: table => new
                {
                    OTPID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MobileNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    OTPCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTPLog", x => x.OTPID);
                });

            migrationBuilder.CreateTable(
                name: "RegisteredUser",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    UserAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredUser", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "SMSLog",
                columns: table => new
                {
                    SMSID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MobileNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMSLog", x => x.SMSID);
                });

            migrationBuilder.CreateTable(
                name: "SuperAdminProvisioningToken",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BoundIpFingerprint = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperAdminProvisioningToken", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "TermsAndConditions",
                columns: table => new
                {
                    TermID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TermText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermsAndConditions", x => x.TermID);
                });

            migrationBuilder.CreateTable(
                name: "TextAdvertisement",
                columns: table => new
                {
                    AdID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextAdvertisement", x => x.AdID);
                });

            migrationBuilder.CreateTable(
                name: "VenueType",
                columns: table => new
                {
                    VenueTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueType", x => x.VenueTypeID);
                });

            migrationBuilder.CreateTable(
                name: "WebsiteVisit",
                columns: table => new
                {
                    VisitID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitorToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    VisitedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteVisit", x => x.VisitID);
                });

            migrationBuilder.CreateTable(
                name: "OfficeUser",
                columns: table => new
                {
                    OfficeUserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    EmailID = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeUser", x => x.OfficeUserID);
                    table.ForeignKey(
                        name: "FK_OfficeUser_OfficeUserRole_RoleID",
                        column: x => x.RoleID,
                        principalTable: "OfficeUserRole",
                        principalColumn: "RoleID");
                });

            migrationBuilder.CreateTable(
                name: "VenueMaster",
                columns: table => new
                {
                    VenueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueTypeID = table.Column<int>(type: "int", nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VenueCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Division = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GoogleMapLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Facilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueMaster", x => x.VenueID);
                    table.ForeignKey(
                        name: "FK_VenueMaster_VenueType_VenueTypeID",
                        column: x => x.VenueTypeID,
                        principalTable: "VenueType",
                        principalColumn: "VenueTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingRequest",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingRegNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    VenueID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    PurposeID = table.Column<int>(type: "int", nullable: false),
                    BookingFromDate = table.Column<DateTime>(type: "date", nullable: false),
                    BookingToDate = table.Column<DateTime>(type: "date", nullable: false),
                    TotalDays = table.Column<int>(type: "int", nullable: false, computedColumnSql: "CAST((julianday(\"BookingToDate\") - julianday(\"BookingFromDate\")) AS INTEGER) + 1", stored: true),
                    IdentityNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RentAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false, computedColumnSql: "\"RentAmount\" + \"SecurityDeposit\"", stored: true),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IFSCCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TermsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    BookingStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Level1UserID = table.Column<int>(type: "int", nullable: true),
                    Level2UserID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRequest", x => x.BookingID);
                    table.ForeignKey(
                        name: "FK_BookingRequest_BookingCategory_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "BookingCategory",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRequest_BookingPurpose_PurposeID",
                        column: x => x.PurposeID,
                        principalTable: "BookingPurpose",
                        principalColumn: "PurposeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRequest_RegisteredUser_UserID",
                        column: x => x.UserID,
                        principalTable: "RegisteredUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRequest_VenueMaster_VenueID",
                        column: x => x.VenueID,
                        principalTable: "VenueMaster",
                        principalColumn: "VenueID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VenueEarningExpense",
                columns: table => new
                {
                    EntryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueID = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "date", nullable: false),
                    EnteredByID = table.Column<int>(type: "int", nullable: false),
                    IsFrozen = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueEarningExpense", x => x.EntryID);
                    table.ForeignKey(
                        name: "FK_VenueEarningExpense_OfficeUser_EnteredByID",
                        column: x => x.EnteredByID,
                        principalTable: "OfficeUser",
                        principalColumn: "OfficeUserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VenueEarningExpense_VenueMaster_VenueID",
                        column: x => x.VenueID,
                        principalTable: "VenueMaster",
                        principalColumn: "VenueID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VenueImage",
                columns: table => new
                {
                    ImageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueID = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueImage", x => x.ImageID);
                    table.ForeignKey(
                        name: "FK_VenueImage_VenueMaster_VenueID",
                        column: x => x.VenueID,
                        principalTable: "VenueMaster",
                        principalColumn: "VenueID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueRentRule",
                columns: table => new
                {
                    RuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    PurposeID = table.Column<int>(type: "int", nullable: false),
                    RentPerDay = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MaxDays = table.Column<int>(type: "int", nullable: false),
                    IsAllottable = table.Column<bool>(type: "bit", nullable: false),
                    NotAllottableReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueRentRule", x => x.RuleID);
                    table.ForeignKey(
                        name: "FK_VenueRentRule_BookingCategory_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "BookingCategory",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VenueRentRule_BookingPurpose_PurposeID",
                        column: x => x.PurposeID,
                        principalTable: "BookingPurpose",
                        principalColumn: "PurposeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VenueRentRule_VenueMaster_VenueID",
                        column: x => x.VenueID,
                        principalTable: "VenueMaster",
                        principalColumn: "VenueID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueUserMapping",
                columns: table => new
                {
                    MappingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueID = table.Column<int>(type: "int", nullable: false),
                    OfficeUserID = table.Column<int>(type: "int", nullable: false),
                    RoleLevel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueUserMapping", x => x.MappingID);
                    table.ForeignKey(
                        name: "FK_VenueUserMapping_OfficeUserRole_RoleLevel",
                        column: x => x.RoleLevel,
                        principalTable: "OfficeUserRole",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_VenueUserMapping_OfficeUser_OfficeUserID",
                        column: x => x.OfficeUserID,
                        principalTable: "OfficeUser",
                        principalColumn: "OfficeUserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VenueUserMapping_VenueMaster_VenueID",
                        column: x => x.VenueID,
                        principalTable: "VenueMaster",
                        principalColumn: "VenueID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingStatusLog",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    ChangedByType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangedByID = table.Column<int>(type: "int", nullable: true),
                    OldStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatusLog", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_BookingStatusLog_BookingRequest_BookingID",
                        column: x => x.BookingID,
                        principalTable: "BookingRequest",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinalSettlement",
                columns: table => new
                {
                    SettlementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    ElectricityCharges = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CleaningCharges = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DeductionRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SettlementStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreparedByID = table.Column<int>(type: "int", nullable: false),
                    ApprovedByID = table.Column<int>(type: "int", nullable: true),
                    PreparedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalSettlement", x => x.SettlementID);
                    table.ForeignKey(
                        name: "FK_FinalSettlement_BookingRequest_BookingID",
                        column: x => x.BookingID,
                        principalTable: "BookingRequest",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    TransactionRefNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GatewayResponse = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransaction", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_BookingRequest_BookingID",
                        column: x => x.BookingID,
                        principalTable: "BookingRequest",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VenueBlockedDate",
                columns: table => new
                {
                    BlockedID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueID = table.Column<int>(type: "int", nullable: false),
                    BlockedDate = table.Column<DateTime>(type: "date", nullable: false),
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueBlockedDate", x => x.BlockedID);
                    table.ForeignKey(
                        name: "FK_VenueBlockedDate_BookingRequest_BookingID",
                        column: x => x.BookingID,
                        principalTable: "BookingRequest",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VenueBlockedDate_VenueMaster_VenueID",
                        column: x => x.VenueID,
                        principalTable: "VenueMaster",
                        principalColumn: "VenueID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountDetail_AccountNumber",
                table: "BankAccountDetail",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequest_BookingRegNo",
                table: "BookingRequest",
                column: "BookingRegNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequest_CategoryID",
                table: "BookingRequest",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequest_PurposeID",
                table: "BookingRequest",
                column: "PurposeID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequest_UserID",
                table: "BookingRequest",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequest_VenueID",
                table: "BookingRequest",
                column: "VenueID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusLog_BookingID",
                table: "BookingStatusLog",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_FinalSettlement_BookingID",
                table: "FinalSettlement",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeUser_RoleID",
                table: "OfficeUser",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeUser_Username",
                table: "OfficeUser",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_BookingID",
                table: "PaymentTransaction",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_TransactionRefNo",
                table: "PaymentTransaction",
                column: "TransactionRefNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredUser_Email",
                table: "RegisteredUser",
                column: "Email",
                unique: true,
                filter: "Email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredUser_MobileNumber",
                table: "RegisteredUser",
                column: "MobileNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuperAdminProvisioningToken_TokenHash",
                table: "SuperAdminProvisioningToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenueBlockedDate_BookingID",
                table: "VenueBlockedDate",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueBlockedDate_VenueID_BlockedDate",
                table: "VenueBlockedDate",
                columns: new[] { "VenueID", "BlockedDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenueEarningExpense_EnteredByID",
                table: "VenueEarningExpense",
                column: "EnteredByID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueEarningExpense_VenueID",
                table: "VenueEarningExpense",
                column: "VenueID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueImage_VenueID",
                table: "VenueImage",
                column: "VenueID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueMaster_VenueTypeID",
                table: "VenueMaster",
                column: "VenueTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueRentRule_CategoryID",
                table: "VenueRentRule",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueRentRule_PurposeID",
                table: "VenueRentRule",
                column: "PurposeID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueRentRule_VenueID",
                table: "VenueRentRule",
                column: "VenueID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueUserMapping_OfficeUserID",
                table: "VenueUserMapping",
                column: "OfficeUserID");

            migrationBuilder.CreateIndex(
                name: "IX_VenueUserMapping_RoleLevel",
                table: "VenueUserMapping",
                column: "RoleLevel");

            migrationBuilder.CreateIndex(
                name: "IX_VenueUserMapping_VenueID_OfficeUserID_RoleLevel",
                table: "VenueUserMapping",
                columns: new[] { "VenueID", "OfficeUserID", "RoleLevel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Advertisement");

            migrationBuilder.DropTable(
                name: "BankAccountDetail");

            migrationBuilder.DropTable(
                name: "BookingStatusLog");

            migrationBuilder.DropTable(
                name: "FinalSettlement");

            migrationBuilder.DropTable(
                name: "ImageBanner");

            migrationBuilder.DropTable(
                name: "OTPLog");

            migrationBuilder.DropTable(
                name: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "SMSLog");

            migrationBuilder.DropTable(
                name: "SuperAdminProvisioningToken");

            migrationBuilder.DropTable(
                name: "TermsAndConditions");

            migrationBuilder.DropTable(
                name: "TextAdvertisement");

            migrationBuilder.DropTable(
                name: "VenueBlockedDate");

            migrationBuilder.DropTable(
                name: "VenueEarningExpense");

            migrationBuilder.DropTable(
                name: "VenueImage");

            migrationBuilder.DropTable(
                name: "VenueRentRule");

            migrationBuilder.DropTable(
                name: "VenueUserMapping");

            migrationBuilder.DropTable(
                name: "WebsiteVisit");

            migrationBuilder.DropTable(
                name: "BookingRequest");

            migrationBuilder.DropTable(
                name: "OfficeUser");

            migrationBuilder.DropTable(
                name: "BookingCategory");

            migrationBuilder.DropTable(
                name: "BookingPurpose");

            migrationBuilder.DropTable(
                name: "RegisteredUser");

            migrationBuilder.DropTable(
                name: "VenueMaster");

            migrationBuilder.DropTable(
                name: "OfficeUserRole");

            migrationBuilder.DropTable(
                name: "VenueType");
        }
    }
}
