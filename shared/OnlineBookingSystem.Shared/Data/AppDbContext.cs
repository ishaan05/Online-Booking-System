using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineBookingSystem.Shared.Models;

namespace OnlineBookingSystem.Shared.Data;

public class AppDbContext : DbContext
{
	public DbSet<VenueTypeEntity> VenueTypes => ((DbContext)this).Set<VenueTypeEntity>();

	public DbSet<VenueMasterEntity> VenueMasters => ((DbContext)this).Set<VenueMasterEntity>();

	public DbSet<VenueImageEntity> VenueImages => ((DbContext)this).Set<VenueImageEntity>();

	public DbSet<VenueRentRuleEntity> VenueRentRules => ((DbContext)this).Set<VenueRentRuleEntity>();

	public DbSet<BookingCategoryEntity> BookingCategories => ((DbContext)this).Set<BookingCategoryEntity>();

	public DbSet<BookingPurposeEntity> BookingPurposes => ((DbContext)this).Set<BookingPurposeEntity>();

	public DbSet<RegisteredUserEntity> RegisteredUsers => ((DbContext)this).Set<RegisteredUserEntity>();

	public DbSet<OtpLogEntity> OtpLogs => ((DbContext)this).Set<OtpLogEntity>();

	public DbSet<BookingRequestEntity> BookingRequests => ((DbContext)this).Set<BookingRequestEntity>();

	public DbSet<BookingStatusLogEntity> BookingStatusLogs => ((DbContext)this).Set<BookingStatusLogEntity>();

	public DbSet<PaymentTransactionEntity> PaymentTransactions => ((DbContext)this).Set<PaymentTransactionEntity>();

	public DbSet<OfficeUserEntity> OfficeUsers => ((DbContext)this).Set<OfficeUserEntity>();

	public DbSet<OfficeUserRoleEntity> OfficeUserRoles => ((DbContext)this).Set<OfficeUserRoleEntity>();

	public DbSet<VenueUserMappingEntity> VenueUserMappings => ((DbContext)this).Set<VenueUserMappingEntity>();

	public DbSet<VenueBlockedDateEntity> VenueBlockedDates => ((DbContext)this).Set<VenueBlockedDateEntity>();

	public DbSet<VenueEarningExpenseEntity> VenueEarningExpenses => ((DbContext)this).Set<VenueEarningExpenseEntity>();

	public DbSet<FinalSettlementEntity> FinalSettlements => ((DbContext)this).Set<FinalSettlementEntity>();

	public DbSet<SmsLogEntity> SmsLogs => ((DbContext)this).Set<SmsLogEntity>();

	public DbSet<AdvertisementEntity> Advertisements => ((DbContext)this).Set<AdvertisementEntity>();

	public DbSet<TextAdvertisementEntity> TextAdvertisements => ((DbContext)this).Set<TextAdvertisementEntity>();

	public DbSet<ImageBannerEntity> ImageBanners => ((DbContext)this).Set<ImageBannerEntity>();

	public DbSet<TermsAndConditionsEntity> TermsAndConditions => ((DbContext)this).Set<TermsAndConditionsEntity>();

	public DbSet<BankAccountDetailEntity> BankAccountDetails => ((DbContext)this).Set<BankAccountDetailEntity>();

	public DbSet<WebsiteVisitEntity> WebsiteVisits => ((DbContext)this).Set<WebsiteVisitEntity>();

	public DbSet<SuperAdminProvisioningTokenEntity> SuperAdminProvisioningTokens => ((DbContext)this).Set<SuperAdminProvisioningTokenEntity>();

	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base((DbContextOptions)(object)options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<VenueTypeEntity>((Action<EntityTypeBuilder<VenueTypeEntity>>)delegate(EntityTypeBuilder<VenueTypeEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<VenueTypeEntity>(e, "VenueType");
			e.HasKey((Expression<Func<VenueTypeEntity, object>>)((VenueTypeEntity x) => x.VenueTypeID));
		});
		modelBuilder.Entity<VenueMasterEntity>((Action<EntityTypeBuilder<VenueMasterEntity>>)delegate(EntityTypeBuilder<VenueMasterEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<VenueMasterEntity>(e, "VenueMaster");
			e.HasKey((Expression<Func<VenueMasterEntity, object>>)((VenueMasterEntity x) => x.VenueID));
			e.HasOne<VenueTypeEntity>((Expression<Func<VenueMasterEntity, VenueTypeEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueMasterEntity, object>>)((VenueMasterEntity x) => x.VenueTypeID))
				.OnDelete((DeleteBehavior)1);
		});
		modelBuilder.Entity<VenueImageEntity>((Action<EntityTypeBuilder<VenueImageEntity>>)delegate(EntityTypeBuilder<VenueImageEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<VenueImageEntity>(e, "VenueImage");
			e.HasKey((Expression<Func<VenueImageEntity, object>>)((VenueImageEntity x) => x.ImageID));
			e.HasOne<VenueMasterEntity>((Expression<Func<VenueImageEntity, VenueMasterEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueImageEntity, object>>)((VenueImageEntity x) => x.VenueID))
				.OnDelete((DeleteBehavior)3);
		});
		modelBuilder.Entity<VenueRentRuleEntity>((Action<EntityTypeBuilder<VenueRentRuleEntity>>)delegate(EntityTypeBuilder<VenueRentRuleEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<VenueRentRuleEntity>(e, "VenueRentRule");
			e.HasKey((Expression<Func<VenueRentRuleEntity, object>>)((VenueRentRuleEntity x) => x.RuleID));
			e.HasOne<VenueMasterEntity>((Expression<Func<VenueRentRuleEntity, VenueMasterEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueRentRuleEntity, object>>)((VenueRentRuleEntity x) => x.VenueID))
				.OnDelete((DeleteBehavior)3);
			e.HasOne<BookingCategoryEntity>((Expression<Func<VenueRentRuleEntity, BookingCategoryEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueRentRuleEntity, object>>)((VenueRentRuleEntity x) => x.CategoryID))
				.OnDelete((DeleteBehavior)1);
			e.HasOne<BookingPurposeEntity>((Expression<Func<VenueRentRuleEntity, BookingPurposeEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueRentRuleEntity, object>>)((VenueRentRuleEntity x) => x.PurposeID))
				.OnDelete((DeleteBehavior)1);
		});
		modelBuilder.Entity<BookingCategoryEntity>((Action<EntityTypeBuilder<BookingCategoryEntity>>)delegate(EntityTypeBuilder<BookingCategoryEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<BookingCategoryEntity>(e, "BookingCategory");
			e.HasKey((Expression<Func<BookingCategoryEntity, object>>)((BookingCategoryEntity x) => x.CategoryID));
		});
		modelBuilder.Entity<BookingPurposeEntity>((Action<EntityTypeBuilder<BookingPurposeEntity>>)delegate(EntityTypeBuilder<BookingPurposeEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<BookingPurposeEntity>(e, "BookingPurpose");
			e.HasKey((Expression<Func<BookingPurposeEntity, object>>)((BookingPurposeEntity x) => x.PurposeID));
		});
		modelBuilder.Entity<RegisteredUserEntity>((Action<EntityTypeBuilder<RegisteredUserEntity>>)delegate(EntityTypeBuilder<RegisteredUserEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<RegisteredUserEntity>(e, "RegisteredUser");
			e.HasKey((Expression<Func<RegisteredUserEntity, object>>)((RegisteredUserEntity x) => x.UserID));
			e.Property((Expression<Func<RegisteredUserEntity, int>>)((RegisteredUserEntity x) => x.UserID)).ValueGeneratedOnAdd();
			e.HasIndex((Expression<Func<RegisteredUserEntity, object>>)((RegisteredUserEntity x) => x.MobileNumber)).IsUnique(true);
			RelationalIndexBuilderExtensions.HasFilter<RegisteredUserEntity>(e.HasIndex((Expression<Func<RegisteredUserEntity, object>>)((RegisteredUserEntity x) => x.Email)).IsUnique(true), "Email IS NOT NULL");
		});
		modelBuilder.Entity<OtpLogEntity>((Action<EntityTypeBuilder<OtpLogEntity>>)delegate(EntityTypeBuilder<OtpLogEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<OtpLogEntity>(e, "OTPLog");
			e.HasKey((Expression<Func<OtpLogEntity, object>>)((OtpLogEntity x) => x.OTPID));
		});
		modelBuilder.Entity<BookingRequestEntity>((Action<EntityTypeBuilder<BookingRequestEntity>>)delegate(EntityTypeBuilder<BookingRequestEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<BookingRequestEntity>(e, "BookingRequest");
			e.HasKey((Expression<Func<BookingRequestEntity, object>>)((BookingRequestEntity x) => x.BookingID));
			e.HasIndex((Expression<Func<BookingRequestEntity, object>>)((BookingRequestEntity x) => x.BookingRegNo)).IsUnique(true);
			e.HasOne<RegisteredUserEntity>((Expression<Func<BookingRequestEntity, RegisteredUserEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<BookingRequestEntity, object>>)((BookingRequestEntity x) => x.UserID))
				.OnDelete((DeleteBehavior)1);
			e.HasOne<VenueMasterEntity>((Expression<Func<BookingRequestEntity, VenueMasterEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<BookingRequestEntity, object>>)((BookingRequestEntity x) => x.VenueID))
				.OnDelete((DeleteBehavior)1);
			e.HasOne<BookingCategoryEntity>((Expression<Func<BookingRequestEntity, BookingCategoryEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<BookingRequestEntity, object>>)((BookingRequestEntity x) => x.CategoryID))
				.OnDelete((DeleteBehavior)1);
			e.HasOne<BookingPurposeEntity>((Expression<Func<BookingRequestEntity, BookingPurposeEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<BookingRequestEntity, object>>)((BookingRequestEntity x) => x.PurposeID))
				.OnDelete((DeleteBehavior)1);
			RelationalPropertyBuilderExtensions.HasComputedColumnSql<int>(e.Property<int>((Expression<Func<BookingRequestEntity, int>>)((BookingRequestEntity x) => x.TotalDays)), "CAST((julianday(\"BookingToDate\") - julianday(\"BookingFromDate\")) AS INTEGER) + 1", (bool?)true);
			RelationalPropertyBuilderExtensions.HasComputedColumnSql<decimal>(e.Property<decimal>((Expression<Func<BookingRequestEntity, decimal>>)((BookingRequestEntity x) => x.TotalAmount)), "\"RentAmount\" + \"SecurityDeposit\"", (bool?)true);
		});
		modelBuilder.Entity<BookingStatusLogEntity>((Action<EntityTypeBuilder<BookingStatusLogEntity>>)delegate(EntityTypeBuilder<BookingStatusLogEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<BookingStatusLogEntity>(e, "BookingStatusLog");
			e.HasKey((Expression<Func<BookingStatusLogEntity, object>>)((BookingStatusLogEntity x) => x.LogID));
			e.HasOne<BookingRequestEntity>((Expression<Func<BookingStatusLogEntity, BookingRequestEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<BookingStatusLogEntity, object>>)((BookingStatusLogEntity x) => x.BookingID))
				.OnDelete((DeleteBehavior)3);
		});
		modelBuilder.Entity<PaymentTransactionEntity>((Action<EntityTypeBuilder<PaymentTransactionEntity>>)delegate(EntityTypeBuilder<PaymentTransactionEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<PaymentTransactionEntity>(e, "PaymentTransaction");
			e.HasKey((Expression<Func<PaymentTransactionEntity, object>>)((PaymentTransactionEntity x) => x.TransactionID));
			e.HasIndex((Expression<Func<PaymentTransactionEntity, object>>)((PaymentTransactionEntity x) => x.TransactionRefNo)).IsUnique(true);
			e.HasOne<BookingRequestEntity>((Expression<Func<PaymentTransactionEntity, BookingRequestEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<PaymentTransactionEntity, object>>)((PaymentTransactionEntity x) => x.BookingID))
				.OnDelete((DeleteBehavior)1);
		});
		modelBuilder.Entity<OfficeUserRoleEntity>((Action<EntityTypeBuilder<OfficeUserRoleEntity>>)delegate(EntityTypeBuilder<OfficeUserRoleEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<OfficeUserRoleEntity>(e, "OfficeUserRole");
			e.HasKey((Expression<Func<OfficeUserRoleEntity, object>>)((OfficeUserRoleEntity x) => x.RoleID));
		});
		modelBuilder.Entity<OfficeUserEntity>((Action<EntityTypeBuilder<OfficeUserEntity>>)delegate(EntityTypeBuilder<OfficeUserEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<OfficeUserEntity>(e, "OfficeUser");
			e.HasKey((Expression<Func<OfficeUserEntity, object>>)((OfficeUserEntity x) => x.OfficeUserID));
			e.HasIndex((Expression<Func<OfficeUserEntity, object>>)((OfficeUserEntity x) => x.Username)).IsUnique(true);
			e.HasOne<OfficeUserRoleEntity>((Expression<Func<OfficeUserEntity, OfficeUserRoleEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<OfficeUserEntity, object>>)((OfficeUserEntity x) => x.RoleID))
				.IsRequired(required: true)
				.OnDelete((DeleteBehavior)0);
		});
		modelBuilder.Entity<VenueUserMappingEntity>((Action<EntityTypeBuilder<VenueUserMappingEntity>>)delegate(EntityTypeBuilder<VenueUserMappingEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<VenueUserMappingEntity>(e, "VenueUserMapping");
			e.HasKey((Expression<Func<VenueUserMappingEntity, object>>)((VenueUserMappingEntity x) => x.MappingID));
			e.HasIndex((Expression<Func<VenueUserMappingEntity, object>>)((VenueUserMappingEntity x) => new { x.VenueID, x.OfficeUserID, x.RoleLevel })).IsUnique(true);
			e.HasOne<VenueMasterEntity>((Expression<Func<VenueUserMappingEntity, VenueMasterEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueUserMappingEntity, object>>)((VenueUserMappingEntity x) => x.VenueID))
				.OnDelete((DeleteBehavior)3);
			e.HasOne<OfficeUserEntity>((Expression<Func<VenueUserMappingEntity, OfficeUserEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueUserMappingEntity, object>>)((VenueUserMappingEntity x) => x.OfficeUserID))
				.OnDelete((DeleteBehavior)1);
			e.HasOne<OfficeUserRoleEntity>((Expression<Func<VenueUserMappingEntity, OfficeUserRoleEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueUserMappingEntity, object>>)((VenueUserMappingEntity x) => x.RoleLevel))
				.OnDelete((DeleteBehavior)0);
		});
		modelBuilder.Entity<VenueBlockedDateEntity>((Action<EntityTypeBuilder<VenueBlockedDateEntity>>)delegate(EntityTypeBuilder<VenueBlockedDateEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<VenueBlockedDateEntity>(e, "VenueBlockedDate");
			e.HasKey((Expression<Func<VenueBlockedDateEntity, object>>)((VenueBlockedDateEntity x) => x.BlockedID));
			e.HasIndex((Expression<Func<VenueBlockedDateEntity, object>>)((VenueBlockedDateEntity x) => new { x.VenueID, x.BlockedDate })).IsUnique(true);
			e.HasOne<VenueMasterEntity>((Expression<Func<VenueBlockedDateEntity, VenueMasterEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueBlockedDateEntity, object>>)((VenueBlockedDateEntity x) => x.VenueID))
				.OnDelete((DeleteBehavior)3);
			e.HasOne<BookingRequestEntity>((Expression<Func<VenueBlockedDateEntity, BookingRequestEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueBlockedDateEntity, object>>)((VenueBlockedDateEntity x) => x.BookingID))
				.OnDelete((DeleteBehavior)2);
		});
		modelBuilder.Entity<VenueEarningExpenseEntity>((Action<EntityTypeBuilder<VenueEarningExpenseEntity>>)delegate(EntityTypeBuilder<VenueEarningExpenseEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<VenueEarningExpenseEntity>(e, "VenueEarningExpense");
			e.HasKey((Expression<Func<VenueEarningExpenseEntity, object>>)((VenueEarningExpenseEntity x) => x.EntryID));
			e.HasOne<VenueMasterEntity>((Expression<Func<VenueEarningExpenseEntity, VenueMasterEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueEarningExpenseEntity, object>>)((VenueEarningExpenseEntity x) => x.VenueID))
				.OnDelete((DeleteBehavior)1);
			e.HasOne<OfficeUserEntity>((Expression<Func<VenueEarningExpenseEntity, OfficeUserEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<VenueEarningExpenseEntity, object>>)((VenueEarningExpenseEntity x) => x.EnteredByID))
				.OnDelete((DeleteBehavior)1);
		});
		modelBuilder.Entity<FinalSettlementEntity>((Action<EntityTypeBuilder<FinalSettlementEntity>>)delegate(EntityTypeBuilder<FinalSettlementEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<FinalSettlementEntity>(e, "FinalSettlement");
			e.HasKey((Expression<Func<FinalSettlementEntity, object>>)((FinalSettlementEntity x) => x.SettlementID));
			e.HasOne<BookingRequestEntity>((Expression<Func<FinalSettlementEntity, BookingRequestEntity>>)null).WithMany((string)null).HasForeignKey((Expression<Func<FinalSettlementEntity, object>>)((FinalSettlementEntity x) => x.BookingID))
				.OnDelete((DeleteBehavior)1);
		});
		modelBuilder.Entity<SmsLogEntity>((Action<EntityTypeBuilder<SmsLogEntity>>)delegate(EntityTypeBuilder<SmsLogEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<SmsLogEntity>(e, "SMSLog");
			e.HasKey((Expression<Func<SmsLogEntity, object>>)((SmsLogEntity x) => x.SMSID));
		});
		modelBuilder.Entity<AdvertisementEntity>((Action<EntityTypeBuilder<AdvertisementEntity>>)delegate(EntityTypeBuilder<AdvertisementEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<AdvertisementEntity>(e, "Advertisement");
			e.HasKey((Expression<Func<AdvertisementEntity, object>>)((AdvertisementEntity x) => x.AdID));
		});
		modelBuilder.Entity<TextAdvertisementEntity>((Action<EntityTypeBuilder<TextAdvertisementEntity>>)delegate(EntityTypeBuilder<TextAdvertisementEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<TextAdvertisementEntity>(e, "TextAdvertisement");
			e.HasKey((Expression<Func<TextAdvertisementEntity, object>>)((TextAdvertisementEntity x) => x.TextAdID));
		});
		modelBuilder.Entity<ImageBannerEntity>((Action<EntityTypeBuilder<ImageBannerEntity>>)delegate(EntityTypeBuilder<ImageBannerEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<ImageBannerEntity>(e, "ImageBanner");
			e.HasKey((Expression<Func<ImageBannerEntity, object>>)((ImageBannerEntity x) => x.ImgId));
		});
		modelBuilder.Entity<TermsAndConditionsEntity>((Action<EntityTypeBuilder<TermsAndConditionsEntity>>)delegate(EntityTypeBuilder<TermsAndConditionsEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<TermsAndConditionsEntity>(e, "TermsAndConditions");
			e.HasKey((Expression<Func<TermsAndConditionsEntity, object>>)((TermsAndConditionsEntity x) => x.TermID));
		});
		modelBuilder.Entity<BankAccountDetailEntity>((Action<EntityTypeBuilder<BankAccountDetailEntity>>)delegate(EntityTypeBuilder<BankAccountDetailEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<BankAccountDetailEntity>(e, "BankAccountDetail");
			e.HasKey((Expression<Func<BankAccountDetailEntity, object>>)((BankAccountDetailEntity x) => x.BankId));
			e.HasIndex((Expression<Func<BankAccountDetailEntity, object>>)((BankAccountDetailEntity x) => x.AccountNumber)).IsUnique(true);
		});
		modelBuilder.Entity<WebsiteVisitEntity>((Action<EntityTypeBuilder<WebsiteVisitEntity>>)delegate(EntityTypeBuilder<WebsiteVisitEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<WebsiteVisitEntity>(e, "WebsiteVisit");
			e.HasKey((Expression<Func<WebsiteVisitEntity, object>>)((WebsiteVisitEntity x) => x.VisitID));
		});
		modelBuilder.Entity<SuperAdminProvisioningTokenEntity>((Action<EntityTypeBuilder<SuperAdminProvisioningTokenEntity>>)delegate(EntityTypeBuilder<SuperAdminProvisioningTokenEntity> e)
		{
			RelationalEntityTypeBuilderExtensions.ToTable<SuperAdminProvisioningTokenEntity>(e, "SuperAdminProvisioningToken");
			e.HasKey((Expression<Func<SuperAdminProvisioningTokenEntity, object>>)((SuperAdminProvisioningTokenEntity x) => x.TokenId));
			e.HasIndex((Expression<Func<SuperAdminProvisioningTokenEntity, object>>)((SuperAdminProvisioningTokenEntity x) => x.TokenHash)).IsUnique(true);
		});
	}
}
