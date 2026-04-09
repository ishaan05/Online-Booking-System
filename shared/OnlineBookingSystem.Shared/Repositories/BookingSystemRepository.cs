using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using BCrypt.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineBookingSystem.Shared.Data;
using OnlineBookingSystem.Shared.Helpers;
using OnlineBookingSystem.Shared.Models;
using OnlineBookingSystem.Shared.Security;
using OnlineBookingSystem.Shared.Services;
using OnlineBookingSystem.Shared.ViewModels;

namespace OnlineBookingSystem.Shared.Repositories;

public class BookingSystemRepository : IBookingSystemRepository
{
	private readonly AppDbContext _db;

	private readonly IConfiguration _cfg;

	private readonly ISmsService _sms;

	private readonly ILogger<BookingSystemRepository> _log;

	public BookingSystemRepository(AppDbContext db, IConfiguration cfg, ISmsService sms, ILogger<BookingSystemRepository> log)
	{
		_db = db;
		_cfg = cfg;
		_sms = sms;
		_log = log;
	}

	private decimal ServiceTaxPercentFromSettings()
	{
		string s = _cfg["Booking:ServiceTaxPercent"];
		if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && result > 0m && result <= 100m)
		{
			return result;
		}
		return 18m;
	}

	private static DateOnly ToDateOnly(DateTime? dt)
	{
		return (!dt.HasValue) ? default(DateOnly) : DateOnly.FromDateTime(dt.Value);
	}

	private static string NormalizeMobileCore(string mobile)
	{
		string text = new string(mobile.Where(char.IsDigit).ToArray());
		string result;
		if (text.Length <= 10)
		{
			result = text;
		}
		else
		{
			int length = text.Length;
			int num = length - 10;
			result = text.Substring(num, length - num);
		}
		return result;
	}

	private static void TryParseFacilitiesJson(string? json, out string? capacity, out string? areaSqmt, out string? rooms, out string? kitchen, out string? toilet, out string? bathroom, out string? notes)
	{
		capacity = (areaSqmt = (rooms = (kitchen = (toilet = (bathroom = (notes = null))))));
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.TryGetProperty("capacity", out var value))
			{
				capacity = value.GetString();
			}
			if (rootElement.TryGetProperty("areaSqmt", out var value2))
			{
				areaSqmt = value2.GetString();
			}
			if (rootElement.TryGetProperty("rooms", out var value3))
			{
				rooms = value3.GetString();
			}
			if (rootElement.TryGetProperty("kitchen", out var value4))
			{
				kitchen = value4.GetString();
			}
			if (rootElement.TryGetProperty("toilet", out var value5))
			{
				toilet = value5.GetString();
			}
			if (rootElement.TryGetProperty("bathroom", out var value6))
			{
				bathroom = value6.GetString();
			}
			if (rootElement.TryGetProperty("notes", out var value7))
			{
				notes = value7.GetString();
			}
		}
		catch
		{
			notes = json;
		}
	}

	private static string BuildFacilitiesJson(VenueMasterEntity v)
	{
		TryParseFacilitiesJson(v.Facilities, out string capacity, out string areaSqmt, out string rooms, out string kitchen, out string toilet, out string bathroom, out string notes);
		if (v.Capacity.HasValue)
		{
			capacity = v.Capacity.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (v.AreaSqMt.HasValue)
		{
			areaSqmt = v.AreaSqMt.Value.ToString("0.##", CultureInfo.InvariantCulture);
		}

		if (v.NoOfRooms.HasValue)
		{
			rooms = v.NoOfRooms.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (v.NoOfKitchen.HasValue)
		{
			kitchen = v.NoOfKitchen.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (v.NoOfToilet.HasValue)
		{
			toilet = v.NoOfToilet.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (v.NoOfBathroom.HasValue)
		{
			bathroom = v.NoOfBathroom.Value.ToString(CultureInfo.InvariantCulture);
		}

		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (!string.IsNullOrEmpty(capacity))
		{
			dictionary["capacity"] = capacity;
		}
		if (!string.IsNullOrEmpty(areaSqmt))
		{
			dictionary["areaSqmt"] = areaSqmt;
		}
		if (!string.IsNullOrEmpty(rooms))
		{
			dictionary["rooms"] = rooms;
		}
		if (!string.IsNullOrEmpty(kitchen))
		{
			dictionary["kitchen"] = kitchen;
		}
		if (!string.IsNullOrEmpty(toilet))
		{
			dictionary["toilet"] = toilet;
		}
		if (!string.IsNullOrEmpty(bathroom))
		{
			dictionary["bathroom"] = bathroom;
		}
		if (!string.IsNullOrEmpty(notes))
		{
			dictionary["notes"] = notes;
		}
		return (dictionary.Count == 0) ? (v.Facilities ?? "{}") : JsonSerializer.Serialize(dictionary);
	}

	private static void MergeFacilitiesIntoVenue(VenueMasterEntity v, string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (v.Facilities != null)
			{
				try
				{
					using JsonDocument jsonDocument2 = JsonDocument.Parse(v.Facilities);
					foreach (JsonProperty item in jsonDocument2.RootElement.EnumerateObject())
					{
						dictionary[item.Name] = item.Value.GetString() ?? "";
					}
				}
				catch
				{
				}
			}
			foreach (JsonProperty item2 in rootElement.EnumerateObject())
			{
				dictionary[item2.Name] = item2.Value.GetString() ?? "";
			}
			v.Facilities = JsonSerializer.Serialize(dictionary);
		}
		catch
		{
			v.Facilities = json;
		}
	}

	public async Task<IReadOnlyList<VenueListVm>> GetActiveVenuesPublicAsync(CancellationToken ct = default(CancellationToken))
	{
		var q = from v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters)
			join t in EntityFrameworkQueryableExtensions.AsNoTracking<VenueTypeEntity>((IQueryable<VenueTypeEntity>)_db.VenueTypes) on v.VenueTypeID equals t.VenueTypeID into tj
			from t in tj.DefaultIfEmpty()
			where v.IsActive
			orderby v.VenueName
			select new
			{
				v = v,
				TypeName = ((t != null) ? t.TypeName : "")
			};
		var list = await EntityFrameworkQueryableExtensions.ToListAsync(q, ct);
		IQueryable<VenueImageEntity> imgQ = from i in EntityFrameworkQueryableExtensions.AsNoTracking<VenueImageEntity>((IQueryable<VenueImageEntity>)_db.VenueImages)
			where i.IsActive
			select i;
		List<VenueListVm> outList = new List<VenueListVm>();
		foreach (var x in list)
		{
			string primary = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<string>(from i in imgQ
				where i.VenueID == x.v.VenueID
				orderby i.SortOrder
				select i.ImagePath, ct);
			TryParseFacilitiesJson(x.v.Facilities, out string cap, out string _, out string _, out string _, out string _, out string _, out string _);
			int? capN = x.v.Capacity ?? (int.TryParse(cap, out var c) ? c : (int?)null);
			outList.Add(new VenueListVm(x.v.VenueID, x.v.VenueTypeID, x.TypeName, x.v.VenueName, x.v.VenueCode, x.v.Address, x.v.City, x.v.Division, x.v.GoogleMapLink, BuildFacilitiesJson(x.v), primary, capN));
			cap = null;
		}
		return outList;
	}

	public async Task<VenueDetailVm?> GetVenueDetailAsync(int id, CancellationToken ct = default(CancellationToken))
	{
		VenueMasterEntity h = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters), (Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == id && x.IsActive), ct);
		if (h == null)
		{
			return null;
		}
		string typeName = (await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<string>(from t in EntityFrameworkQueryableExtensions.AsNoTracking<VenueTypeEntity>((IQueryable<VenueTypeEntity>)_db.VenueTypes)
			where t.VenueTypeID == h.VenueTypeID
			select t.TypeName, ct)) ?? "";
		List<VenueImageVm> images = await EntityFrameworkQueryableExtensions.ToListAsync<VenueImageVm>(from i in EntityFrameworkQueryableExtensions.AsNoTracking<VenueImageEntity>((IQueryable<VenueImageEntity>)_db.VenueImages)
			where i.VenueID == id && i.IsActive
			orderby i.SortOrder
			select new VenueImageVm(i.ImageID, i.VenueID, i.ImagePath, i.Caption, i.SortOrder, i.IsActive), ct);
		IReadOnlyList<VenueRentRuleVm> rules = await GetRentRulesForHallAsync(id, ct);
		TryParseFacilitiesJson(h.Facilities, out string cap, out string area, out string rooms, out string kit, out string toi, out string bath, out string notes);
		if (h.Capacity.HasValue)
		{
			cap = h.Capacity.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (h.AreaSqMt.HasValue)
		{
			area = h.AreaSqMt.Value.ToString("0.##", CultureInfo.InvariantCulture);
		}

		if (h.NoOfRooms.HasValue)
		{
			rooms = h.NoOfRooms.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (h.NoOfKitchen.HasValue)
		{
			kit = h.NoOfKitchen.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (h.NoOfToilet.HasValue)
		{
			toi = h.NoOfToilet.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (h.NoOfBathroom.HasValue)
		{
			bath = h.NoOfBathroom.Value.ToString(CultureInfo.InvariantCulture);
		}

		return new VenueDetailVm(PrimaryImagePath: images.FirstOrDefault()?.ImagePath, VenueID: h.VenueID, VenueTypeID: h.VenueTypeID, TypeName: typeName, VenueName: h.VenueName, VenueCode: h.VenueCode, Address: h.Address, City: h.City, Division: h.Division, GoogleMapLink: h.GoogleMapLink, Facilities: BuildFacilitiesJson(h), Images: images, RentRules: rules, Capacity: cap, AreaInSqmt: area, NoOfRoomsAvailable: rooms, NoOfKitchen: kit, NoOfToilet: toi, NoOfBathroom: bath, AdditionalFacilities: notes);
	}

	public async Task<IReadOnlyList<VenueRentRuleVm>> GetRentRulesForHallAsync(int hallId, CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)(from r in EntityFrameworkQueryableExtensions.AsNoTracking<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)_db.VenueRentRules)
			where r.VenueID == hallId && r.IsActive
			orderby r.RuleID
			select r), ct)).Select((VenueRentRuleEntity r) => new VenueRentRuleVm(r.RuleID, r.VenueID, r.CategoryID, r.PurposeID, r.RentPerDay, r.SecurityDeposit, r.MaxDays, r.IsAllottable, r.NotAllottableReason, r.IsActive)).ToList();
	}

	public async Task<IReadOnlyList<AdvertisementVm>> GetAdvertisementsPublicAsync(DateOnly onDate, CancellationToken ct = default(CancellationToken))
	{
		DateTime d = onDate.ToDateTime(TimeOnly.MinValue);
		return (await EntityFrameworkQueryableExtensions.ToListAsync<AdvertisementEntity>((IQueryable<AdvertisementEntity>)(from x in EntityFrameworkQueryableExtensions.AsNoTracking<AdvertisementEntity>((IQueryable<AdvertisementEntity>)_db.Advertisements)
			where x.IsActive && x.StartDate <= d && x.EndDate >= d
			orderby x.AdID
			select x), ct)).Select((AdvertisementEntity a) => new AdvertisementVm(a.AdID, a.AdTitle, a.AdImagePath, a.AdURL, ToDateOnly(a.StartDate), ToDateOnly(a.EndDate), a.IsActive)).ToList();
	}

	public async Task<IReadOnlyList<BookingCategoryVm>> GetBookingCategoriesActiveAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)(from c in EntityFrameworkQueryableExtensions.AsNoTracking<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories)
			where c.IsActive
			orderby c.CategoryID
			select c), ct)).Select((BookingCategoryEntity c) => new BookingCategoryVm(c.CategoryID, c.CategoryName, c.IdentityLabel, c.IdentityFormat, c.DocumentLabel, c.IsActive)).ToList();
	}

	public async Task<IReadOnlyList<VenueTypeVm>> GetVenueTypesActiveAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<VenueTypeEntity>((IQueryable<VenueTypeEntity>)(from t in EntityFrameworkQueryableExtensions.AsNoTracking<VenueTypeEntity>((IQueryable<VenueTypeEntity>)_db.VenueTypes)
			where t.IsActive
			orderby t.VenueTypeID
			select t), ct)).Select((VenueTypeEntity t) => new VenueTypeVm(t.VenueTypeID, t.TypeName, t.IsActive)).ToList();
	}

	public async Task<IReadOnlyList<VenueTypeVm>> GetAllVenueTypesAdminAsync(CancellationToken ct = default(CancellationToken))
	{
		return await _db.VenueTypes.AsNoTracking()
			.OrderBy((VenueTypeEntity t) => t.VenueTypeID)
			.Select((VenueTypeEntity t) => new VenueTypeVm(t.VenueTypeID, t.TypeName, t.IsActive))
			.ToListAsync(ct);
	}

	public async Task<int> UpsertVenueTypeAsync(VenueTypeUpsertVm body, CancellationToken ct = default(CancellationToken))
	{
		string name = (body.TypeName ?? "").Trim();
		if (name.Length == 0)
		{
			throw new InvalidOperationException("Venue type name is required.");
		}

		if (name.Length > 50)
		{
			name = name.Substring(0, 50);
		}

		if (body.VenueTypeID.HasValue && body.VenueTypeID.Value > 0)
		{
			int vid = body.VenueTypeID.Value;
			VenueTypeEntity row = await EntityFrameworkQueryableExtensions.FirstAsync<VenueTypeEntity>((IQueryable<VenueTypeEntity>)_db.VenueTypes, (Expression<Func<VenueTypeEntity, bool>>)((VenueTypeEntity t) => t.VenueTypeID == vid), ct);
			row.TypeName = name;
			row.IsActive = body.IsActive;
			await ((DbContext)_db).SaveChangesAsync(ct);
			return vid;
		}

		VenueTypeEntity inserted = new VenueTypeEntity
		{
			TypeName = name,
			IsActive = body.IsActive
		};
		_db.VenueTypes.Add(inserted);
		await ((DbContext)_db).SaveChangesAsync(ct);
		return inserted.VenueTypeID;
	}

	public async Task<IReadOnlyList<TermsVm>> GetTermsActiveAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<TermsAndConditionsEntity>((IQueryable<TermsAndConditionsEntity>)(from t in EntityFrameworkQueryableExtensions.AsNoTracking<TermsAndConditionsEntity>((IQueryable<TermsAndConditionsEntity>)_db.TermsAndConditions)
			where t.IsActive
			orderby t.SortOrder, t.TermID
			select t), ct)).Select((TermsAndConditionsEntity t) => new TermsVm(t.TermID, t.TermText, t.SortOrder, t.IsActive)).ToList();
	}

	public async Task<IReadOnlyList<BookingPurposeVm>> GetBookingPurposesActiveAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)(from p in EntityFrameworkQueryableExtensions.AsNoTracking<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes)
			where p.IsActive
			orderby p.PurposeID
			select p), ct)).Select((BookingPurposeEntity p) => new BookingPurposeVm(p.PurposeID, p.PurposeName, p.MaxDays, p.IsActive)).ToList();
	}

	public async Task<RentQuoteResponse?> GetRentQuoteAsync(int venueId, int categoryId, int purposeId, int totalDays, CancellationToken ct = default(CancellationToken))
	{
		if (await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters), (Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == venueId && x.IsActive), ct) == null)
		{
			return null;
		}
		VenueRentRuleEntity rule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)(from r in EntityFrameworkQueryableExtensions.AsNoTracking<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)_db.VenueRentRules)
			where r.VenueID == venueId && r.CategoryID == categoryId && r.PurposeID == purposeId && r.IsActive
			orderby r.RuleID
			select r), ct);
		if (rule == null)
		{
			return null;
		}
		if (!rule.IsAllottable)
		{
			return new RentQuoteResponse(IsAllottable: false, rule.NotAllottableReason, rule.RentPerDay, rule.SecurityDeposit, 0m, 0m, rule.MaxDays, ServiceTaxPercentFromSettings());
		}
		decimal rent = rule.RentPerDay * (decimal)totalDays;
		return new RentQuoteResponse(ServiceTaxPercent: ServiceTaxPercentFromSettings(), IsAllottable: true, NotAllottableReason: null, RentPerDay: rule.RentPerDay, SecurityDeposit: rule.SecurityDeposit, RentAmount: rent, TotalPayable: rent + rule.SecurityDeposit, MaxDays: rule.MaxDays);
	}

	public async Task<bool> CheckVenueAvailabilityAsync(int venueId, DateOnly from, DateOnly to, CancellationToken ct = default(CancellationToken))
	{
		if (await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters), (Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == venueId && x.IsActive), ct) == null)
		{
			return false;
		}
		DateTime f = from.ToDateTime(TimeOnly.MinValue);
		DateTime t = to.ToDateTime(TimeOnly.MinValue);
		if (await EntityFrameworkQueryableExtensions.AnyAsync<BookingRequestEntity>(from b in EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests)
			where b.VenueID == venueId
			where b.BookingStatus == null || (!DbFunctionsExtensions.Like(EF.Functions, b.BookingStatus, "%cancel%") && !DbFunctionsExtensions.Like(EF.Functions, b.BookingStatus, "%reject%"))
			select b, (Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity b) => b.BookingFromDate <= t && b.BookingToDate >= f), ct))
		{
			return false;
		}
		return !(await EntityFrameworkQueryableExtensions.AnyAsync<VenueBlockedDateEntity>(from x in EntityFrameworkQueryableExtensions.AsNoTracking<VenueBlockedDateEntity>((IQueryable<VenueBlockedDateEntity>)_db.VenueBlockedDates)
			where x.VenueID == venueId
			select x, (Expression<Func<VenueBlockedDateEntity, bool>>)((VenueBlockedDateEntity x) => x.BlockedDate >= f && x.BlockedDate <= t), ct));
	}

	public async Task<CreateBookingResponse> CreatePublicBookingAsync(
		CreateBookingRequestVm body,
		CancellationToken ct = default(CancellationToken),
		bool skipCustomerStatusLog = false)
	{
		if (!DateOnly.TryParse(body.FromDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
		{
			return new CreateBookingResponse(null, null, "Invalid from date.");
		}
		if (!DateOnly.TryParse(body.ToDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
		{
			return new CreateBookingResponse(null, null, "Invalid to date.");
		}
		if (body.UserID <= 0)
		{
			return new CreateBookingResponse(null, null, "Please sign in to complete the booking.");
		}
		VenueMasterEntity? hall = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters),
			(Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == body.VenueID && x.IsActive),
			ct);
		if (hall == null)
		{
			return new CreateBookingResponse(null, null, "Unknown hall.");
		}
		VenueRentRuleEntity rule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)(from r in EntityFrameworkQueryableExtensions.AsNoTracking<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)_db.VenueRentRules)
			where r.VenueID == body.VenueID && r.CategoryID == body.CategoryID && r.PurposeID == body.PurposeID && r.IsActive
			orderby r.RuleID
			select r), ct);
		if (rule == null || !rule.IsAllottable)
		{
			return new CreateBookingResponse(null, null, "No active rent rule for this hall, category, and purpose.");
		}
		int days = Math.Max(1, toDate.DayNumber - fromDate.DayNumber + 1);
		decimal rentAmount = rule.RentPerDay * (decimal)days;
		decimal security = rule.SecurityDeposit;
		decimal? totalPayable = body.TotalPayable;
		decimal tp = default(decimal);
		int num;
		if (totalPayable.HasValue)
		{
			tp = totalPayable.GetValueOrDefault();
			num = ((tp > 0m) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		if (num != 0)
		{
			decimal expected = rentAmount + security;
			if (Math.Abs(tp - expected) > 0.05m)
			{
				security = rule.SecurityDeposit;
				rentAmount = Math.Max(0m, tp - security);
			}
		}
		if (!(await CheckVenueAvailabilityAsync(body.VenueID, fromDate, toDate, ct)))
		{
			return new CreateBookingResponse(null, null, "Hall not available for these dates.");
		}
		if (await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<RegisteredUserEntity>(EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers), (Expression<Func<RegisteredUserEntity, bool>>)((RegisteredUserEntity x) => x.UserID == body.UserID), ct) == null)
		{
			return new CreateBookingResponse(null, null, "Unknown user.");
		}
		string tempRegNo = "T" + Guid.NewGuid().ToString("N").Substring(0, 28);
		BookingRequestEntity row = new BookingRequestEntity
		{
			BookingRegNo = tempRegNo,
			UserID = body.UserID,
			VenueID = body.VenueID,
			CategoryID = body.CategoryID,
			PurposeID = body.PurposeID,
			BookingFromDate = fromDate.ToDateTime(TimeOnly.MinValue),
			BookingToDate = toDate.ToDateTime(TimeOnly.MinValue),
			IdentityNumber = (body.IdentityNumber ?? ""),
			DocumentPath = (string.IsNullOrWhiteSpace(body.DocumentPath) ? "" : body.DocumentPath.Trim()),
			RentAmount = rentAmount,
			SecurityDeposit = security,
			BankName = (body.BankName ?? ""),
			AccountNumber = (body.AccountNumber ?? ""),
			IFSCCode = (body.IFSCCode ?? ""),
			TermsAccepted = true,
			BookingStatus = "Pending",
			PaymentStatus = "Unpaid",
			CreatedAt = DateTime.UtcNow
		};
		string bookingNo;
		try
		{
			await using (IDbContextTransaction tx = await ((DbContext)_db).Database.BeginTransactionAsync(ct))
			{
				_db.BookingRequests.Add(row);
				await ((DbContext)_db).SaveChangesAsync(ct);
				string venueCode = NormalizeVenueCodeForFn(hall.VenueCode, body.VenueID);
				SqlTransaction? sqlTx = GetAmbientSqlTransaction(_db);
				bookingNo = await ComputeFnBookingRegNoAsync(venueCode, row.BookingID, sqlTx, ct);
				row.BookingRegNo = bookingNo;
				await ((DbContext)_db).SaveChangesAsync(ct);
				await tx.CommitAsync(ct);
			}
		}
		catch (SqlException ex) when (ex.Number == 2812 || ex.Number == 208)
		{
			return new CreateBookingResponse(null, null, "Booking number service is not configured. Create scalar function dbo.fn_GenerateBookingRegNo (@VenueCode NVARCHAR(10), @BookingID INT) on SQL Server — see database/fn_GenerateBookingRegNo.sql.");
		}
		catch (Exception ex) when (ex is SqlException or InvalidOperationException)
		{
			return new CreateBookingResponse(null, null, $"Could not allocate booking number: {ex.Message}");
		}
		var u = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<RegisteredUserEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers),
			(Expression<Func<RegisteredUserEntity, bool>>)((RegisteredUserEntity x) => x.UserID == body.UserID),
			ct);
		var v = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters),
			(Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == body.VenueID),
			ct);
		if (u == null)
		{
			_log.LogWarning("Booking {BookingId} saved; SMS not sent: user {UserId} not found.", row.BookingID, body.UserID);
		}
		else if (string.IsNullOrWhiteSpace(u.MobileNumber))
		{
			_log.LogWarning("Booking {BookingId} saved; SMS not sent: no mobile number for user {UserId}.", row.BookingID, body.UserID);
		}
		else
		{
			await _sms.NotifyBookingSubmittedAsync(
				u.MobileNumber,
				bookingNo,
				v?.VenueName ?? "",
				fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				ct);
		}

		if (!skipCustomerStatusLog)
		{
			_db.BookingStatusLogs.Add(new BookingStatusLogEntity
			{
				BookingID = row.BookingID,
				ChangedByType = "Customer",
				ChangedByID = body.UserID,
				OldStatus = null,
				NewStatus = "Pending",
				Remarks = "Customer submitted booking.",
				ChangedAt = DateTime.UtcNow,
			});
			await ((DbContext)_db).SaveChangesAsync(ct);
		}

		return new CreateBookingResponse(bookingNo, row.BookingID, null);
	}

	public async Task<PublicBookingStatusVm?> GetPublicBookingStatusAsync(string bookingRegNo, CancellationToken ct = default(CancellationToken))
	{
		string q = bookingRegNo.Trim();
		var row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(from b in EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests)
			join v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters) on b.VenueID equals v.VenueID
			where b.BookingRegNo == q
			select new { b, v.VenueName }, ct);
		if (row == null)
		{
			return null;
		}
		DateOnly from = ToDateOnly(row.b.BookingFromDate);
		DateOnly to = ToDateOnly(row.b.BookingToDate);
		return new PublicBookingStatusVm(TotalDays: (!(from == default(DateOnly)) && !(to == default(DateOnly))) ? Math.Max(1, to.DayNumber - from.DayNumber + 1) : 0, BookingRegNo: row.b.BookingRegNo, Status: MapToPublicBookingStatus(row.b.BookingStatus), VenueName: row.VenueName, BookingFromDate: from, BookingToDate: to, TotalAmount: row.b.TotalAmount);
	}

	public async Task<PublicBookingStatusVm?> GetPublicBookingStatusForRegisteredUserAsync(string bookingRegNo, int registeredUserId, CancellationToken ct = default(CancellationToken))
	{
		string q = bookingRegNo.Trim();
		var row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(from b in EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests)
			join v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters) on b.VenueID equals v.VenueID
			where b.BookingRegNo == q && b.UserID == registeredUserId
			select new { b, v.VenueName }, ct);
		if (row == null)
		{
			return null;
		}
		DateOnly from = ToDateOnly(row.b.BookingFromDate);
		DateOnly to = ToDateOnly(row.b.BookingToDate);
		return new PublicBookingStatusVm(TotalDays: (!(from == default(DateOnly)) && !(to == default(DateOnly))) ? Math.Max(1, to.DayNumber - from.DayNumber + 1) : 0, BookingRegNo: row.b.BookingRegNo, Status: MapToPublicBookingStatus(row.b.BookingStatus), VenueName: row.VenueName, BookingFromDate: from, BookingToDate: to, TotalAmount: row.b.TotalAmount);
	}

	public async Task<IReadOnlyList<CustomerBookingListVm>> GetCustomerBookingsForUserAsync(int userId, string? email, string? mobileNumber, CancellationToken ct = default(CancellationToken))
	{
		RegisteredUserEntity? u = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<RegisteredUserEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers),
			(Expression<Func<RegisteredUserEntity, bool>>)((RegisteredUserEntity x) => x.UserID == userId),
			ct);
		if (u == null)
		{
			throw new UnauthorizedAccessException();
		}
		string em = (email ?? "").Trim();
		string mobDigits = CustomerBookingsDigitsOnly(mobileNumber);
		string uMobDigits = CustomerBookingsDigitsOnly(u.MobileNumber);
		bool emailMatch = em.Length > 0 && string.Equals((u.Email ?? "").Trim(), em, StringComparison.OrdinalIgnoreCase);
		bool mobileMatch = mobDigits.Length >= 8 && uMobDigits.Length >= 8 && string.Equals(mobDigits, uMobDigits, StringComparison.Ordinal);
		if (!emailMatch && !mobileMatch)
		{
			throw new UnauthorizedAccessException();
		}
		return await QueryCustomerBookingListAsync(userId, ct);
	}

	public async Task<IReadOnlyList<CustomerBookingListVm>> GetCustomerBookingsForAuthenticatedUserAsync(int userId, CancellationToken ct = default(CancellationToken))
	{
		if (userId <= 0)
		{
			throw new UnauthorizedAccessException();
		}
		RegisteredUserEntity? u = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<RegisteredUserEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers),
			(Expression<Func<RegisteredUserEntity, bool>>)((RegisteredUserEntity x) => x.UserID == userId),
			ct);
		if (u == null)
		{
			throw new UnauthorizedAccessException();
		}
		return await QueryCustomerBookingListAsync(userId, ct);
	}

	private async Task<IReadOnlyList<CustomerBookingListVm>> QueryCustomerBookingListAsync(int userId, CancellationToken ct)
	{
		var rows = await EntityFrameworkQueryableExtensions.ToListAsync(from b in EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests)
			join v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters) on b.VenueID equals v.VenueID
			join c in EntityFrameworkQueryableExtensions.AsNoTracking<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories) on b.CategoryID equals c.CategoryID into cj
			from c in cj.DefaultIfEmpty()
			join p in EntityFrameworkQueryableExtensions.AsNoTracking<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes) on b.PurposeID equals p.PurposeID into pj
			from p in pj.DefaultIfEmpty()
			where b.UserID == userId
			orderby b.CreatedAt descending
			select new
			{
				b,
				VenueName = v.VenueName,
				CategoryName = (c != null) ? c.CategoryName : "",
				PurposeName = (p != null) ? p.PurposeName : ""
			}, ct);
		List<CustomerBookingListVm> list = new List<CustomerBookingListVm>(rows.Count);
		foreach (var x in rows)
		{
			DateOnly from = ToDateOnly(x.b.BookingFromDate);
			DateOnly to = ToDateOnly(x.b.BookingToDate);
			list.Add(new CustomerBookingListVm(
				x.b.BookingID,
				x.b.BookingRegNo ?? "",
				x.VenueName ?? "",
				x.CategoryName ?? "",
				x.PurposeName ?? "",
				from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				x.b.TotalAmount,
				MapToPublicBookingStatus(x.b.BookingStatus),
				x.b.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)));
		}
		return list;
	}

	public async Task<BookingReceiptVm?> GetBookingReceiptAsync(string bookingRegNo, CancellationToken ct = default(CancellationToken))
	{
		string q = bookingRegNo.Trim();
		var row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(from b in EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests)
			join u in EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers) on b.UserID equals u.UserID
			join v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters) on b.VenueID equals v.VenueID
			join p in EntityFrameworkQueryableExtensions.AsNoTracking<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes) on b.PurposeID equals p.PurposeID into pj
			from p in pj.DefaultIfEmpty()
			where b.BookingRegNo == q
			select new
			{
				b = b,
				FullName = u.FullName,
				Purpose = ((p != null) ? p.PurposeName : ""),
				VenueName = v.VenueName
			}, ct);
		if (row == null)
		{
			return null;
		}
		var pay = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(from t in EntityFrameworkQueryableExtensions.AsNoTracking<PaymentTransactionEntity>((IQueryable<PaymentTransactionEntity>)_db.PaymentTransactions)
			where t.BookingID == row.b.BookingID && DbFunctionsExtensions.Like(EF.Functions, t.PaymentStatus, "%Success%")
			orderby t.TransactionDate descending
			select new { t.AmountPaid, t.PaymentMode, t.TransactionRefNo, t.TransactionDate }, ct);
		return new BookingReceiptVm(row.b.BookingRegNo, row.FullName ?? "", row.Purpose ?? "", ToDateOnly(row.b.BookingFromDate), ToDateOnly(row.b.BookingToDate), row.VenueName, pay?.AmountPaid, pay?.PaymentMode, pay?.TransactionRefNo, pay?.TransactionDate ?? row.b.CreatedAt);
	}

	public async Task<BookingReceiptVm?> GetBookingReceiptForRegisteredUserAsync(string bookingRegNo, int registeredUserId, CancellationToken ct = default(CancellationToken))
	{
		string q = bookingRegNo.Trim();
		var row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(from b in EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests)
			join u in EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers) on b.UserID equals u.UserID
			join v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters) on b.VenueID equals v.VenueID
			join p in EntityFrameworkQueryableExtensions.AsNoTracking<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes) on b.PurposeID equals p.PurposeID into pj
			from p in pj.DefaultIfEmpty()
			where b.BookingRegNo == q && b.UserID == registeredUserId
			select new
			{
				b = b,
				FullName = u.FullName,
				Purpose = ((p != null) ? p.PurposeName : ""),
				VenueName = v.VenueName
			}, ct);
		if (row == null)
		{
			return null;
		}
		var pay = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(from t in EntityFrameworkQueryableExtensions.AsNoTracking<PaymentTransactionEntity>((IQueryable<PaymentTransactionEntity>)_db.PaymentTransactions)
			where t.BookingID == row.b.BookingID && DbFunctionsExtensions.Like(EF.Functions, t.PaymentStatus, "%Success%")
			orderby t.TransactionDate descending
			select new { t.AmountPaid, t.PaymentMode, t.TransactionRefNo, t.TransactionDate }, ct);
		return new BookingReceiptVm(row.b.BookingRegNo, row.FullName ?? "", row.Purpose ?? "", ToDateOnly(row.b.BookingFromDate), ToDateOnly(row.b.BookingToDate), row.VenueName, pay?.AmountPaid, pay?.PaymentMode, pay?.TransactionRefNo, pay?.TransactionDate ?? row.b.CreatedAt);
	}

	public async Task<IReadOnlyList<BookingStatusLogVm>?> GetBookingStatusLogsForRegisteredUserAsync(int registeredUserId, int bookingId, CancellationToken ct = default(CancellationToken))
	{
		BookingRequestEntity? bk = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingRequestEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests),
			(Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity b) => b.BookingID == bookingId),
			ct);
		if (bk == null)
		{
			return null;
		}
		if (bk.UserID != registeredUserId)
		{
			return null;
		}
		return await QueryBookingStatusLogsAsync(bookingId, ct);
	}

	public async Task<IReadOnlyList<BookingStatusLogVm>> GetBookingStatusLogsForOfficeAsync(OfficePortalAccessVm access, int bookingId, CancellationToken ct = default(CancellationToken))
	{
		await EnsureOfficeUserCanAccessBookingAsync(access, bookingId, ct);
		return await QueryBookingStatusLogsAsync(bookingId, ct);
	}

	private async Task<IReadOnlyList<BookingStatusLogVm>> QueryBookingStatusLogsAsync(int bookingId, CancellationToken ct)
	{
		var rows = await EntityFrameworkQueryableExtensions.ToListAsync(
			from l in EntityFrameworkQueryableExtensions.AsNoTracking<BookingStatusLogEntity>((IQueryable<BookingStatusLogEntity>)_db.BookingStatusLogs)
			where l.BookingID == bookingId
			orderby l.ChangedAt ascending
			select l,
			ct);
		List<BookingStatusLogVm> list = new List<BookingStatusLogVm>(rows.Count);
		foreach (BookingStatusLogEntity l in rows)
		{
			list.Add(new BookingStatusLogVm(l.LogID, l.BookingID, l.ChangedByType, l.ChangedByID, l.OldStatus, l.NewStatus, l.Remarks, l.ChangedAt));
		}
		return list;
	}

	public async Task<IReadOnlyList<CalendarDateVm>> GetVenueCalendarAsync(int venueId, DateOnly from, DateOnly to, CancellationToken ct = default(CancellationToken))
	{
		if (await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters), (Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == venueId), ct) == null)
		{
			return Array.Empty<CalendarDateVm>();
		}
		DateTime f = from.ToDateTime(TimeOnly.MinValue);
		DateTime t = to.ToDateTime(TimeOnly.MinValue);
		HashSet<DateOnly> bookedDates = new HashSet<DateOnly>();
		foreach (BookingRequestEntity b in await EntityFrameworkQueryableExtensions.ToListAsync<BookingRequestEntity>(from bookingRequestEntity in EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests)
			where bookingRequestEntity.VenueID == venueId
			where bookingRequestEntity.BookingStatus == null || (!DbFunctionsExtensions.Like(EF.Functions, bookingRequestEntity.BookingStatus, "%cancel%") && !DbFunctionsExtensions.Like(EF.Functions, bookingRequestEntity.BookingStatus, "%reject%"))
			where bookingRequestEntity.BookingFromDate <= t && bookingRequestEntity.BookingToDate >= f
			select bookingRequestEntity, ct))
		{
			DateOnly a = ToDateOnly(b.BookingFromDate);
			DateOnly z = ToDateOnly(b.BookingToDate);
			DateOnly d = a;
			while (d <= z)
			{
				bookedDates.Add(d);
				d = d.AddDays(1);
			}
		}
		HashSet<DateOnly> blockedDates = new HashSet<DateOnly>();
		foreach (DateTime bd in await EntityFrameworkQueryableExtensions.ToListAsync<DateTime>(from x in EntityFrameworkQueryableExtensions.AsNoTracking<VenueBlockedDateEntity>((IQueryable<VenueBlockedDateEntity>)_db.VenueBlockedDates)
			where x.VenueID == venueId && x.BlockedDate >= f && x.BlockedDate <= t
			select x.BlockedDate, ct))
		{
			blockedDates.Add(ToDateOnly(bd));
		}
		List<CalendarDateVm> list = new List<CalendarDateVm>();
		for (DateOnly d = from; d <= to; d = d.AddDays(1))
		{
			string iso = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			if (blockedDates.Contains(d))
			{
				list.Add(new CalendarDateVm(iso, Available: false, UnavailableReason: "blocked"));
			}
			else if (bookedDates.Contains(d))
			{
				list.Add(new CalendarDateVm(iso, Available: false, UnavailableReason: "booked"));
			}
			else
			{
				list.Add(new CalendarDateVm(iso, Available: true, UnavailableReason: null));
			}
		}
		return list;
	}

	public async Task<RegisterUserResponse> RegisterOrLoginUserAsync(string fullName, string mobile, CancellationToken ct = default(CancellationToken))
	{
		string core = NormalizeMobileCore(mobile.Trim());
		RegisteredUserEntity existing = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers, (Expression<Func<RegisteredUserEntity, bool>>)((RegisteredUserEntity x) => x.MobileNumber == core || x.MobileNumber == mobile.Trim()), ct);
		if (existing != null)
		{
			return new RegisterUserResponse(existing.UserID, IsNewUser: false);
		}
		RegisteredUserEntity u = new RegisteredUserEntity
		{
			FullName = fullName.Trim(),
			MobileNumber = ((core.Length == 10) ? core : mobile.Trim()),
			CreatedAt = DateTime.UtcNow
		};
		_db.RegisteredUsers.Add(u);
		await ((DbContext)_db).SaveChangesAsync(ct);
		return new RegisterUserResponse(u.UserID, IsNewUser: true);
	}

	public async Task<RegisterAccountResponse> RegisterAccountAsync(RegisterAccountRequest body, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			string fn = (body.FullName ?? "").Trim();
			string mob = (body.MobileNumber ?? "").Trim();
			string em = (body.Email ?? "").Trim();
			string pwd = body.Password ?? "";
			if (fn.Length == 0 || mob.Length == 0 || em.Length == 0 || pwd.Length == 0)
			{
				return new RegisterAccountResponse(null, "All fields are required.");
			}
			if (fn.Length > 150 || mob.Length > 15 || em.Length > 256)
			{
				return new RegisterAccountResponse(null, "A value exceeds the maximum length allowed for the database.");
			}
			if (!PasswordPolicy.IsValid(pwd))
			{
				return new RegisterAccountResponse(null, PasswordPolicy.RequirementMessage);
			}
			if (!em.Contains(".com", StringComparison.OrdinalIgnoreCase))
			{
				return new RegisterAccountResponse(null, "Email must contain .com.");
			}
			string mobCore = NormalizeMobileCore(mob);
			if (mobCore.Length != 10)
			{
				return new RegisterAccountResponse(null, "Mobile number must be 10 digits.");
			}
			if (await EntityFrameworkQueryableExtensions.AnyAsync<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers, (Expression<Func<RegisteredUserEntity, bool>>)((RegisteredUserEntity x) => x.MobileNumber == mobCore || x.MobileNumber == mob || (x.Email != null && x.Email == em)), ct))
			{
				return new RegisterAccountResponse(null, "An account with this email or mobile number already exists.");
			}
			string hash = BCrypt.Net.BCrypt.HashPassword(pwd);
			RegisteredUserEntity u = new RegisteredUserEntity
			{
				FullName = fn,
				MobileNumber = mobCore,
				Email = em,
				PasswordHash = hash,
				CreatedAt = DateTime.UtcNow
			};
			_db.RegisteredUsers.Add(u);
			await ((DbContext)_db).SaveChangesAsync(ct);
			return new RegisterAccountResponse(u.UserID, null);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (DbUpdateException ex)
		{
			_log.LogWarning(ex, "RegisterAccount: insert into RegisteredUser failed (duplicate or schema).");
			return new RegisterAccountResponse(null, "An account with this email or mobile number already exists, or registration could not be completed.");
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "RegisterAccount: failed (connection, missing table/column, or unexpected error).");
			return new RegisterAccountResponse(null, "Registration could not be completed. Ensure the API can reach SQL Server and the RegisteredUser table matches the current schema.");
		}
	}

	public async Task<LoginAccountResponse> LoginAccountAsync(LoginAccountRequest body, CancellationToken ct = default(CancellationToken))
	{
		string id = CleanCredential(body.EmailOrMobile);
		string pwd = CleanCredential(body.Password);
		if (id.Length == 0 || pwd.Length == 0)
		{
			return new LoginAccountResponse(null, null, null, null, "Email or mobile and password are required.");
		}
		List<RegisteredUserEntity> candidates = await EntityFrameworkQueryableExtensions.ToListAsync<RegisteredUserEntity>(from x in EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers)
			where x.PasswordHash != null && x.PasswordHash != ""
			select x, ct);
		RegisteredUserEntity u = null;
		foreach (RegisteredUserEntity row in candidates)
		{
			string hash = row.PasswordHash ?? "";
			if (BCrypt.Net.BCrypt.Verify(pwd, hash))
			{
				string em = CleanCredential(row.Email);
				if (em.Length > 0 && string.Equals(em, id, StringComparison.OrdinalIgnoreCase))
				{
					u = row;
					break;
				}
				if (MobileMatches(id, row.MobileNumber))
				{
					u = row;
					break;
				}
			}
		}
		if (u == null)
		{
			return new LoginAccountResponse(null, null, null, null, "Invalid email or mobile, or password.");
		}
		(await EntityFrameworkQueryableExtensions.FirstAsync<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers, (Expression<Func<RegisteredUserEntity, bool>>)((RegisteredUserEntity x) => x.UserID == u.UserID), ct)).LastLoginAt = DateTime.UtcNow;
		await ((DbContext)_db).SaveChangesAsync(ct);
		return new LoginAccountResponse(u.UserID, u.FullName, u.MobileNumber, u.Email, null);
		static string CleanCredential(string? s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return "";
			}
			string source = s.Trim();
			return new string(source.Where((char c) => !char.IsControl(c) && c != '\u200b' && c != '\ufeff').ToArray());
		}
		static bool MobileMatches(string idClean, string? mobileStored)
		{
			string text = CleanCredential(mobileStored ?? "");
			if (text.Length == 0)
			{
				return false;
			}
			if (string.Equals(text, idClean, StringComparison.Ordinal))
			{
				return true;
			}
			string text2 = NormalizeMobileCore(idClean);
			string text3 = NormalizeMobileCore(text);
			return text2.Length >= 8 && text3.Length >= 8 && string.Equals(text2, text3, StringComparison.Ordinal);
		}
	}

	public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest body, CancellationToken ct = default(CancellationToken))
	{
		string id = CleanCredential(body.EmailOrMobile);
		string newPwd = CleanCredential(body.NewPassword ?? "");
		if (id.Length == 0 || newPwd.Length == 0)
		{
			return new ResetPasswordResponse("Email or mobile and new password are required.");
		}
		if (!IsValidResetIdentifier(id))
		{
			return new ResetPasswordResponse("Use a 10-digit mobile number, or an email address containing .com.");
		}
		if (!PasswordPolicy.IsValid(newPwd))
		{
			return new ResetPasswordResponse(PasswordPolicy.RequirementMessage);
		}
		List<RegisteredUserEntity> rows = await EntityFrameworkQueryableExtensions.ToListAsync<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers, ct);
		RegisteredUserEntity u = null;
		foreach (RegisteredUserEntity row in rows)
		{
			string em = CleanCredential(row.Email);
			if (em.Length > 0 && string.Equals(em, id, StringComparison.OrdinalIgnoreCase))
			{
				u = row;
				break;
			}
			if (MobileMatches(id, row.MobileNumber))
			{
				u = row;
				break;
			}
		}
		if (u == null)
		{
			return new ResetPasswordResponse("No account matches that email or mobile.");
		}
		u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPwd);
		await ((DbContext)_db).SaveChangesAsync(ct);
		return new ResetPasswordResponse(null);
		static string CleanCredential(string? s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return "";
			}
			string source = s.Trim();
			return new string(source.Where((char c) => !char.IsControl(c) && c != '\u200b' && c != '\ufeff').ToArray());
		}
		static bool IsValidResetIdentifier(string text)
		{
			if (text.Contains(".com", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			string text2 = new string(text.Where(char.IsDigit).ToArray());
			if (text2.Length == 0)
			{
				return false;
			}
			string text3;
			if (text2.Length <= 10)
			{
				text3 = text2;
			}
			else
			{
				int length = text2.Length;
				int num = length - 10;
				text3 = text2.Substring(num, length - num);
			}
			string text4 = text3;
			return text4.Length == 10;
		}
		static bool MobileMatches(string idClean, string? mobileStored)
		{
			string text = CleanCredential(mobileStored ?? "");
			if (text.Length == 0)
			{
				return false;
			}
			if (string.Equals(text, idClean, StringComparison.Ordinal))
			{
				return true;
			}
			string text2 = NormalizeMobileCore(idClean);
			string text3 = NormalizeMobileCore(text);
			return text2.Length >= 8 && text3.Length >= 8 && string.Equals(text2, text3, StringComparison.Ordinal);
		}
	}

	public async Task<string> GenerateOtpAsync(string mobile, string purpose, CancellationToken ct = default(CancellationToken))
	{
		string code = Random.Shared.Next(100000, 999999).ToString();
		OtpLogEntity log = new OtpLogEntity
		{
			MobileNumber = mobile.Trim(),
			OTPCode = code,
			Purpose = purpose.Trim(),
			GeneratedAt = DateTime.UtcNow,
			ExpiresAt = DateTime.UtcNow.AddMinutes(10.0)
		};
		_db.OtpLogs.Add(log);
		await ((DbContext)_db).SaveChangesAsync(ct);
		return code;
	}

	public async Task<bool> ValidateOtpAsync(string mobile, string otp, string purpose, CancellationToken ct = default(CancellationToken))
	{
		string m = mobile.Trim();
		string p = purpose.Trim();
		OtpLogEntity row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<OtpLogEntity>((IQueryable<OtpLogEntity>)(from x in (IQueryable<OtpLogEntity>)_db.OtpLogs
			where !x.IsUsed && x.MobileNumber == m && x.Purpose == p && x.ExpiresAt > DateTime.UtcNow
			orderby x.GeneratedAt descending
			select x), ct);
		if (row == null || row.OTPCode != otp.Trim())
		{
			return false;
		}
		row.IsUsed = true;
		row.UsedAt = DateTime.UtcNow;
		await ((DbContext)_db).SaveChangesAsync(ct);
		return true;
	}

	public async Task<OfficePortalAccessVm?> GetOfficePortalAccessAsync(int officeUserId, CancellationToken ct = default(CancellationToken))
	{
		OfficeUserEntity? user = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<OfficeUserEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<OfficeUserEntity>((IQueryable<OfficeUserEntity>)_db.OfficeUsers),
			(Expression<Func<OfficeUserEntity, bool>>)((OfficeUserEntity x) => x.OfficeUserID == officeUserId && x.IsActive),
			ct);
		if (user == null)
		{
			return null;
		}
		List<int> venueIds = await EntityFrameworkQueryableExtensions.ToListAsync<int>(
			Queryable.Distinct<int>(
				Queryable.Select<VenueUserMappingEntity, int>(
					Queryable.Where<VenueUserMappingEntity>(
						EntityFrameworkQueryableExtensions.AsNoTracking<VenueUserMappingEntity>((IQueryable<VenueUserMappingEntity>)_db.VenueUserMappings),
						(Expression<Func<VenueUserMappingEntity, bool>>)((VenueUserMappingEntity m) => m.OfficeUserID == officeUserId && m.IsActive)),
					(Expression<Func<VenueUserMappingEntity, int>>)((VenueUserMappingEntity m) => m.VenueID))),
			ct);
		return new OfficePortalAccessVm(user.OfficeUserID, user.RoleID, venueIds);
	}

	public async Task<IReadOnlyList<AdminBookingGridVm>> GetAdminBookingsForGridAsync(OfficePortalAccessVm access, CancellationToken ct = default(CancellationToken))
	{
		IQueryable<BookingRequestEntity> bookingsQ = EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests);
		if (!access.IsSuperAdmin)
		{
			if (access.VenueIds.Count == 0)
			{
				return Array.Empty<AdminBookingGridVm>();
			}
			IReadOnlyList<int> allowedVenues = access.VenueIds;
			bookingsQ = bookingsQ.Where((BookingRequestEntity b) => allowedVenues.Contains(b.VenueID));
			if (access.IsVerifyingAuthority)
			{
				bookingsQ = bookingsQ.Where((BookingRequestEntity b) =>
					string.IsNullOrEmpty(b.BookingStatus)
					|| b.BookingStatus == "Pending"
					|| (b.BookingStatus != null && EF.Functions.Like(b.BookingStatus, "%reject%")));
			}
			else if (access.IsAcceptingAuthority)
			{
				bookingsQ = bookingsQ.Where((BookingRequestEntity b) =>
					b.BookingStatus == "ForwardedToL2"
					|| b.BookingStatus == "Forwarded"
					|| b.BookingStatus == "Accepted"
					|| b.BookingStatus == "Confirmed"
					|| b.BookingStatus == "Approved"
					|| b.BookingStatus == "PaymentPending"
					|| (b.BookingStatus != null && EF.Functions.Like(b.BookingStatus, "%reject%")));
			}
		}
		return (await EntityFrameworkQueryableExtensions.ToListAsync((from b in bookingsQ
			join u in EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers) on b.UserID equals u.UserID
			join v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters) on b.VenueID equals v.VenueID
			join c in EntityFrameworkQueryableExtensions.AsNoTracking<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories) on b.CategoryID equals c.CategoryID into cj
			from c in cj.DefaultIfEmpty()
			join p in EntityFrameworkQueryableExtensions.AsNoTracking<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes) on b.PurposeID equals p.PurposeID into pj
			from p in pj.DefaultIfEmpty()
			orderby b.CreatedAt descending
			select new { b, u, v, c, p }).Take(500), ct)).Select(x => new AdminBookingGridVm(
			x.b.BookingID.ToString(),
			x.b.BookingRegNo,
			x.u.FullName ?? "",
			x.u.MobileNumber ?? "",
			x.u.Email ?? "",
			x.v.VenueName ?? "",
			(x.u.UserAddress ?? "").Trim(),
			x.b.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture),
			(x.c != null) ? x.c.CategoryName : "",
			(x.p != null) ? x.p.PurposeName : "",
			x.b.BookingFromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			x.b.BookingToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			x.b.BookingStatus ?? "",
			x.b.Level2UserID)).ToList();
	}

	public async Task<DashboardActivityBundleVm> GetRecentDashboardActivityAsync(OfficePortalAccessVm access, int take, CancellationToken ct = default(CancellationToken))
	{
		take = Math.Clamp(take, 1, 40);
		if (!access.IsSuperAdmin && (access.VenueIds == null || access.VenueIds.Count == 0))
		{
			return new DashboardActivityBundleVm(Array.Empty<DashboardActivityItemVm>(), Array.Empty<DashboardActivityItemVm>());
		}
		IQueryable<BookingRequestEntity> bookingQ = EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests);
		if (!access.IsSuperAdmin)
		{
			IReadOnlyList<int> vid = access.VenueIds;
			bookingQ = bookingQ.Where((BookingRequestEntity b) => vid.Contains(b.VenueID));
		}
		var raw = await (from l in _db.BookingStatusLogs.AsNoTracking()
			join b in bookingQ on l.BookingID equals b.BookingID
			join v in _db.VenueMasters.AsNoTracking() on b.VenueID equals v.VenueID
			join ur in _db.RegisteredUsers.AsNoTracking() on b.UserID equals ur.UserID
			orderby l.ChangedAt descending
			select new
			{
				l,
				b,
				v,
				ur
			}).Take(500).ToListAsync(ct);
		List<int> ouIds = raw.Where(x => !string.Equals(x.l.ChangedByType, "Customer", StringComparison.OrdinalIgnoreCase) && x.l.ChangedByID != null).Select(x => x.l.ChangedByID!.Value).Distinct()
			.ToList();
		Dictionary<int, string> ouNames = new Dictionary<int, string>();
		if (ouIds.Count > 0)
		{
			ouNames = await _db.OfficeUsers.AsNoTracking().Where(o => ouIds.Contains(o.OfficeUserID))
				.ToDictionaryAsync(o => o.OfficeUserID, o => (o.FullName ?? "").Trim(), ct);
		}
		List<DashboardActivityItemVm> admin = new List<DashboardActivityItemVm>();
		List<DashboardActivityItemVm> customer = new List<DashboardActivityItemVm>();
		int ai = 0;
		int ci = 0;
		foreach (var x in raw)
		{
			bool isCust = string.Equals(x.l.ChangedByType, "Customer", StringComparison.OrdinalIgnoreCase);
			string regNo = x.b.BookingRegNo ?? "";
			string hall = (x.v.VenueName ?? "").Trim();
			string sub = regNo + " · " + hall;
			string who;
			if (isCust)
			{
				string fn = (x.ur.FullName ?? "").Trim();
				who = (fn.Length == 0) ? "Customer" : FirstToken(fn);
			}
			else if (x.l.ChangedByID != null && ouNames.TryGetValue(x.l.ChangedByID.Value, out string nm) && nm.Length > 0)
			{
				who = FirstToken(nm);
			}
			else
			{
				who = (x.l.ChangedByType ?? "Admin").Trim();
			}
			string line = who + " · " + StatusVerb(x.l.NewStatus, isCust);
			string timeLabel = FormatRelativeActivity(x.l.ChangedAt);
			if (isCust)
			{
				if (customer.Count < take)
				{
					customer.Add(new DashboardActivityItemVm(line, sub, timeLabel, AvatarTone(ci)));
					ci++;
				}
			}
			else if (admin.Count < take)
			{
				admin.Add(new DashboardActivityItemVm(line, sub, timeLabel, AvatarTone(ai)));
				ai++;
			}
			if (admin.Count >= take && customer.Count >= take)
			{
				break;
			}
		}
		return new DashboardActivityBundleVm(admin, customer);
	}

	private static string FirstToken(string s)
	{
		string[] parts = s.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		return (parts.Length == 0) ? s : parts[0];
	}

	private static string StatusVerb(string newStatus, bool customer)
	{
		string sl = (newStatus ?? "").Trim().ToLowerInvariant();
		if (sl.Contains("accept") || sl.Contains("approv") || sl.Contains("confirm") || sl == "paymentpending")
		{
			return "Approved booking";
		}
		if (sl.Contains("forward"))
		{
			return "Forwarded booking";
		}
		if (sl.Contains("reject"))
		{
			return "Rejected";
		}
		if (customer)
		{
			return "New submission";
		}
		return "Updated booking";
	}

	private static string AvatarTone(int i)
	{
		return (i % 4) switch
		{
			0 => "b",
			1 => "g",
			2 => "o",
			_ => "s",
		};
	}

	private static string FormatRelativeActivity(DateTime t)
	{
		DateTime now = DateTime.UtcNow;
		if (t.Kind == DateTimeKind.Unspecified)
		{
			t = DateTime.SpecifyKind(t, DateTimeKind.Utc);
		}
		TimeSpan diff = now - t.ToUniversalTime();
		if (diff.TotalMilliseconds < 0.0)
		{
			return "Upcoming";
		}
		if (diff.TotalMinutes < 1.0)
		{
			return "Just now";
		}
		if (diff.TotalMinutes < 60.0)
		{
			return (int)diff.TotalMinutes + " min ago";
		}
		if (diff.TotalHours < 24.0)
		{
			return (int)diff.TotalHours + " hr ago";
		}
		if (diff.TotalDays < 14.0)
		{
			return (int)diff.TotalDays + "d ago";
		}
		return t.ToUniversalTime().ToString("MMM d", CultureInfo.InvariantCulture);
	}

	public async Task<AdminBookingDetailVm?> GetAdminBookingDetailAsync(OfficePortalAccessVm access, int bookingId, CancellationToken ct = default(CancellationToken))
	{
		var row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(from b in EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests)
			join u in EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers) on b.UserID equals u.UserID
			join v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters) on b.VenueID equals v.VenueID
			join c in EntityFrameworkQueryableExtensions.AsNoTracking<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories) on b.CategoryID equals c.CategoryID into cj
			from c in cj.DefaultIfEmpty()
			join p in EntityFrameworkQueryableExtensions.AsNoTracking<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes) on b.PurposeID equals p.PurposeID into pj
			from p in pj.DefaultIfEmpty()
			where b.BookingID == bookingId
			select new { b, u, v, c, p }, ct);
		if (row == null)
		{
			return null;
		}
		if (!access.IsSuperAdmin && (access.VenueIds.Count == 0 || !access.VenueIds.Contains(row.b.VenueID)))
		{
			return null;
		}
		List<BookingStatusLogEntity> logRows = await EntityFrameworkQueryableExtensions.ToListAsync(
			from l in EntityFrameworkQueryableExtensions.AsNoTracking<BookingStatusLogEntity>((IQueryable<BookingStatusLogEntity>)_db.BookingStatusLogs)
			where l.BookingID == bookingId
			orderby l.ChangedAt
			select l,
			ct);
		List<BookingStatusLogEntryVm> history = logRows.ConvertAll((BookingStatusLogEntity l) => new BookingStatusLogEntryVm(
			l.LogID,
			l.ChangedByType ?? "",
			l.ChangedByID,
			l.OldStatus,
			l.NewStatus ?? "",
			l.Remarks,
			l.ChangedAt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)));
		return new AdminBookingDetailVm(
			row.b.BookingID,
			row.b.BookingRegNo,
			row.b.VenueID,
			row.v.VenueName ?? "",
			row.b.CategoryID,
			row.c?.CategoryName ?? "",
			row.c?.IdentityLabel ?? "ID number",
			row.c?.DocumentLabel ?? "ID document",
			row.b.PurposeID,
			row.p?.PurposeName ?? "",
			row.p?.MaxDays ?? 1,
			row.b.IdentityNumber ?? "",
			row.b.DocumentPath ?? "",
			row.b.BankName ?? "",
			row.b.AccountNumber ?? "",
			row.b.IFSCCode ?? "",
			row.b.TotalAmount,
			row.b.BookingStatus ?? "",
			row.b.Level1UserID,
			row.b.Level2UserID,
			row.u.FullName ?? "",
			row.u.MobileNumber ?? "",
			row.u.Email,
			row.u.UserAddress,
			row.b.BookingFromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			row.b.BookingToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			history);
	}

	public async Task ExecuteL1BookingActionAsync(OfficePortalAccessVm access, int bookingId, string action, string? remarks, CancellationToken ct = default(CancellationToken))
	{
		await EnsureOfficeUserCanAccessBookingAsync(access, bookingId, ct);
		if (!access.IsSuperAdmin && !access.IsVerifyingAuthority)
		{
			throw new UnauthorizedAccessException("Only verifying authority or super admin can perform this action.");
		}
		string a = (action ?? "").Trim();
		if (!string.Equals(a, "Forward", StringComparison.OrdinalIgnoreCase) && !string.Equals(a, "Reject", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Invalid action. Use Forward or Reject.");
		}
		BookingRequestEntity? bk = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingRequestEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests),
			(Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity x) => x.BookingID == bookingId),
			ct);
		bool provisionalReject = bk != null
			&& string.Equals(a, "Reject", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(bk.BookingStatus, "Pending", StringComparison.Ordinal)
			&& bk.Level2UserID != null;
		if (string.Equals(a, "Reject", StringComparison.OrdinalIgnoreCase) && !provisionalReject && string.IsNullOrWhiteSpace(remarks))
		{
			throw new InvalidOperationException("Please add a reject reason.");
		}
		string spAction = string.Equals(a, "Forward", StringComparison.OrdinalIgnoreCase) ? "Forward" : "Reject";
		string r = (remarks ?? "").Trim();
		if (r.Length > 500)
		{
			r = r.Substring(0, 500);
		}
		string? oldStatus = bk?.BookingStatus;
		await _db.Database.ExecuteSqlRawAsync(
			"EXEC sp_L1ForwardOrReject @BookingID, @L1UserID, @Action, @Remarks",
			new object[]
			{
				new SqlParameter("@BookingID", bookingId),
				new SqlParameter("@L1UserID", access.OfficeUserID),
				new SqlParameter("@Action", spAction),
				new SqlParameter("@Remarks", string.IsNullOrEmpty(r) ? DBNull.Value : r),
			},
			ct);
		BookingRequestEntity? after = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingRequestEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests),
			(Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity x) => x.BookingID == bookingId),
			ct);
		string byType = access.IsSuperAdmin ? "Super Admin" : "Verifying Auth";
		_db.BookingStatusLogs.Add(new BookingStatusLogEntity
		{
			BookingID = bookingId,
			ChangedByType = byType,
			ChangedByID = access.OfficeUserID,
			OldStatus = oldStatus,
			NewStatus = after?.BookingStatus ?? "",
			Remarks = string.IsNullOrEmpty(r) ? null : r,
			ChangedAt = DateTime.UtcNow,
		});
		await ((DbContext)_db).SaveChangesAsync(ct);
	}

	public async Task ExecuteL2BookingActionAsync(OfficePortalAccessVm access, int bookingId, string action, string? remarks, CancellationToken ct = default(CancellationToken))
	{
		await EnsureOfficeUserCanAccessBookingAsync(access, bookingId, ct);
		if (!access.IsSuperAdmin && !access.IsAcceptingAuthority)
		{
			throw new UnauthorizedAccessException("Only accepting authority or super admin can perform this action.");
		}
		BookingRequestEntity? b = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingRequestEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests),
			(Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity x) => x.BookingID == bookingId),
			ct);
		if (b == null)
		{
			throw new InvalidOperationException("Booking not found.");
		}
		if (!IsForwardedToL2Status(b.BookingStatus))
		{
			throw new InvalidOperationException("Booking is not forwarded for acceptance review.");
		}
		string? oldStatusL2 = b.BookingStatus;
		string a = (action ?? "").Trim();
		if (!string.Equals(a, "Return", StringComparison.OrdinalIgnoreCase) && !string.Equals(a, "Reject", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Invalid action. Use Return (provisionally approve back to L1) or Reject.");
		}
		if (string.Equals(a, "Reject", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(remarks))
		{
			throw new InvalidOperationException("Please add a reject reason.");
		}
		string spAction = string.Equals(a, "Return", StringComparison.OrdinalIgnoreCase) ? "Return" : "Reject";
		string r = (remarks ?? "").Trim();
		if (r.Length > 500)
		{
			r = r.Substring(0, 500);
		}
		await _db.Database.ExecuteSqlRawAsync(
			"EXEC sp_L2AcceptRejectReturn @BookingID, @L2UserID, @Action, @Remarks",
			new object[]
			{
				new SqlParameter("@BookingID", bookingId),
				new SqlParameter("@L2UserID", access.OfficeUserID),
				new SqlParameter("@Action", spAction),
				new SqlParameter("@Remarks", string.IsNullOrEmpty(r) ? DBNull.Value : r),
			},
			ct);
		BookingRequestEntity? afterL2 = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingRequestEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests),
			(Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity x) => x.BookingID == bookingId),
			ct);
		string byTypeL2 = access.IsSuperAdmin ? "Super Admin" : "Approving Auth";
		_db.BookingStatusLogs.Add(new BookingStatusLogEntity
		{
			BookingID = bookingId,
			ChangedByType = byTypeL2,
			ChangedByID = access.OfficeUserID,
			OldStatus = oldStatusL2,
			NewStatus = afterL2?.BookingStatus ?? "",
			Remarks = string.IsNullOrEmpty(r) ? null : r,
			ChangedAt = DateTime.UtcNow,
		});
		await ((DbContext)_db).SaveChangesAsync(ct);
	}

	public async Task ExecuteL1FinalApproveAsync(OfficePortalAccessVm access, L1FinalApproveVm body, CancellationToken ct = default(CancellationToken))
	{
		if (body == null)
		{
			throw new ArgumentNullException(nameof(body));
		}
		int bookingId = body.BookingID;
		await EnsureOfficeUserCanAccessBookingAsync(access, bookingId, ct);
		if (!access.IsSuperAdmin && !access.IsVerifyingAuthority)
		{
			throw new UnauthorizedAccessException("Only verifying authority or super admin can finally approve.");
		}
		string mode = (body.PaymentMode ?? "").Trim();
		string payStatus = (body.PaymentStatus ?? "").Trim();
		string refNo = (body.TransactionRefNo ?? "").Trim();
		if (mode.Length == 0 || mode.Length > 50)
		{
			throw new InvalidOperationException("Please select a payment mode.");
		}
		if (payStatus.Length == 0 || payStatus.Length > 30)
		{
			throw new InvalidOperationException("Please select a payment status.");
		}
		if (refNo.Length == 0 || refNo.Length > 100)
		{
			throw new InvalidOperationException("Please enter the transaction reference or cheque number.");
		}
		if (body.AmountPaid <= 0m)
		{
			throw new InvalidOperationException("Total amount received must be greater than zero.");
		}
		bool refTaken = await EntityFrameworkQueryableExtensions.AnyAsync<PaymentTransactionEntity>(
			(IQueryable<PaymentTransactionEntity>)_db.PaymentTransactions,
			(Expression<Func<PaymentTransactionEntity, bool>>)((PaymentTransactionEntity t) => t.TransactionRefNo == refNo),
			ct);
		if (refTaken)
		{
			throw new InvalidOperationException("This transaction reference is already recorded. Use a unique reference.");
		}
		BookingRequestEntity? b = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests, (Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity x) => x.BookingID == bookingId), ct);
		if (b == null)
		{
			throw new InvalidOperationException("Booking not found.");
		}
		if (!string.Equals(b.BookingStatus, "Pending", StringComparison.Ordinal) || b.Level2UserID == null)
		{
			throw new InvalidOperationException("Only provisionally reviewed (returned) bookings can be finally approved here.");
		}
		_db.PaymentTransactions.Add(new PaymentTransactionEntity
		{
			BookingID = bookingId,
			TransactionRefNo = refNo,
			AmountPaid = body.AmountPaid,
			PaymentMode = mode,
			PaymentStatus = payStatus,
			TransactionDate = DateTime.UtcNow,
			GatewayResponse = null
		});
		string old = b.BookingStatus;
		b.BookingStatus = "Accepted";
		b.UpdatedAt = DateTime.UtcNow;
		if (string.Equals(payStatus, "Success", StringComparison.OrdinalIgnoreCase))
		{
			b.PaymentStatus = "Paid";
		}
		_db.BookingStatusLogs.Add(new BookingStatusLogEntity
		{
			BookingID = bookingId,
			ChangedByType = "Level1",
			ChangedByID = access.OfficeUserID,
			OldStatus = old,
			NewStatus = "Accepted",
			Remarks = "Final approval after L2 provisional review; payment recorded.",
			ChangedAt = DateTime.UtcNow
		});
		try
		{
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
		catch (DbUpdateException ex)
		{
			if (ex.InnerException is SqlException sql && (sql.Number == 2627 || sql.Number == 2601))
			{
				throw new InvalidOperationException("This transaction reference is already in use.");
			}
			throw;
		}
		RegisteredUserEntity? reg = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<RegisteredUserEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<RegisteredUserEntity>((IQueryable<RegisteredUserEntity>)_db.RegisteredUsers),
			(Expression<Func<RegisteredUserEntity, bool>>)((RegisteredUserEntity x) => x.UserID == b.UserID),
			ct);
		VenueMasterEntity? vm = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters),
			(Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == b.VenueID),
			ct);
		if (reg != null && !string.IsNullOrWhiteSpace(reg.MobileNumber))
		{
			await _sms.NotifyBookingApprovedAsync(
				reg.MobileNumber,
				b.BookingRegNo,
				vm?.VenueName ?? "",
				ToDateOnly(b.BookingFromDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				ToDateOnly(b.BookingToDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				ct);
			string logMsg = "Booking approved — " + (b.BookingRegNo ?? "").Trim();
			if (logMsg.Length > 500)
			{
				logMsg = logMsg.Substring(0, 500);
			}
			await _db.Database.ExecuteSqlRawAsync(
				"EXEC sp_LogSMS @MobileNumber, @MessageText, @Purpose, @IsDelivered",
				new object[]
				{
					new SqlParameter("@MobileNumber", reg.MobileNumber.Trim()),
					new SqlParameter("@MessageText", logMsg),
					new SqlParameter("@Purpose", "BookingApproved"),
					new SqlParameter("@IsDelivered", false),
				},
				ct);
		}
	}

	public async Task<IReadOnlyList<VenueAdminRowVm>> GetAllVenuesAdminAsync(OfficePortalAccessVm access, CancellationToken ct = default(CancellationToken))
	{
		IQueryable<VenueMasterEntity> q = EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters);
		if (!access.IsSuperAdmin)
		{
			if (access.VenueIds.Count == 0)
			{
				return Array.Empty<VenueAdminRowVm>();
			}
			IReadOnlyList<int> allowedVenues = access.VenueIds;
			q = q.Where((VenueMasterEntity v) => allowedVenues.Contains(v.VenueID));
		}
		return (await EntityFrameworkQueryableExtensions.ToListAsync<VenueMasterEntity>((IQueryable<VenueMasterEntity>)(from v in q
			orderby v.VenueName
			select v), ct)).Select((VenueMasterEntity h) => new VenueAdminRowVm(h.VenueID, h.VenueTypeID, h.VenueName, h.VenueCode, h.Address, h.City, h.Division, h.GoogleMapLink, h.Facilities, h.IsActive, h.CreatedAt, h.Capacity, h.AreaSqMt, h.NoOfRooms, h.NoOfKitchen, h.NoOfToilet, h.NoOfBathroom)).ToList();
	}

	public async Task<int> UpsertVenueAsync(VenueMasterUpsertVm body, CancellationToken ct = default(CancellationToken))
	{
		int? venueID = body.VenueID;
		int vid = default(int);
		int num;
		if (venueID.HasValue)
		{
			vid = venueID.GetValueOrDefault();
			num = ((vid > 0) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		VenueMasterEntity h;
		if (num != 0)
		{
			h = await EntityFrameworkQueryableExtensions.FirstAsync<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters, (Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == vid), ct);
		}
		else
		{
			h = new VenueMasterEntity
			{
				CreatedAt = DateTime.UtcNow
			};
			_db.VenueMasters.Add(h);
		}
		int typeId = body.VenueTypeID;
		if (!await _db.VenueTypes.AsNoTracking().AnyAsync((VenueTypeEntity t) => t.VenueTypeID == typeId, ct))
		{
			int fallback = await _db.VenueTypes.AsNoTracking()
				.OrderBy((VenueTypeEntity t) => t.VenueTypeID)
				.Select((VenueTypeEntity t) => t.VenueTypeID)
				.FirstOrDefaultAsync(ct);
			if (fallback == 0)
			{
				throw new InvalidOperationException("No rows in VenueType. Add at least one venue type, or restart the API to seed a default.");
			}

			typeId = fallback;
		}

		h.VenueTypeID = typeId;
		h.VenueName = TruncateForVenue(body.VenueName, 100);
		h.VenueCode = TruncateForVenue(body.VenueCode, 10);
		h.Address = TruncateForVenue(body.Address, 255);
		h.City = TruncateForVenue(string.IsNullOrWhiteSpace(body.City) ? "Nagpur" : body.City.Trim(), 100);
		h.Division = TruncateForVenue(string.IsNullOrWhiteSpace(body.Division) ? "Nagpur" : body.Division.Trim(), 100);
		h.GoogleMapLink = TruncateForVenueNullable(body.GoogleMapLink, 500);
		h.IsActive = body.IsActive;
		MergeFacilitiesIntoVenue(h, body.Facilities);
		h.Capacity = body.Capacity;
		h.AreaSqMt = body.AreaSqMt;
		h.NoOfRooms = body.NoOfRooms;
		h.NoOfKitchen = body.NoOfKitchen;
		h.NoOfToilet = body.NoOfToilet;
		h.NoOfBathroom = body.NoOfBathroom;
		await ((DbContext)_db).SaveChangesAsync(ct);
		return h.VenueID;
	}

	private static string TruncateForVenue(string value, int maxLen)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "";
		}

		return value.Length <= maxLen ? value : value.Substring(0, maxLen);
	}

	private static string? TruncateForVenueNullable(string? value, int maxLen)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}

		return value.Length <= maxLen ? value : value.Substring(0, maxLen);
	}

	public async Task DeleteVenueAsync(int id, CancellationToken ct = default(CancellationToken))
	{
		VenueMasterEntity h = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters, (Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == id), ct);
		if (h != null)
		{
			h.IsActive = false;
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
	}

	public async Task<IReadOnlyList<RateChartLikeVm>> GetRateChartsAsync(OfficePortalAccessVm access, CancellationToken ct = default(CancellationToken))
	{
		IQueryable<VenueRentRuleEntity> rules = EntityFrameworkQueryableExtensions.AsNoTracking<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)_db.VenueRentRules);
		if (!access.IsSuperAdmin)
		{
			if (access.VenueIds.Count == 0)
			{
				return Array.Empty<RateChartLikeVm>();
			}
			IReadOnlyList<int> allowedVenues = access.VenueIds;
			rules = rules.Where((VenueRentRuleEntity r) => allowedVenues.Contains(r.VenueID));
		}
		return (await EntityFrameworkQueryableExtensions.ToListAsync(from r in rules
			join v in EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters) on r.VenueID equals v.VenueID
			join c in EntityFrameworkQueryableExtensions.AsNoTracking<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories) on r.CategoryID equals c.CategoryID into cj
			from c in cj.DefaultIfEmpty()
			orderby r.RuleID
			select new { r, v, c }, ct)).Select(x => new RateChartLikeVm(x.r.RuleID.ToString(CultureInfo.InvariantCulture), x.v.VenueName, (x.c != null) ? x.c.CategoryName : "", x.r.MaxDays.ToString(CultureInfo.InvariantCulture), x.r.MaxDays.ToString(CultureInfo.InvariantCulture), x.r.RentPerDay.ToString("0.##", CultureInfo.InvariantCulture), "", "")).ToList();
	}

	public async Task<int> UpsertRentRuleAsync(int venueId, VenueRentRuleVm body, CancellationToken ct = default(CancellationToken))
	{
		if (await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters), (Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == venueId), ct) == null)
		{
			throw new InvalidOperationException("Hall not found");
		}
		VenueRentRuleEntity row;
		if (body.RuleID > 0)
		{
			row = await EntityFrameworkQueryableExtensions.FirstAsync<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)_db.VenueRentRules, (Expression<Func<VenueRentRuleEntity, bool>>)((VenueRentRuleEntity x) => x.RuleID == body.RuleID && x.VenueID == venueId), ct);
		}
		else
		{
			row = new VenueRentRuleEntity();
			_db.VenueRentRules.Add(row);
		}
		row.VenueID = venueId;
		row.CategoryID = body.CategoryID;
		row.PurposeID = body.PurposeID;
		row.RentPerDay = body.RentPerDay;
		row.SecurityDeposit = body.SecurityDeposit;
		row.MaxDays = body.MaxDays;
		row.IsAllottable = body.IsAllottable;
		row.NotAllottableReason = body.NotAllottableReason;
		row.IsActive = body.IsActive;
		await ((DbContext)_db).SaveChangesAsync(ct);
		return row.RuleID;
	}

	public async Task DeleteRentRuleAsync(int ruleId, CancellationToken ct = default(CancellationToken))
	{
		VenueRentRuleEntity row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueRentRuleEntity>((IQueryable<VenueRentRuleEntity>)_db.VenueRentRules, (Expression<Func<VenueRentRuleEntity, bool>>)((VenueRentRuleEntity x) => x.RuleID == ruleId), ct);
		if (row != null)
		{
			_db.VenueRentRules.Remove(row);
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
	}

	public async Task<IReadOnlyList<BookingCategoryVm>> GetAllBookingCategoriesAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)(from x in EntityFrameworkQueryableExtensions.AsNoTracking<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories)
			orderby x.CategoryID
			select x), ct)).Select((BookingCategoryEntity c) => new BookingCategoryVm(c.CategoryID, c.CategoryName, c.IdentityLabel, c.IdentityFormat, c.DocumentLabel, c.IsActive)).ToList();
	}

	public async Task<int> UpsertBookingCategoryAsync(BookingCategoryUpsertVm body, CancellationToken ct = default(CancellationToken))
	{
		int? categoryID = body.CategoryID;
		int cid = default(int);
		int num;
		if (categoryID.HasValue)
		{
			cid = categoryID.GetValueOrDefault();
			num = ((cid > 0) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		BookingCategoryEntity row;
		if (num != 0)
		{
			row = await EntityFrameworkQueryableExtensions.FirstAsync<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories, (Expression<Func<BookingCategoryEntity, bool>>)((BookingCategoryEntity x) => x.CategoryID == cid), ct);
		}
		else
		{
			row = new BookingCategoryEntity();
			_db.BookingCategories.Add(row);
		}
		row.CategoryName = body.CategoryName;
		row.IdentityLabel = body.IdentityLabel;
		row.IdentityFormat = body.IdentityFormat;
		row.DocumentLabel = body.DocumentLabel;
		row.IsActive = body.IsActive;
		await ((DbContext)_db).SaveChangesAsync(ct);
		return row.CategoryID;
	}

	public async Task DeleteBookingCategoryAsync(int id, CancellationToken ct = default(CancellationToken))
	{
		BookingCategoryEntity row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories, (Expression<Func<BookingCategoryEntity, bool>>)((BookingCategoryEntity x) => x.CategoryID == id), ct);
		if (row != null)
		{
			_db.BookingCategories.Remove(row);
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
	}

	public async Task<IReadOnlyList<BookingPurposeVm>> GetAllBookingPurposesAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)(from p in EntityFrameworkQueryableExtensions.AsNoTracking<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes)
			orderby p.PurposeID
			select p), ct)).Select((BookingPurposeEntity p) => new BookingPurposeVm(p.PurposeID, p.PurposeName, p.MaxDays, p.IsActive)).ToList();
	}

	public async Task<int> UpsertBookingPurposeAsync(BookingPurposeUpsertVm body, CancellationToken ct = default(CancellationToken))
	{
		string name = (body.PurposeName ?? "").Trim();
		if (name.Length == 0)
		{
			throw new InvalidOperationException("Purpose name is required.");
		}

		if (name.Length > 150)
		{
			name = name.Substring(0, 150);
		}

		int maxDays = body.MaxDays < 1 ? 1 : (body.MaxDays > 365 ? 365 : body.MaxDays);
		if (body.PurposeID.HasValue && body.PurposeID.Value > 0)
		{
			int pid = body.PurposeID.Value;
			BookingPurposeEntity row = await EntityFrameworkQueryableExtensions.FirstAsync<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes, (Expression<Func<BookingPurposeEntity, bool>>)((BookingPurposeEntity p) => p.PurposeID == pid), ct);
			row.PurposeName = name;
			row.MaxDays = maxDays;
			row.IsActive = body.IsActive;
			await ((DbContext)_db).SaveChangesAsync(ct);
			return pid;
		}

		BookingPurposeEntity inserted = new BookingPurposeEntity
		{
			PurposeName = name,
			MaxDays = maxDays,
			IsActive = body.IsActive
		};
		_db.BookingPurposes.Add(inserted);
		await ((DbContext)_db).SaveChangesAsync(ct);
		return inserted.PurposeID;
	}

	public async Task DeleteBookingPurposeAsync(int id, CancellationToken ct = default(CancellationToken))
	{
		BookingPurposeEntity row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes, (Expression<Func<BookingPurposeEntity, bool>>)((BookingPurposeEntity p) => p.PurposeID == id), ct);
		if (row != null)
		{
			_db.BookingPurposes.Remove(row);
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
	}

	public async Task<IReadOnlyList<AdvertisementVm>> GetAllAdvertisementsAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<AdvertisementEntity>((IQueryable<AdvertisementEntity>)(from x in EntityFrameworkQueryableExtensions.AsNoTracking<AdvertisementEntity>((IQueryable<AdvertisementEntity>)_db.Advertisements)
			orderby x.AdID
			select x), ct)).Select((AdvertisementEntity a) => new AdvertisementVm(a.AdID, a.AdTitle, a.AdImagePath, a.AdURL, ToDateOnly(a.StartDate), ToDateOnly(a.EndDate), a.IsActive)).ToList();
	}

	public async Task<int> UpsertAdvertisementAsync(AdvertisementUpsertVm body, CancellationToken ct = default(CancellationToken))
	{
		int? adID = body.AdID;
		int aid = default(int);
		int num;
		if (adID.HasValue)
		{
			aid = adID.GetValueOrDefault();
			num = ((aid > 0) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		AdvertisementEntity row;
		if (num != 0)
		{
			row = await EntityFrameworkQueryableExtensions.FirstAsync<AdvertisementEntity>((IQueryable<AdvertisementEntity>)_db.Advertisements, (Expression<Func<AdvertisementEntity, bool>>)((AdvertisementEntity x) => x.AdID == aid), ct);
		}
		else
		{
			row = new AdvertisementEntity
			{
				CreatedAt = DateTime.UtcNow
			};
			_db.Advertisements.Add(row);
		}
		row.AdTitle = body.AdTitle;
		row.AdImagePath = body.AdImagePath;
		row.AdURL = body.AdURL;
		row.StartDate = body.StartDate.ToDateTime(TimeOnly.MinValue);
		row.EndDate = body.EndDate.ToDateTime(TimeOnly.MinValue);
		row.IsActive = body.IsActive;
		await ((DbContext)_db).SaveChangesAsync(ct);
		return row.AdID;
	}

	public async Task DeleteAdvertisementAsync(int adId, CancellationToken ct = default(CancellationToken))
	{
		AdvertisementEntity row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<AdvertisementEntity>((IQueryable<AdvertisementEntity>)_db.Advertisements, (Expression<Func<AdvertisementEntity, bool>>)((AdvertisementEntity x) => x.AdID == adId), ct);
		if (row != null)
		{
			_db.Advertisements.Remove(row);
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
	}

	public async Task<IReadOnlyList<TextAdvertisementVm>> GetTextAdvertisementsPublicAsync(DateOnly onDate, CancellationToken ct = default(CancellationToken))
	{
		DateTime d = onDate.ToDateTime(TimeOnly.MinValue);
		return (await EntityFrameworkQueryableExtensions.ToListAsync<TextAdvertisementEntity>((IQueryable<TextAdvertisementEntity>)(from x in EntityFrameworkQueryableExtensions.AsNoTracking<TextAdvertisementEntity>((IQueryable<TextAdvertisementEntity>)_db.TextAdvertisements)
			where x.IsActive && x.StartDate <= d && x.EndDate >= d
			orderby x.TextAdID
			select x), ct)).Select((TextAdvertisementEntity t) => new TextAdvertisementVm(t.TextAdID, t.Advertise, ToDateOnly(t.StartDate), ToDateOnly(t.EndDate), t.IsActive)).ToList();
	}

	public async Task<IReadOnlyList<TextAdvertisementVm>> GetAllTextAdvertisementsAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<TextAdvertisementEntity>((IQueryable<TextAdvertisementEntity>)(from x in EntityFrameworkQueryableExtensions.AsNoTracking<TextAdvertisementEntity>((IQueryable<TextAdvertisementEntity>)_db.TextAdvertisements)
			orderby x.TextAdID
			select x), ct)).Select((TextAdvertisementEntity t) => new TextAdvertisementVm(t.TextAdID, t.Advertise, ToDateOnly(t.StartDate), ToDateOnly(t.EndDate), t.IsActive)).ToList();
	}

	public async Task<int> UpsertTextAdvertisementAsync(TextAdvertisementUpsertVm body, CancellationToken ct = default(CancellationToken))
	{
		AdsSchemaGuard.EnsureTextAdvertisementAndImageBanner(_db);
		int? textAdID = body.TextAdID;
		int tid = default(int);
		int num2;
		if (textAdID.HasValue)
		{
			tid = textAdID.GetValueOrDefault();
			num2 = ((tid > 0) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		TextAdvertisementEntity row2;
		if (num2 != 0)
		{
			row2 = await EntityFrameworkQueryableExtensions.FirstAsync<TextAdvertisementEntity>((IQueryable<TextAdvertisementEntity>)_db.TextAdvertisements, (Expression<Func<TextAdvertisementEntity, bool>>)((TextAdvertisementEntity x) => x.TextAdID == tid), ct);
		}
		else
		{
			row2 = new TextAdvertisementEntity
			{
				CreatedAt = DateTime.UtcNow
			};
			_db.TextAdvertisements.Add(row2);
		}
		row2.Advertise = body.Advertise ?? "";
		row2.StartDate = body.StartDate.ToDateTime(TimeOnly.MinValue);
		row2.EndDate = body.EndDate.ToDateTime(TimeOnly.MinValue);
		row2.IsActive = body.IsActive;
		try
		{
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
		catch (DbUpdateException ex)
		{
			_log.LogWarning(ex, "TextAdvertisement SaveChanges failed; re-applying schema DDL and retrying once.");
			AdsSchemaGuard.EnsureTextAdvertisementAndImageBanner(_db);
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
		return row2.TextAdID;
	}

	public async Task DeleteTextAdvertisementAsync(int textAdId, CancellationToken ct = default(CancellationToken))
	{
		TextAdvertisementEntity row3 = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<TextAdvertisementEntity>((IQueryable<TextAdvertisementEntity>)_db.TextAdvertisements, (Expression<Func<TextAdvertisementEntity, bool>>)((TextAdvertisementEntity x) => x.TextAdID == textAdId), ct);
		if (row3 != null)
		{
			_db.TextAdvertisements.Remove(row3);
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
	}

	public async Task<IReadOnlyList<ImageBannerVm>> GetImageBannersPublicAsync(DateOnly onDate, CancellationToken ct = default(CancellationToken))
	{
		DateTime d = onDate.ToDateTime(TimeOnly.MinValue);
		return (await EntityFrameworkQueryableExtensions.ToListAsync<ImageBannerEntity>((IQueryable<ImageBannerEntity>)(from x in EntityFrameworkQueryableExtensions.AsNoTracking<ImageBannerEntity>((IQueryable<ImageBannerEntity>)_db.ImageBanners)
			where x.IsActive && x.StartDate <= d && x.EndDate >= d
			orderby x.ImgId
			select x), ct)).Select((ImageBannerEntity x) => new ImageBannerVm(x.ImgId, x.ImgPath, x.ImgURL, ToDateOnly(x.StartDate), ToDateOnly(x.EndDate), x.IsActive)).ToList();
	}

	public async Task<IReadOnlyList<ImageBannerVm>> GetAllImageBannersAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await EntityFrameworkQueryableExtensions.ToListAsync<ImageBannerEntity>((IQueryable<ImageBannerEntity>)(from x in EntityFrameworkQueryableExtensions.AsNoTracking<ImageBannerEntity>((IQueryable<ImageBannerEntity>)_db.ImageBanners)
			orderby x.ImgId
			select x), ct)).Select((ImageBannerEntity x) => new ImageBannerVm(x.ImgId, x.ImgPath, x.ImgURL, ToDateOnly(x.StartDate), ToDateOnly(x.EndDate), x.IsActive)).ToList();
	}

	public async Task<int> UpsertImageBannerAsync(ImageBannerUpsertVm body, CancellationToken ct = default(CancellationToken))
	{
		int? imgId = body.ImgId;
		int iid = default(int);
		int num3;
		if (imgId.HasValue)
		{
			iid = imgId.GetValueOrDefault();
			num3 = ((iid > 0) ? 1 : 0);
		}
		else
		{
			num3 = 0;
		}
		ImageBannerEntity rowB;
		if (num3 != 0)
		{
			rowB = await EntityFrameworkQueryableExtensions.FirstAsync<ImageBannerEntity>((IQueryable<ImageBannerEntity>)_db.ImageBanners, (Expression<Func<ImageBannerEntity, bool>>)((ImageBannerEntity x) => x.ImgId == iid), ct);
		}
		else
		{
			rowB = new ImageBannerEntity
			{
				CreatedAt = DateTime.UtcNow
			};
			_db.ImageBanners.Add(rowB);
		}
		rowB.ImgPath = body.ImgPath;
		rowB.ImgURL = body.ImgURL;
		rowB.StartDate = body.StartDate.ToDateTime(TimeOnly.MinValue);
		rowB.EndDate = body.EndDate.ToDateTime(TimeOnly.MinValue);
		rowB.IsActive = body.IsActive;
		await ((DbContext)_db).SaveChangesAsync(ct);
		return rowB.ImgId;
	}

	public async Task DeleteImageBannerAsync(int imgId, CancellationToken ct = default(CancellationToken))
	{
		ImageBannerEntity rowD = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<ImageBannerEntity>((IQueryable<ImageBannerEntity>)_db.ImageBanners, (Expression<Func<ImageBannerEntity, bool>>)((ImageBannerEntity x) => x.ImgId == imgId), ct);
		if (rowD != null)
		{
			_db.ImageBanners.Remove(rowD);
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
	}

	public async Task<IReadOnlyList<OfficeUserRoleVm>> GetOfficeUserRolesAsync(CancellationToken ct = default(CancellationToken))
	{
		return await _db.OfficeUserRoles.AsNoTracking()
			.OrderBy((OfficeUserRoleEntity r) => r.RoleID)
			.Select((OfficeUserRoleEntity r) => new OfficeUserRoleVm(r.RoleID, r.RoleName ?? ""))
			.ToListAsync(ct);
	}

	public async Task<IReadOnlyList<OfficeUserVm>> GetOfficeUsersAsync(CancellationToken ct = default(CancellationToken))
	{
		List<OfficeUserEntity> users = await _db.OfficeUsers.AsNoTracking()
			.OrderBy((OfficeUserEntity x) => x.OfficeUserID)
			.ToListAsync(ct);
		if (users.Count == 0)
		{
			return Array.Empty<OfficeUserVm>();
		}

		List<int> userIds = users.ConvertAll((OfficeUserEntity u) => u.OfficeUserID);
		List<int> roleIds = users.Where((OfficeUserEntity u) => u.RoleID > 0).Select((OfficeUserEntity u) => u.RoleID).Distinct().ToList();
		Dictionary<int, string> roleNameById = (roleIds.Count == 0)
			? new Dictionary<int, string>()
			: await _db.OfficeUserRoles.AsNoTracking()
				.Where((OfficeUserRoleEntity r) => roleIds.Contains(r.RoleID))
				.ToDictionaryAsync((OfficeUserRoleEntity r) => r.RoleID, (OfficeUserRoleEntity r) => r.RoleName ?? "", ct);

		var mappingRows = await _db.VenueUserMappings.AsNoTracking()
			.Where((VenueUserMappingEntity m) => userIds.Contains(m.OfficeUserID) && m.IsActive)
			.Select((VenueUserMappingEntity m) => new { m.OfficeUserID, m.VenueID })
			.ToListAsync(ct);
		Dictionary<int, List<int>> venueByUser = mappingRows
			.GroupBy((m) => m.OfficeUserID)
			.ToDictionary((g) => g.Key, (g) => g.Select((x) => x.VenueID).Distinct().ToList());

		List<OfficeUserVm> list = new List<OfficeUserVm>(users.Count);
		foreach (OfficeUserEntity u in users)
		{
			IReadOnlyList<int> vids = venueByUser.TryGetValue(u.OfficeUserID, out List<int>? vl)
				? vl
				: Array.Empty<int>();
			string? rn = (u.RoleID > 0 && roleNameById.TryGetValue(u.RoleID, out string? name)) ? name : null;
			list.Add(MapOfficeUser(u, vids, rn));
		}

		return list;
	}

	public async Task<OfficeUserVm?> GetOfficeUserAsync(int id, CancellationToken ct = default(CancellationToken))
	{
		OfficeUserEntity? row = await _db.OfficeUsers.AsNoTracking().FirstOrDefaultAsync((OfficeUserEntity x) => x.OfficeUserID == id, ct);
		if (row == null)
		{
			return null;
		}

		List<int> vids = await _db.VenueUserMappings.AsNoTracking()
			.Where((VenueUserMappingEntity m) => m.OfficeUserID == id && m.IsActive)
			.Select((VenueUserMappingEntity m) => m.VenueID)
			.Distinct()
			.ToListAsync(ct);

		string? roleName = null;
		if (row.RoleID > 0)
		{
			OfficeUserRoleEntity? rr = await _db.OfficeUserRoles.AsNoTracking()
				.FirstOrDefaultAsync((OfficeUserRoleEntity r) => r.RoleID == row.RoleID, ct);
			roleName = rr?.RoleName;
		}

		return MapOfficeUser(row, vids, roleName);
	}

	public async Task<bool> AnyActiveSuperAdminExistsAsync(CancellationToken ct = default(CancellationToken))
	{
		return await _db.OfficeUsers.AsNoTracking()
			.AnyAsync((OfficeUserEntity u) => u.IsActive && u.RoleID == 1, ct);
	}

	public async Task<SuperAdminProvisionResult> TryProvisionFirstSuperAdminAsync(
		byte[] tokenHash,
		byte[]? requestIpFingerprint,
		BootstrapSuperAdminRequest request,
		CancellationToken ct = default(CancellationToken))
	{
		if (tokenHash == null || tokenHash.Length != 32)
		{
			return new SuperAdminProvisionResult(false, 0, SuperAdminProvisionFailure.InvalidOrExpiredToken, null);
		}

		if (request == null ||
		    string.IsNullOrWhiteSpace(request.FullName) ||
		    string.IsNullOrWhiteSpace(request.Username) ||
		    string.IsNullOrWhiteSpace(request.Password))
		{
			return new SuperAdminProvisionResult(false, 0, SuperAdminProvisionFailure.Validation, "required_fields");
		}

		if (request.Password.Length < 8)
		{
			return new SuperAdminProvisionResult(false, 0, SuperAdminProvisionFailure.Validation, "password_length");
		}

		DbContext ctx = (DbContext)_db;
		await using IDbContextTransaction transaction = await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
		try
		{
			await OfficeUserRoleBootstrapGuard.EnsureMinimumRolesAsync(_db, ct);

			DateTime now = DateTime.UtcNow;
			SqlParameter pHash = new("h", System.Data.SqlDbType.VarBinary, 32) { Value = tokenHash };
			SqlParameter pNow = new("now", System.Data.SqlDbType.DateTime2) { Value = now };
			SuperAdminProvisioningTokenEntity? token = await _db.SuperAdminProvisioningTokens
				.FromSqlRaw(
					"""
					SELECT * FROM dbo.SuperAdminProvisioningToken WITH (UPDLOCK, ROWLOCK)
					WHERE TokenHash = @h AND UsedAtUtc IS NULL AND ExpiresAtUtc > @now
					""",
					pHash,
					pNow)
				.AsTracking()
				.FirstOrDefaultAsync(ct);

			if (token == null)
			{
				await transaction.RollbackAsync(ct);
				return new SuperAdminProvisionResult(false, 0, SuperAdminProvisionFailure.InvalidOrExpiredToken, null);
			}

			if (token.BoundIpFingerprint is { Length: > 0 } bound)
			{
				if (requestIpFingerprint == null || requestIpFingerprint.Length != 32 ||
				    !ProvisioningCrypto.FixedTimeEquals(bound, requestIpFingerprint))
				{
					await transaction.RollbackAsync(ct);
					return new SuperAdminProvisionResult(false, 0, SuperAdminProvisionFailure.IpNotAllowed, null);
				}
			}

			bool superExists = await _db.OfficeUsers
				.AnyAsync((OfficeUserEntity u) => u.IsActive && u.RoleID == 1, ct);
			if (superExists)
			{
				await transaction.RollbackAsync(ct);
				return new SuperAdminProvisionResult(false, 0, SuperAdminProvisionFailure.SuperAdminAlreadyExists, null);
			}

			var create = new OfficeUserCreateVm(
				request.FullName.Trim(),
				request.Username.Trim(),
				request.Password,
				Role: null,
				RoleID: 1,
				MobileNumber: string.IsNullOrWhiteSpace(request.MobileNumber) ? null : request.MobileNumber.Trim(),
				EmailID: string.IsNullOrWhiteSpace(request.EmailID) ? null : request.EmailID.Trim(),
				VenueIDs: Array.Empty<int>());

			int officeUserId;
			try
			{
				officeUserId = await CreateOfficeUserWithinTransactionAsync(create, ct);
			}
			catch (ArgumentException)
			{
				await transaction.RollbackAsync(ct);
				return new SuperAdminProvisionResult(false, 0, SuperAdminProvisionFailure.Validation, "office_user");
			}
			catch (DbUpdateException ex)
			{
				_log.LogWarning(ex, "Provision first super admin: database rejected insert (e.g. duplicate username).");
				await transaction.RollbackAsync(ct);
				return new SuperAdminProvisionResult(false, 0, SuperAdminProvisionFailure.Validation, "db_update");
			}

			token.UsedAtUtc = DateTime.UtcNow;
			await ctx.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
			return new SuperAdminProvisionResult(true, officeUserId, SuperAdminProvisionFailure.None, null);
		}
		catch
		{
			await transaction.RollbackAsync(ct);
			throw;
		}
	}

	public async Task MintSuperAdminProvisioningTokenAsync(byte[] tokenHash, DateTime expiresAtUtc, byte[]? boundIpFingerprint, CancellationToken ct = default(CancellationToken))
	{
		DateTime created = DateTime.UtcNow;
		_db.SuperAdminProvisioningTokens.Add(new SuperAdminProvisioningTokenEntity
		{
			TokenHash = tokenHash,
			CreatedAtUtc = created,
			ExpiresAtUtc = expiresAtUtc,
			UsedAtUtc = null,
			BoundIpFingerprint = boundIpFingerprint,
		});
		await ((DbContext)_db).SaveChangesAsync(ct);
	}

	/// <summary>Creates an office user and venue mappings; caller must own an active transaction.</summary>
	private async Task<int> CreateOfficeUserWithinTransactionAsync(OfficeUserCreateVm body, CancellationToken ct)
	{
		if (!body.RoleID.HasValue || body.RoleID.Value <= 0)
		{
			throw new ArgumentException("RoleID must reference a row in OfficeUserRole.", nameof(body));
		}

		int roleId = body.RoleID.Value;
		OfficeUserRoleEntity? resolvedRole = await _db.OfficeUserRoles.AsNoTracking()
			.FirstOrDefaultAsync((OfficeUserRoleEntity r) => r.RoleID == roleId, ct);
		if (resolvedRole == null)
		{
			throw new ArgumentException("RoleID does not exist in OfficeUserRole.", nameof(body));
		}

		DbContext ctx = (DbContext)_db;
		string hash = BCrypt.Net.BCrypt.HashPassword(body.Password);

		OfficeUserEntity row = new OfficeUserEntity
		{
			FullName = body.FullName,
			Username = body.Username.Trim(),
			PasswordHash = hash,
			RoleID = roleId,
			MobileNumber = body.MobileNumber,
			EmailID = body.EmailID,
			CreatedAt = DateTime.UtcNow,
			IsActive = true
		};
		_db.OfficeUsers.Add(row);
		await ctx.SaveChangesAsync(ct);

		IReadOnlyList<int> venueIds = body.VenueIDs ?? Array.Empty<int>();
		foreach (int vid in venueIds.Distinct())
		{
			if (vid <= 0)
			{
				continue;
			}

			bool venueExists = await _db.VenueMasters.AsNoTracking()
				.AnyAsync((VenueMasterEntity v) => v.VenueID == vid, ct);
			if (!venueExists)
			{
				continue;
			}

			_db.VenueUserMappings.Add(new VenueUserMappingEntity
			{
				VenueID = vid,
				OfficeUserID = row.OfficeUserID,
				RoleLevel = roleId,
				IsActive = true
			});
		}

		await ctx.SaveChangesAsync(ct);
		return row.OfficeUserID;
	}

	public async Task<int> CreateOfficeUserAsync(OfficeUserCreateVm body, CancellationToken ct = default(CancellationToken))
	{
		DbContext ctx = (DbContext)_db;
		await using IDbContextTransaction transaction = await ctx.Database.BeginTransactionAsync(ct);
		try
		{
			int id = await CreateOfficeUserWithinTransactionAsync(body, ct);
			await transaction.CommitAsync(ct);
			return id;
		}
		catch
		{
			await transaction.RollbackAsync(ct);
			throw;
		}
	}

	public async Task UpdateOfficeUserAsync(int id, OfficeUserUpdateVm body, CancellationToken ct = default(CancellationToken))
	{
		DbContext ctx = (DbContext)_db;
		await using IDbContextTransaction transaction = await ctx.Database.BeginTransactionAsync(ct);
		try
		{
			OfficeUserEntity row = await EntityFrameworkQueryableExtensions.FirstAsync<OfficeUserEntity>((IQueryable<OfficeUserEntity>)_db.OfficeUsers, (Expression<Func<OfficeUserEntity, bool>>)((OfficeUserEntity x) => x.OfficeUserID == id), ct);
			if (body.FullName != null)
			{
				row.FullName = body.FullName;
			}
			if (body.Password != null)
			{
				row.PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password);
			}
			if (body.RoleID.HasValue)
			{
				OfficeUserRoleEntity? roleRow = await _db.OfficeUserRoles.AsNoTracking()
					.FirstOrDefaultAsync((OfficeUserRoleEntity r) => r.RoleID == body.RoleID.Value, ct);
				if (roleRow != null)
				{
					row.RoleID = body.RoleID.Value;
				}
			}
			if (body.MobileNumber != null)
			{
				row.MobileNumber = body.MobileNumber;
			}
			if (body.EmailID != null)
			{
				row.EmailID = body.EmailID;
			}
			if (body.IsActive == false)
			{
				row.IsActive = false;
			}
			else if (body.IsActive == true)
			{
				row.IsActive = true;
			}
			if (body.VenueIDs != null)
			{
				List<VenueUserMappingEntity> existing = await _db.VenueUserMappings.Where((VenueUserMappingEntity m) => m.OfficeUserID == id).ToListAsync(ct);
				_db.VenueUserMappings.RemoveRange(existing);
				foreach (int vid in body.VenueIDs.Distinct())
				{
					if (vid <= 0)
					{
						continue;
					}

					bool venueExists = await _db.VenueMasters.AsNoTracking()
						.AnyAsync((VenueMasterEntity v) => v.VenueID == vid, ct);
					if (!venueExists)
					{
						continue;
					}

					_db.VenueUserMappings.Add(new VenueUserMappingEntity
					{
						VenueID = vid,
						OfficeUserID = id,
						RoleLevel = row.RoleID,
						IsActive = true
					});
				}
			}

			await ctx.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
		}
		catch
		{
			await transaction.RollbackAsync(ct);
			throw;
		}
	}

	public async Task DeactivateOfficeUserAsync(int id, CancellationToken ct = default(CancellationToken))
	{
		OfficeUserEntity row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<OfficeUserEntity>((IQueryable<OfficeUserEntity>)_db.OfficeUsers, (Expression<Func<OfficeUserEntity, bool>>)((OfficeUserEntity x) => x.OfficeUserID == id), ct);
		if (row != null)
		{
			row.IsActive = false;
			await ((DbContext)_db).SaveChangesAsync(ct);
		}
	}

	public async Task<IReadOnlyList<AccountDetailsLikeVm>> GetBankAccountsAsync(CancellationToken ct = default(CancellationToken))
	{
		List<BankAccountDetailEntity> rows = await EntityFrameworkQueryableExtensions.ToListAsync<BankAccountDetailEntity>(
			Queryable.OrderBy<BankAccountDetailEntity, int>(
				EntityFrameworkQueryableExtensions.AsNoTracking<BankAccountDetailEntity>((IQueryable<BankAccountDetailEntity>)_db.BankAccountDetails),
				(Expression<Func<BankAccountDetailEntity, int>>)((BankAccountDetailEntity x) => x.BankId)),
			ct);
		List<AccountDetailsLikeVm> list = new List<AccountDetailsLikeVm>(rows.Count);
		foreach (BankAccountDetailEntity x in rows)
		{
			list.Add(new AccountDetailsLikeVm(
				x.BankId.ToString(CultureInfo.InvariantCulture),
				(x.Place ?? "").Trim(),
				(x.BankName ?? "").Trim(),
				(x.AccountNumber ?? "").Trim(),
				(x.BankAddress ?? "").Trim(),
				(x.IFSCCode ?? "").Trim(),
				(x.ContactName ?? "").Trim(),
				(x.MobileNumber ?? "").Trim(),
				(x.ChequeInFavour ?? "").Trim()));
		}
		return list;
	}

	private async Task EnsureOfficeUserCanAccessBookingAsync(OfficePortalAccessVm access, int bookingId, CancellationToken ct)
	{
		if (access.IsSuperAdmin)
		{
			return;
		}
		BookingRequestEntity? row = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingRequestEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<BookingRequestEntity>((IQueryable<BookingRequestEntity>)_db.BookingRequests),
			(Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity x) => x.BookingID == bookingId),
			ct);
		if (row == null)
		{
			throw new InvalidOperationException("Booking not found.");
		}
		if (access.VenueIds.Count == 0 || !access.VenueIds.Contains(row.VenueID))
		{
			throw new UnauthorizedAccessException("This booking is outside your assigned venues.");
		}
	}

	private static string CustomerBookingsDigitsOnly(string? s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		char[] buf = s.Where(char.IsDigit).ToArray();
		return new string(buf);
	}

	/// <summary>Same labels as <c>dbo.fn_GetPublicBookingStatus</c>.</summary>
	private static string MapToPublicBookingStatus(string? internalStatus)
	{
		string s = (internalStatus ?? "").Trim();
		return s switch
		{
			"Pending" => "Pending",
			"ForwardedToL2" => "Pending",
			"Forwarded" => "Pending",
			"Accepted" => "Approved",
			"PaymentPending" => "Approved",
			"Confirmed" => "Approved",
			"Approved" => "Approved",
			"Rejected" => "Rejected",
			"Cancelled" => "Cancelled",
			_ => "Pending"
		};
	}

	private static bool IsForwardedToL2Status(string? s)
	{
		string x = (s ?? "").Trim();
		return string.Equals(x, "ForwardedToL2", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(x, "Forwarded", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Maps hall <c>VenueCode</c> to <c>NVARCHAR(10)</c> for <c>fn_GenerateBookingRegNo</c>.</summary>
	private static string NormalizeVenueCodeForFn(string? venueCode, int venueId)
	{
		string s = (venueCode ?? "").Trim();
		if (s.Length == 0)
		{
			s = "V" + venueId.ToString(CultureInfo.InvariantCulture);
		}
		if (s.Length > 10)
		{
			s = s.Substring(0, 10);
		}
		return s;
	}

	public async Task SetVenueActiveAsync(OfficePortalAccessVm access, int venueId, bool isActive, CancellationToken ct = default(CancellationToken))
	{
		if (!access.IsSuperAdmin)
		{
			throw new UnauthorizedAccessException("Only super admin can change venue active state.");
		}
		VenueMasterEntity? h = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(
			(IQueryable<VenueMasterEntity>)_db.VenueMasters,
			(Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity x) => x.VenueID == venueId),
			ct);
		if (h == null)
		{
			throw new InvalidOperationException("Venue not found.");
		}
		h.IsActive = isActive;
		await ((DbContext)_db).SaveChangesAsync(ct);
	}

	public async Task CancelBookingBySuperAdminAsync(OfficePortalAccessVm access, int bookingId, string? remarks, CancellationToken ct = default(CancellationToken))
	{
		if (!access.IsSuperAdmin)
		{
			throw new UnauthorizedAccessException("Only super admin can cancel bookings.");
		}
		BookingRequestEntity? b = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingRequestEntity>(
			(IQueryable<BookingRequestEntity>)_db.BookingRequests,
			(Expression<Func<BookingRequestEntity, bool>>)((BookingRequestEntity x) => x.BookingID == bookingId),
			ct);
		if (b == null)
		{
			throw new InvalidOperationException("Booking not found.");
		}
		string old = b.BookingStatus ?? "";
		b.BookingStatus = "Cancelled";
		b.UpdatedAt = DateTime.UtcNow;
		string r = (remarks ?? "").Trim();
		if (r.Length > 500)
		{
			r = r.Substring(0, 500);
		}
		_db.BookingStatusLogs.Add(new BookingStatusLogEntity
		{
			BookingID = bookingId,
			ChangedByType = "Super Admin",
			ChangedByID = access.OfficeUserID,
			OldStatus = old,
			NewStatus = "Cancelled",
			Remarks = string.IsNullOrEmpty(r) ? "Cancelled by super admin." : r,
			ChangedAt = DateTime.UtcNow,
		});
		List<VenueBlockedDateEntity> blocks = await EntityFrameworkQueryableExtensions.ToListAsync<VenueBlockedDateEntity>(
			Queryable.Where<VenueBlockedDateEntity>(
				(IQueryable<VenueBlockedDateEntity>)_db.VenueBlockedDates,
				(Expression<Func<VenueBlockedDateEntity, bool>>)((VenueBlockedDateEntity x) => x.BookingID == bookingId)),
			ct);
		foreach (VenueBlockedDateEntity x in blocks)
		{
			_db.VenueBlockedDates.Remove(x);
		}
		await ((DbContext)_db).SaveChangesAsync(ct);
	}

	public async Task<CreateBookingResponse> CreateAdminVenueBookingAsync(OfficePortalAccessVm access, AdminVenueBookingCreateVm body, CancellationToken ct = default(CancellationToken))
	{
		if (!access.IsSuperAdmin)
		{
			return new CreateBookingResponse(null, null, "Only super admin can create admin venue bookings.");
		}
		string hall = (body.Hall ?? "").Trim();
		string category = (body.Category ?? "").Trim();
		string purpose = (body.Purpose ?? "").Trim();
		if (string.IsNullOrEmpty(hall) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(purpose))
		{
			return new CreateBookingResponse(null, null, "Hall, category, and purpose are required.");
		}
		VenueMasterEntity? venue = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters),
			(Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity v) => v.IsActive && v.VenueName == hall),
			ct);
		if (venue == null)
		{
			venue = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<VenueMasterEntity>(
				EntityFrameworkQueryableExtensions.AsNoTracking<VenueMasterEntity>((IQueryable<VenueMasterEntity>)_db.VenueMasters),
				(Expression<Func<VenueMasterEntity, bool>>)((VenueMasterEntity v) => v.IsActive && (v.VenueName ?? "").Contains(hall)),
				ct);
		}
		if (venue == null)
		{
			return new CreateBookingResponse(null, null, "Could not match an active hall / institute name.");
		}
		BookingCategoryEntity? cat = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingCategoryEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<BookingCategoryEntity>((IQueryable<BookingCategoryEntity>)_db.BookingCategories),
			(Expression<Func<BookingCategoryEntity, bool>>)((BookingCategoryEntity c) => c.CategoryName == category),
			ct);
		if (cat == null)
		{
			return new CreateBookingResponse(null, null, "Unknown category.");
		}
		BookingPurposeEntity? pur = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<BookingPurposeEntity>(
			EntityFrameworkQueryableExtensions.AsNoTracking<BookingPurposeEntity>((IQueryable<BookingPurposeEntity>)_db.BookingPurposes),
			(Expression<Func<BookingPurposeEntity, bool>>)((BookingPurposeEntity p) => p.PurposeName == purpose),
			ct);
		if (pur == null)
		{
			return new CreateBookingResponse(null, null, "Unknown purpose.");
		}
		RegisterUserResponse reg = await RegisterOrLoginUserAsync(body.FullName ?? "", body.Mobile ?? "", ct);
		CreateBookingRequestVm vm = new CreateBookingRequestVm
		{
			UserID = reg.UserId,
			VenueID = venue.VenueID,
			CategoryID = cat.CategoryID,
			PurposeID = pur.PurposeID,
			FromDate = body.FromDate ?? "",
			ToDate = body.ToDate ?? "",
			IdentityNumber = "",
			DocumentPath = "",
			BankName = "",
			AccountNumber = "",
			IFSCCode = "",
			TotalPayable = null,
		};
		CreateBookingResponse created = await CreatePublicBookingAsync(vm, ct, skipCustomerStatusLog: true);
		if (!string.IsNullOrEmpty(created.ErrorMessage) || created.BookingID == null)
		{
			return created;
		}
		if (!DateOnly.TryParse(body.FromDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
			|| !DateOnly.TryParse(body.ToDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
		{
			return created;
		}
		DateOnly d = fromDate;
		while (d <= toDate)
		{
			DateTime bd = d.ToDateTime(TimeOnly.MinValue);
			bool exists = await EntityFrameworkQueryableExtensions.AnyAsync<VenueBlockedDateEntity>(
				EntityFrameworkQueryableExtensions.AsNoTracking<VenueBlockedDateEntity>((IQueryable<VenueBlockedDateEntity>)_db.VenueBlockedDates),
				(Expression<Func<VenueBlockedDateEntity, bool>>)((VenueBlockedDateEntity x) => x.VenueID == venue.VenueID && x.BlockedDate == bd),
				ct);
			if (!exists)
			{
				_db.VenueBlockedDates.Add(new VenueBlockedDateEntity
				{
					VenueID = venue.VenueID,
					BlockedDate = bd,
					BookingID = created.BookingID,
					Reason = "Super admin venue booking",
				});
			}
			d = d.AddDays(1);
		}
		_db.BookingStatusLogs.Add(new BookingStatusLogEntity
		{
			BookingID = created.BookingID!.Value,
			ChangedByType = "Super Admin",
			ChangedByID = access.OfficeUserID,
			OldStatus = null,
			NewStatus = "Pending",
			Remarks = "Admin booking created via portal.",
			ChangedAt = DateTime.UtcNow,
		});
		await ((DbContext)_db).SaveChangesAsync(ct);
		return created;
	}

	private static SqlTransaction? GetAmbientSqlTransaction(AppDbContext db)
	{
		IDbContextTransaction? t = db.Database.CurrentTransaction;
		if (t == null)
		{
			return null;
		}
		if (t is IInfrastructure<DbTransaction> infra && infra.Instance is SqlTransaction st)
		{
			return st;
		}
		return null;
	}

	/// <summary>Calls <c>dbo.fn_GenerateBookingRegNo</c> on SQL Server, or applies the same formatting for SQLite (see <c>database/fn_GenerateBookingRegNo.sql</c>).</summary>
	private async Task<string> ComputeFnBookingRegNoAsync(string venueCodeNvarchar10, int bookingId, SqlTransaction? ambientTransaction, CancellationToken ct)
	{
		if (_db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
		{
			return FormatBookingRegNoLikeSqlFn(venueCodeNvarchar10, bookingId);
		}

		DbConnection conn = _db.Database.GetDbConnection();
		var wasOpen = conn.State == ConnectionState.Open;
		if (!wasOpen)
		{
			await _db.Database.OpenConnectionAsync(ct);
		}
		try
		{
			if (conn is not SqlConnection sqlConn)
			{
				throw new InvalidOperationException("SQL Server is required for booking registration numbers (or use SQLite).");
			}
			await using var cmd = sqlConn.CreateCommand();
			cmd.Transaction = ambientTransaction;
			cmd.CommandText = "SELECT dbo.fn_GenerateBookingRegNo(@VenueCode, @BookingID)";
			cmd.CommandType = CommandType.Text;
			cmd.Parameters.Add(new SqlParameter("@VenueCode", SqlDbType.NVarChar, 10) { Value = venueCodeNvarchar10 });
			cmd.Parameters.Add(new SqlParameter("@BookingID", SqlDbType.Int) { Value = bookingId });
			object? v = await cmd.ExecuteScalarAsync(ct);
			if (v == null || v == DBNull.Value)
			{
				throw new InvalidOperationException("fn_GenerateBookingRegNo returned no value.");
			}
			string no = Convert.ToString(v, CultureInfo.InvariantCulture)?.Trim() ?? "";
			if (no.Length == 0)
			{
				throw new InvalidOperationException("fn_GenerateBookingRegNo returned an empty value.");
			}
			return no.Length <= 30 ? no : no.Substring(0, 30);
		}
		finally
		{
			if (!wasOpen && ambientTransaction == null)
			{
				await _db.Database.CloseConnectionAsync();
			}
		}
	}

	/// <summary>Matches <c>UPPER(@VenueCode) + '-' + YEAR(GETDATE()) + RIGHT('0000' + CAST(@BookingID AS NVARCHAR(20)), 4)</c>.</summary>
	private static string FormatBookingRegNoLikeSqlFn(string venueCodeNvarchar10, int bookingId)
	{
		string y = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
		string idStr = bookingId.ToString(CultureInfo.InvariantCulture);
		string padded = "0000" + idStr;
		string suffix = padded.Length > 4 ? padded.Substring(padded.Length - 4) : padded.PadLeft(4, '0');
		string code = (venueCodeNvarchar10 ?? "").Trim().ToUpperInvariant();
		if (code.Length > 10)
		{
			code = code.Substring(0, 10);
		}
		string no = code + "-" + y + suffix;
		return no.Length <= 30 ? no : no.Substring(0, 30);
	}

	private static OfficeUserVm MapOfficeUser(OfficeUserEntity a, IReadOnlyList<int> venueIds, string? roleName)
	{
		string claim = OfficeJwtRoleMapper.ToJwtClaim(a.RoleID, roleName ?? "");
		return new OfficeUserVm(
			a.OfficeUserID,
			a.FullName ?? "",
			a.Username ?? "",
			claim,
			a.RoleID,
			venueIds,
			roleName,
			a.MobileNumber,
			a.EmailID,
			a.IsActive,
			a.CreatedAt);
	}
}
