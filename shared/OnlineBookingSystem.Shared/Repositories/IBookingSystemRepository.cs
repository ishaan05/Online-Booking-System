using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OnlineBookingSystem.Shared.ViewModels;

namespace OnlineBookingSystem.Shared.Repositories;

public interface IBookingSystemRepository
{
	Task<IReadOnlyList<VenueListVm>> GetActiveVenuesPublicAsync(CancellationToken ct = default(CancellationToken));

	Task<VenueDetailVm?> GetVenueDetailAsync(int id, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<VenueRentRuleVm>> GetRentRulesForHallAsync(int hallId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<AdvertisementVm>> GetAdvertisementsPublicAsync(DateOnly onDate, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<BookingCategoryVm>> GetBookingCategoriesActiveAsync(CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<VenueTypeVm>> GetVenueTypesActiveAsync(CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<VenueTypeVm>> GetAllVenueTypesAdminAsync(CancellationToken ct = default(CancellationToken));

	Task<int> UpsertVenueTypeAsync(VenueTypeUpsertVm body, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<TermsVm>> GetTermsActiveAsync(CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<BookingPurposeVm>> GetBookingPurposesActiveAsync(CancellationToken ct = default(CancellationToken));

	Task<RentQuoteResponse?> GetRentQuoteAsync(int venueId, int categoryId, int purposeId, int totalDays, CancellationToken ct = default(CancellationToken));

	Task<bool> CheckVenueAvailabilityAsync(int venueId, DateOnly from, DateOnly to, CancellationToken ct = default(CancellationToken));

	Task<CreateBookingResponse> CreatePublicBookingAsync(CreateBookingRequestVm body, CancellationToken ct = default(CancellationToken), bool skipCustomerStatusLog = false);

	Task<PublicBookingStatusVm?> GetPublicBookingStatusAsync(string bookingRegNo, CancellationToken ct = default(CancellationToken));

	Task<PublicBookingStatusVm?> GetPublicBookingStatusForRegisteredUserAsync(string bookingRegNo, int registeredUserId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<CustomerBookingListVm>> GetCustomerBookingsForUserAsync(int userId, string? email, string? mobileNumber, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<CustomerBookingListVm>> GetCustomerBookingsForAuthenticatedUserAsync(int userId, CancellationToken ct = default(CancellationToken));

	Task<BookingReceiptVm?> GetBookingReceiptAsync(string bookingRegNo, CancellationToken ct = default(CancellationToken));

	Task<BookingReceiptVm?> GetBookingReceiptForRegisteredUserAsync(string bookingRegNo, int registeredUserId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<BookingStatusLogVm>?> GetBookingStatusLogsForRegisteredUserAsync(int registeredUserId, int bookingId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<BookingStatusLogVm>> GetBookingStatusLogsForOfficeAsync(OfficePortalAccessVm access, int bookingId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<CalendarDateVm>> GetVenueCalendarAsync(int venueId, DateOnly from, DateOnly to, CancellationToken ct = default(CancellationToken));

	Task<RegisterUserResponse> RegisterOrLoginUserAsync(string fullName, string mobile, CancellationToken ct = default(CancellationToken));

	Task<RegisterAccountResponse> RegisterAccountAsync(RegisterAccountRequest body, CancellationToken ct = default(CancellationToken));

	Task<LoginAccountResponse> LoginAccountAsync(LoginAccountRequest body, CancellationToken ct = default(CancellationToken));

	Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest body, CancellationToken ct = default(CancellationToken));

	Task<string> GenerateOtpAsync(string mobile, string purpose, CancellationToken ct = default(CancellationToken));

	Task<bool> ValidateOtpAsync(string mobile, string otp, string purpose, CancellationToken ct = default(CancellationToken));

	Task<OfficePortalAccessVm?> GetOfficePortalAccessAsync(int officeUserId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<AdminBookingGridVm>> GetAdminBookingsForGridAsync(OfficePortalAccessVm access, CancellationToken ct = default(CancellationToken));

	Task<DashboardActivityBundleVm> GetRecentDashboardActivityAsync(OfficePortalAccessVm access, int take, CancellationToken ct = default(CancellationToken));

	Task<AdminBookingDetailVm?> GetAdminBookingDetailAsync(OfficePortalAccessVm access, int bookingId, CancellationToken ct = default(CancellationToken));

	Task ExecuteL1BookingActionAsync(OfficePortalAccessVm access, int bookingId, string action, string? remarks, CancellationToken ct = default(CancellationToken));

	Task ExecuteL2BookingActionAsync(OfficePortalAccessVm access, int bookingId, string action, string? remarks, CancellationToken ct = default(CancellationToken));

	Task ExecuteL1FinalApproveAsync(OfficePortalAccessVm access, L1FinalApproveVm body, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<VenueAdminRowVm>> GetAllVenuesAdminAsync(OfficePortalAccessVm access, CancellationToken ct = default(CancellationToken));

	Task<int> UpsertVenueAsync(VenueMasterUpsertVm body, CancellationToken ct = default(CancellationToken));

	Task DeleteVenueAsync(int id, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<RateChartLikeVm>> GetRateChartsAsync(OfficePortalAccessVm access, CancellationToken ct = default(CancellationToken));

	Task<int> UpsertRentRuleAsync(int venueId, VenueRentRuleVm body, CancellationToken ct = default(CancellationToken));

	Task DeleteRentRuleAsync(int ruleId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<BookingCategoryVm>> GetAllBookingCategoriesAsync(CancellationToken ct = default(CancellationToken));

	Task<int> UpsertBookingCategoryAsync(BookingCategoryUpsertVm body, CancellationToken ct = default(CancellationToken));

	Task DeleteBookingCategoryAsync(int id, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<BookingPurposeVm>> GetAllBookingPurposesAsync(CancellationToken ct = default(CancellationToken));

	Task<int> UpsertBookingPurposeAsync(BookingPurposeUpsertVm body, CancellationToken ct = default(CancellationToken));

	Task DeleteBookingPurposeAsync(int id, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<AdvertisementVm>> GetAllAdvertisementsAsync(CancellationToken ct = default(CancellationToken));

	Task<int> UpsertAdvertisementAsync(AdvertisementUpsertVm body, CancellationToken ct = default(CancellationToken));

	Task DeleteAdvertisementAsync(int compositeAdId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<TextAdvertisementVm>> GetTextAdvertisementsPublicAsync(DateOnly onDate, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<TextAdvertisementVm>> GetAllTextAdvertisementsAsync(CancellationToken ct = default(CancellationToken));

	Task<int> UpsertTextAdvertisementAsync(TextAdvertisementUpsertVm body, CancellationToken ct = default(CancellationToken));

	Task DeleteTextAdvertisementAsync(int textAdId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<ImageBannerVm>> GetImageBannersPublicAsync(DateOnly onDate, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<ImageBannerVm>> GetAllImageBannersAsync(CancellationToken ct = default(CancellationToken));

	Task<int> UpsertImageBannerAsync(ImageBannerUpsertVm body, CancellationToken ct = default(CancellationToken));

	Task DeleteImageBannerAsync(int imgId, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<OfficeUserVm>> GetOfficeUsersAsync(CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<OfficeUserRoleVm>> GetOfficeUserRolesAsync(CancellationToken ct = default(CancellationToken));

	Task<OfficeUserVm?> GetOfficeUserAsync(int id, CancellationToken ct = default(CancellationToken));

	Task<int> CreateOfficeUserAsync(OfficeUserCreateVm body, CancellationToken ct = default(CancellationToken));

	/// <summary>Returns true if at least one active office user with Super Admin role (RoleID = 1) exists.</summary>
	Task<bool> AnyActiveSuperAdminExistsAsync(CancellationToken ct = default(CancellationToken));

	/// <summary>
	/// Atomically validates a one-time provisioning token, creates the first Super Admin, and invalidates the token.
	/// </summary>
	Task<SuperAdminProvisionResult> TryProvisionFirstSuperAdminAsync(
		byte[] tokenHash,
		byte[]? requestIpFingerprint,
		BootstrapSuperAdminRequest request,
		CancellationToken ct = default(CancellationToken));

	/// <summary>Persists a new one-time provisioning token (hash only).</summary>
	Task MintSuperAdminProvisioningTokenAsync(byte[] tokenHash, DateTime expiresAtUtc, byte[]? boundIpFingerprint, CancellationToken ct = default(CancellationToken));

	Task UpdateOfficeUserAsync(int id, OfficeUserUpdateVm body, CancellationToken ct = default(CancellationToken));

	Task DeactivateOfficeUserAsync(int id, CancellationToken ct = default(CancellationToken));

	Task<IReadOnlyList<AccountDetailsLikeVm>> GetBankAccountsAsync(CancellationToken ct = default(CancellationToken));

	Task SetVenueActiveAsync(OfficePortalAccessVm access, int venueId, bool isActive, CancellationToken ct = default(CancellationToken));

	Task CancelBookingBySuperAdminAsync(OfficePortalAccessVm access, int bookingId, string? remarks, CancellationToken ct = default(CancellationToken));

	Task<CreateBookingResponse> CreateAdminVenueBookingAsync(OfficePortalAccessVm access, AdminVenueBookingCreateVm body, CancellationToken ct = default(CancellationToken));

}
