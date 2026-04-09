import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, forkJoin, Observable, of, throwError } from 'rxjs';
import { catchError, filter, map, switchMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { formatHttpErrorMessage } from './http-error-message';
import { ToastService } from './toast.service';

export type BookingStatus =
  | 'pending'
  | 'provisionallyApproved'
  | 'forward'
  | 'approved'
  | 'rejected'
  | 'cancelled';

export interface OfficeUserRoleRecord {
  roleID: number;
  roleName: string;
}

export interface AdminRoleRecord {
  id: string;
  fullName: string;
  email: string;
  mobile: string;
  /** Display name from OfficeUserRole when available. */
  roleName: string;
  roleId: number | null;
  venueIds: number[];
  password: string;
}

export interface RateChartRecord {
  id: string;
  hallName: string;
  bookingCategory: string;
  capacityFrom: string;
  capacityTo: string;
  rate: string;
  effectiveFrom: string;
  effectiveTo: string;
}

export interface CategoryRecord {
  id: string;
  categoryType: string;
  identityLabel: string;
  identityFormat: string;
  documentLabel: string;
  isActive: boolean;
}

export interface PurposeRecord {
  id: string;
  purposeName: string;
  maxDays: number;
  isActive: boolean;
}

export interface AccountDetailsRecord {
  id: string;
  hallName: string;
  bankName: string;
  accountNo: string;
  bankAddress: string;
  ifsc: string;
  contactName: string;
  mobile: string;
  chequeInFavour: string;
}

export interface VenueTypeRecord {
  id: string;
  typeName: string;
  isActive: boolean;
}

export interface HallDescriptionRecord {
  id: string;
  /** VenueType row id for API upsert */
  venueTypeId: number;
  shortCode: string;
  name: string;
  /** From VenueMaster; used for office employee venue labels. */
  city?: string;
  gpsLocation: string;
  capacity: string;
  address: string;
  areaSqmt: string;
  rooms: string;
  kitchen: string;
  toilet: string;
  bathroom: string;
  facilities: string;
  photoDataUrl: string;
  status: string;
}

export interface TextAdvertiseRecord {
  id: string;
  startDate: string;
  endDate: string;
  advertise: string;
}

export interface ImageAdvertiseRecord {
  id: string;
  startDate: string;
  endDate: string;
  imageDataUrl: string;
}

/** Home hero slider — **ImageBanner** table. */
export interface ImageBannerRecord {
  id: string;
  startDate: string;
  endDate: string;
  imageDataUrl: string;
  imgURL: string;
}

export interface DashboardActivityItemDto {
  line: string;
  sub: string;
  timeLabel: string;
  avatarTone: string;
}

export interface DashboardActivityBundleDto {
  admin: DashboardActivityItemDto[];
  customer: DashboardActivityItemDto[];
}

export interface BookingRecord {
  id: string;
  bookingNo: string;
  fullName: string;
  mobile: string;
  email: string;
  hall: string;
  address: string;
  totalAmount: string;
  category: string;
  purpose: string;
  fromDate: string;
  toDate: string;
  status: BookingStatus;
}

function uid(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

@Injectable({
  providedIn: 'root',
})
export class AdminDataService {
  private readonly base = environment.apiBaseUrl.replace(/\/$/, '');
  private readonly admins$ = new BehaviorSubject<AdminRoleRecord[]>([]);
  private readonly officeUserRoles$ = new BehaviorSubject<OfficeUserRoleRecord[]>([]);
  private readonly rateCharts$ = new BehaviorSubject<RateChartRecord[]>([]);
  private readonly categories$ = new BehaviorSubject<CategoryRecord[]>([]);
  private readonly purposesRows$ = new BehaviorSubject<PurposeRecord[]>([]);
  private readonly accounts$ = new BehaviorSubject<AccountDetailsRecord[]>([]);
  private readonly halls$ = new BehaviorSubject<HallDescriptionRecord[]>([]);
  private readonly venueTypes$ = new BehaviorSubject<VenueTypeRecord[]>([]);
  private readonly textAds$ = new BehaviorSubject<TextAdvertiseRecord[]>([]);
  private readonly imageAds$ = new BehaviorSubject<ImageAdvertiseRecord[]>([]);
  private readonly imageBanners$ = new BehaviorSubject<ImageBannerRecord[]>([]);
  private readonly bookings$ = new BehaviorSubject<BookingRecord[]>([]);

  /** purposeId -> name for rate rule fallback */
  private purposesCache: { purposeID: number; purposeName: string }[] = [];

  /** First full `loadAll()` after login; drives section skeletons (not re-shown on refresh). */
  private readonly hydrated$ = new BehaviorSubject(false);
  readonly dataHydrated = this.hydrated$.asObservable();
  private firstLoadComplete = false;

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private toast: ToastService,
  ) {
    this.auth.loggedIn.pipe(filter((v) => !v)).subscribe(() => {
      this.firstLoadComplete = false;
      this.hydrated$.next(false);
    });
  }

  readonly admins = this.admins$.asObservable();
  /** OfficeUserRole rows for admin forms (e.g. Add Employee). */
  readonly officeUserRoles = this.officeUserRoles$.asObservable();
  readonly rateCharts = this.rateCharts$.asObservable();
  readonly categories = this.categories$.asObservable();
  /** All booking purposes (admin grid); distinct from internal `purposesCache` used for rate rules. */
  readonly purposesRows = this.purposesRows$.asObservable();
  readonly accounts = this.accounts$.asObservable();
  readonly halls = this.halls$.asObservable();
  /** All venue types (Super Admin API); empty for other roles if 403. */
  readonly venueTypes = this.venueTypes$.asObservable();
  readonly textAds = this.textAds$.asObservable();
  readonly imageAds = this.imageAds$.asObservable();
  readonly imageBanners = this.imageBanners$.asObservable();
  readonly bookings = this.bookings$.asObservable();

  private hdr(): HttpHeaders {
    const t = this.auth.getToken();
    return new HttpHeaders(t ? { Authorization: `Bearer ${t}` } : {});
  }

  private reloadOnSuccess(
    obs: Observable<unknown>,
    errorFallback: string,
    onSuccess?: () => void,
    successMessage = 'Saved successfully.',
  ): void {
    obs.subscribe({
      next: () => {
        onSuccess?.();
        this.toast.success(successMessage);
        this.loadAll();
      },
      error: (err: unknown) => this.toast.error(formatHttpErrorMessage(err, errorFallback)),
    });
  }

  /** Load all master data from API (call after login). */
  loadAll(): void {
    if (!this.auth.isLoggedIn()) {
      return;
    }
    if (!this.firstLoadComplete) {
      this.hydrated$.next(false);
    }
    const h = { headers: this.hdr() };
    const failed: string[] = [];
    const swallow = <T>(label: string, source: Observable<T>) =>
      source.pipe(
        catchError((err: unknown) => {
          console.error(`[AdminDataService] ${label}`, err);
          failed.push(label);
          return of([] as unknown as T);
        }),
      );
    forkJoin({
      bookings: swallow(
        'bookings',
        this.http.get<BookingRecordDto[]>(`${this.base}/api/admin/bookings/grid`, h),
      ),
      venues: swallow('venues', this.http.get<VenueAdminDto[]>(`${this.base}/api/venues/admin/all`, h)),
      venueTypesApi: swallow(
        'venue types',
        this.http.get<VenueTypeAdminDto[]>(`${this.base}/api/venuetypes/admin/all`, h),
      ),
      categories: swallow(
        'categories',
        this.http.get<CategoryDto[]>(`${this.base}/api/bookingcategories/all`, h),
      ),
      purposes: swallow(
        'purposes',
        this.http.get<PurposeDto[]>(`${this.base}/api/bookingpurposes/all`, h),
      ),
      rates: swallow(
        'rate charts',
        this.http.get<RateChartDto[]>(`${this.base}/api/venues/rate-charts`, h),
      ),
      ads: swallow('advertisements', this.http.get<AdDto[]>(`${this.base}/api/advertisements/all`, h)),
      textAdsApi: swallow(
        'text advertisements',
        this.http.get<TextAdDto[]>(`${this.base}/api/TextAdvertisements/all`, h),
      ),
      imageBannersApi: swallow(
        'image banners',
        this.http.get<ImageBannerDto[]>(`${this.base}/api/ImageBanners/all`, h),
      ),
      users: swallow('office users', this.http.get<OfficeUserDto[]>(`${this.base}/api/officeusers`, h)),
      officeUserRolesApi: swallow(
        'office user roles',
        this.http.get<OfficeUserRoleDto[]>(`${this.base}/api/officeusers/roles`, h),
      ),
      accounts: swallow(
        'bank accounts',
        this.http.get<AccountDetailsRecord[]>(`${this.base}/api/admin/metadata/bank-accounts`, h),
      ),
    }).subscribe((r) => {
      if (failed.length) {
        const hint =
          failed.length >= 5
            ? 'Office APIs returned errors (often 401 or API not running). Check sign-in and that OnlineBookingSystem.Api is up with /api proxied.'
            : `Could not load: ${failed.join(', ')}.`;
        console.warn('[AdminDataService]', hint);
      }
      this.purposesCache = r.purposes.map((p) => ({
        purposeID: p.purposeID,
        purposeName: p.purposeName ?? '',
      }));
      this.bookings$.next(r.bookings.map(mapBooking));
      this.halls$.next(r.venues.map((v) => mapVenue(v)));
      this.venueTypes$.next(
        (r.venueTypesApi as VenueTypeAdminDto[]).map((t) => ({
          id: String(t.venueTypeID),
          typeName: t.typeName ?? '',
          isActive: !!t.isActive,
        })),
      );
      this.categories$.next(
        r.categories.map((c) => ({
          id: String(c.categoryID),
          categoryType: c.categoryName ?? '',
          identityLabel: c.identityLabel ?? '',
          identityFormat: c.identityFormat ?? '',
          documentLabel: c.documentLabel ?? '',
          isActive: c.isActive !== false,
        })),
      );
      this.purposesRows$.next(
        r.purposes.map((p) => ({
          id: String(p.purposeID),
          purposeName: p.purposeName ?? '',
          maxDays: typeof p.maxDays === 'number' && p.maxDays > 0 ? p.maxDays : 1,
          isActive: p.isActive !== false,
        })),
      );
      this.rateCharts$.next(r.rates.map(mapRate));
      const imgRows: ImageAdvertiseRecord[] = [];
      for (const a of r.ads) {
        const path = (a.adImagePath ?? '').trim();
        if (!path) {
          continue;
        }
        const id = String(a.adID);
        const sd = isoDate(a.startDate);
        const ed = isoDate(a.endDate);
        const url = path.startsWith('http') ? path : `${this.base}${path.startsWith('/') ? '' : '/'}${path}`;
        imgRows.push({ id, startDate: sd, endDate: ed, imageDataUrl: url });
      }
      this.textAds$.next(
        r.textAdsApi.map((t) => ({
          id: String(t.textAdID),
          startDate: isoDate(t.startDate),
          endDate: isoDate(t.endDate),
          advertise: t.advertise ?? '',
        })),
      );
      this.imageAds$.next(imgRows);
      this.imageBanners$.next(
        r.imageBannersApi.map((b) => {
          const bid = b.imgId ?? b.ImgId ?? 0;
          const path = (b.imgPath ?? b.ImgPath ?? '').trim();
          const ext = (b.imgURL ?? b.ImgURL ?? '').trim();
          let preview = '';
          if (path) {
            preview = path.startsWith('http') ? path : `${this.base}${path.startsWith('/') ? '' : '/'}${path}`;
          } else if (ext) {
            preview = ext;
          }
          return {
            id: String(bid),
            startDate: isoDate(b.startDate ?? b.StartDate ?? ''),
            endDate: isoDate(b.endDate ?? b.EndDate ?? ''),
            imageDataUrl: preview,
            imgURL: ext,
          };
        }),
      );
      this.admins$.next(r.users.map(mapOfficeUser));
      this.officeUserRoles$.next(r.officeUserRolesApi.map(mapOfficeUserRole));
      this.accounts$.next(r.accounts);
      this.firstLoadComplete = true;
      this.hydrated$.next(true);
    });
  }

  getAdmins(): AdminRoleRecord[] {
    return this.admins$.value;
  }

  getOfficeUserRoles(): OfficeUserRoleRecord[] {
    return this.officeUserRoles$.value;
  }

  /**
   * Fills the role dropdown reliably (e.g. after login or if bulk load swallowed a 401).
   * Office users with Level1/Level2 can call GET …/roles; only Super Admin can mutate users.
   */
  refreshOfficeUserRoles(): void {
    if (!this.auth.isLoggedIn()) {
      return;
    }
    this.http
      .get<OfficeUserRoleDto[]>(`${this.base}/api/officeusers/roles`, { headers: this.hdr() })
      .subscribe({
        next: (rows) => {
          const mapped = (Array.isArray(rows) ? rows : []).map(mapOfficeUserRole).filter((r) => r.roleID > 0);
          this.officeUserRoles$.next(mapped);
        },
        error: (err: unknown) => {
          console.error('[AdminDataService] office user roles', err);
          this.toast.error(formatHttpErrorMessage(err, 'Could not load roles. Check sign-in and API.'));
        },
      });
  }

  getRateCharts(): RateChartRecord[] {
    return this.rateCharts$.value;
  }

  getCategories(): CategoryRecord[] {
    return this.categories$.value;
  }

  getPurposes(): PurposeRecord[] {
    return this.purposesRows$.value;
  }

  getAccounts(): AccountDetailsRecord[] {
    return this.accounts$.value;
  }

  getHalls(): HallDescriptionRecord[] {
    return this.halls$.value;
  }

  getVenueTypes(): VenueTypeRecord[] {
    return this.venueTypes$.value;
  }

  getTextAds(): TextAdvertiseRecord[] {
    return this.textAds$.value;
  }

  getImageAds(): ImageAdvertiseRecord[] {
    return this.imageAds$.value;
  }

  getImageBanners(): ImageBannerRecord[] {
    return this.imageBanners$.value;
  }

  getBookings(): BookingRecord[] {
    return this.bookings$.value;
  }

  getBookingsByStatus(status: BookingStatus): BookingRecord[] {
    return this.bookings$.value.filter((b) => b.status === status);
  }

  getBookingCounts(): {
    total: number;
    pending: number;
    provisional: number;
    forward: number;
    approved: number;
    rejected: number;
    cancelled: number;
  } {
    const all = this.bookings$.value;
    return {
      total: all.length,
      pending: all.filter((b) => b.status === 'pending').length,
      provisional: all.filter((b) => b.status === 'provisionallyApproved').length,
      forward: all.filter((b) => b.status === 'forward').length,
      approved: all.filter((b) => b.status === 'approved').length,
      rejected: all.filter((b) => b.status === 'rejected').length,
      cancelled: all.filter((b) => b.status === 'cancelled').length,
    };
  }

  /** Full booking row for admin review (matches API `AdminBookingDetailVm`). */
  fetchBookingDetail(bookingId: number): Observable<AdminBookingDetailDto> {
    const url = `${this.base}/api/admin/bookings/${bookingId}`;
    return this.http.get<AdminBookingDetailDto>(url, { headers: this.hdr() });
  }

  /** Calls `sp_L1ForwardOrReject` via API (action: Forward | Reject). */
  postL1BookingAction(bookingId: number, action: 'Forward' | 'Reject', remarks: string | null): Observable<void> {
    const url = `${this.base}/api/admin/bookings/l1-action`;
    return this.http.post<void>(url, { bookingID: bookingId, action, remarks }, { headers: this.hdr() });
  }

  /** Calls `sp_L2AcceptRejectReturn` via API (Return = provisionally approve back to L1; Reject). */
  postL2BookingAction(bookingId: number, action: 'Return' | 'Reject', remarks: string | null): Observable<void> {
    const url = `${this.base}/api/admin/bookings/l2-action`;
    return this.http.post<void>(url, { bookingID: bookingId, action, remarks }, { headers: this.hdr() });
  }

  /** Final L1 approve after L2 returned booking (payment row + Accepted, SMS, `sp_LogSMS`). */
  postL1FinalApprove(body: L1FinalApprovePayload): Observable<void> {
    const url = `${this.base}/api/admin/bookings/l1-final-approve`;
    return this.http.post<void>(url, body, { headers: this.hdr() });
  }

  getPurposeLabelsSorted(): string[] {
    return [...this.purposesCache].map((p) => p.purposeName).filter((n) => !!n?.trim()).sort((a, b) => a.localeCompare(b));
  }

  getHallOptions(): string[] {
    return this.halls$.value.map((h) => h.name);
  }

  upsertAdmin(record: Omit<AdminRoleRecord, 'id'> & { id?: string }): void {
    const venueIDs = (record.venueIds ?? [])
      .map((x) => Number(x))
      .filter((n) => Number.isFinite(n) && n > 0);
    const bodyCreate = {
      fullName: record.fullName,
      username: record.email.trim(),
      password: record.password || 'ChangeMe!123',
      roleID: record.roleId ?? undefined,
      role: record.roleName?.trim() || undefined,
      mobileNumber: record.mobile || null,
      emailID: record.email.trim(),
      venueIDs,
    };
    const url = `${this.base}/api/officeusers`;
    const req = record.id
      ? this.http.put(
          `${url}/${record.id}`,
          {
            fullName: bodyCreate.fullName,
            password: record.password || undefined,
            roleID: bodyCreate.roleID,
            role: bodyCreate.role,
            mobileNumber: bodyCreate.mobileNumber,
            emailID: bodyCreate.emailID,
            isActive: true,
            venueIDs,
          },
          { headers: this.hdr() },
        )
      : this.http.post(url, bodyCreate, { headers: this.hdr() });
    this.reloadOnSuccess(req, 'Could not save office user.');
  }

  deleteAdmin(id: string): void {
    this.reloadOnSuccess(
      this.http.delete(`${this.base}/api/officeusers/${id}`, { headers: this.hdr() }),
      'Could not remove office user.',
      undefined,
      'Deleted successfully.',
    );
  }

  upsertRateChart(record: Omit<RateChartRecord, 'id'> & { id?: string }): void {
    const venueId = this.halls$.value.find((h) => h.name === record.hallName)?.id;
    const cat = this.categories$.value.find((c) => c.categoryType === record.bookingCategory);
    const purposeId = this.purposesCache[0]?.purposeID ?? 1;
    if (!venueId || !cat) {
      const list = [...this.rateCharts$.value];
      if (record.id) {
        const i = list.findIndex((x) => x.id === record.id);
        if (i >= 0) {
          list[i] = { ...list[i], ...record, id: record.id };
        }
      } else {
        list.push({ ...record, id: uid() });
      }
      this.rateCharts$.next(list);
      return;
    }
    const rent = parseFloat(record.rate) || 0;
    const body = {
      ruleID: record.id && /^\d+$/.test(record.id) ? Number(record.id) : 0,
      venueID: Number(venueId),
      categoryID: Number(cat.id),
      purposeID: purposeId,
      rentPerDay: rent,
      securityDeposit: 0,
      maxDays: 3,
      isAllottable: true,
      notAllottableReason: null as string | null,
      isActive: true,
    };
    this.reloadOnSuccess(
      this.http.post(`${this.base}/api/venues/${venueId}/venuerentrules`, body, { headers: this.hdr() }),
      'Could not save rate rule.',
    );
  }

  deleteRateChart(id: string): void {
    if (/^\d+$/.test(id)) {
      const row = this.rateCharts$.value.find((x) => x.id === id);
      const venueId = row ? this.halls$.value.find((h) => h.name === row.hallName)?.id : undefined;
      if (venueId) {
        this.reloadOnSuccess(
          this.http.delete(`${this.base}/api/venues/${venueId}/venuerentrules/${id}`, { headers: this.hdr() }),
          'Could not delete rate rule.',
          undefined,
          'Deleted successfully.',
        );
        return;
      }
    }
    this.rateCharts$.next(this.rateCharts$.value.filter((x) => x.id !== id));
  }

  upsertCategory(record: Omit<CategoryRecord, 'id'> & { id?: string }): void {
    const body = {
      categoryID: record.id && /^\d+$/.test(record.id) ? Number(record.id) : undefined,
      categoryName: record.categoryType.trim(),
      identityLabel: (record.identityLabel ?? '').trim() || 'ID',
      identityFormat: (record.identityFormat ?? '').trim() || 'As per policy',
      documentLabel: (record.documentLabel ?? '').trim() || 'Supporting document',
      isActive: record.isActive,
    };
    this.reloadOnSuccess(
      this.http.post(`${this.base}/api/bookingcategories`, body, { headers: this.hdr() }),
      'Could not save category.',
    );
  }

  upsertPurpose(record: Omit<PurposeRecord, 'id'> & { id?: string }): void {
    const body = {
      purposeID: record.id && /^\d+$/.test(record.id) ? Number(record.id) : undefined,
      purposeName: record.purposeName.trim(),
      maxDays: record.maxDays,
      isActive: record.isActive,
    };
    this.reloadOnSuccess(
      this.http.post(`${this.base}/api/bookingpurposes`, body, { headers: this.hdr() }),
      'Could not save booking purpose.',
    );
  }

  deleteCategory(id: string): void {
    this.reloadOnSuccess(
      this.http.delete(`${this.base}/api/bookingcategories/${id}`, { headers: this.hdr() }),
      'Could not delete category.',
      undefined,
      'Deleted successfully.',
    );
  }

  deletePurpose(id: string): void {
    this.reloadOnSuccess(
      this.http.delete(`${this.base}/api/bookingpurposes/${id}`, { headers: this.hdr() }),
      'Could not delete booking purpose.',
      undefined,
      'Deleted successfully.',
    );
  }

  upsertAccount(record: Omit<AccountDetailsRecord, 'id'> & { id?: string }): void {
    const list = [...this.accounts$.value];
    if (record.id) {
      const i = list.findIndex((x) => x.id === record.id);
      if (i >= 0) {
        list[i] = { ...list[i], ...record, id: record.id };
      }
    } else {
      list.push({ ...record, id: uid() });
    }
    this.accounts$.next(list);
  }

  deleteAccount(id: string): void {
    this.accounts$.next(this.accounts$.value.filter((x) => x.id !== id));
  }

  upsertHall(record: Omit<HallDescriptionRecord, 'id'> & { id?: string }): void {
    const fac = buildFacilitiesJson(record);
    const codeRaw = record.shortCode || record.name.slice(0, 6).toUpperCase().replace(/\s/g, '');
    const body = {
      venueID: record.id && /^\d+$/.test(record.id) ? Number(record.id) : undefined,
      venueTypeID: record.venueTypeId > 0 ? record.venueTypeId : 1,
      venueName: record.name,
      /** DB column VenueCode is nvarchar(10) */
      venueCode: codeRaw.slice(0, 10),
      address: record.address || 'Nagpur',
      city: (record.city ?? '').trim() || 'Nagpur',
      division: 'Nagpur',
      googleMapLink: record.gpsLocation || null,
      facilities: fac,
      isActive: record.status !== 'Inactive',
      capacity: parseOptionalInt(record.capacity),
      areaSqMt: parseOptionalDecimal(record.areaSqmt),
      noOfRooms: parseOptionalInt(record.rooms),
      noOfKitchen: parseOptionalInt(record.kitchen),
      noOfToilet: parseOptionalInt(record.toilet),
      noOfBathroom: parseOptionalInt(record.bathroom),
    };
    this.reloadOnSuccess(
      this.http.post(`${this.base}/api/venues`, body, { headers: this.hdr() }),
      'Could not save venue.',
    );
  }

  upsertVenueType(record: Omit<VenueTypeRecord, 'id'> & { id?: string }): void {
    const body = {
      venueTypeID: record.id && /^\d+$/.test(record.id) ? Number(record.id) : undefined,
      typeName: record.typeName.trim(),
      isActive: record.isActive,
    };
    this.reloadOnSuccess(
      this.http.post(`${this.base}/api/venuetypes/admin`, body, { headers: this.hdr() }),
      'Could not save venue type.',
    );
  }

  deleteHall(id: string): void {
    this.reloadOnSuccess(
      this.http.delete(`${this.base}/api/venues/${id}`, { headers: this.hdr() }),
      'Could not delete venue.',
      undefined,
      'Deleted successfully.',
    );
  }

  /** Persists to **TextAdvertisement** (not Advertisement). */
  upsertTextAd(
    record: Omit<TextAdvertiseRecord, 'id'> & { id?: string },
    onSuccess?: () => void,
  ): void {
    const body = {
      textAdID: record.id && /^\d+$/.test(record.id) ? Number(record.id) : undefined,
      advertise: record.advertise,
      startDate: record.startDate,
      endDate: record.endDate,
      isActive: true,
    };
    this.reloadOnSuccess(
      this.http.post(`${this.base}/api/TextAdvertisements`, body, { headers: this.hdr() }),
      'Could not save text advertisement.',
      onSuccess,
    );
  }

  deleteTextAd(id: string): void {
    this.reloadOnSuccess(
      this.http.delete(`${this.base}/api/TextAdvertisements/${id}`, { headers: this.hdr() }),
      'Could not delete text advertisement.',
      undefined,
      'Deleted successfully.',
    );
  }

  /**
   * Saves to the Advertisement table. New images must be uploaded first — DB AdImagePath is max 500 chars (not a data URL).
   */
  upsertImageAd(
    record: Omit<ImageAdvertiseRecord, 'id'> & { id?: string },
    onSuccess?: () => void,
  ): void {
    const adID = record.id && /^\d+$/.test(record.id) ? Number(record.id) : undefined;
    const startDate = record.startDate;
    const endDate = record.endDate;
    const src = record.imageDataUrl.trim();
    if (!src) {
      return;
    }

    const postAd = (adImagePath: string) =>
      this.http.post(`${this.base}/api/advertisements`, {
        adID,
        adTitle: 'Image advertisement',
        adImagePath,
        adURL: null as string | null,
        startDate,
        endDate,
        isActive: true,
      }, { headers: this.hdr() });

    if (src.startsWith('data:')) {
      let blob: Blob;
      try {
        blob = dataUrlToBlob(src);
      } catch {
        this.toast.error('Could not read the image file. Try another PNG or JPG.');
        return;
      }
      const ext = blob.type.includes('png') ? 'png' : 'jpg';
      const fd = new FormData();
      fd.append('file', new File([blob], `ad-banner.${ext}`, { type: blob.type || 'image/jpeg' }));
      this.reloadOnSuccess(
        this.http
          .post<unknown>(`${this.base}/api/Documents/upload`, fd, { headers: this.hdr() })
          .pipe(
            switchMap((up) => {
              const path = uploadDocumentPathFromResponse(up);
              return path ? postAd(path) : throwError(() => new Error('Upload returned no file path.'));
            }),
          ),
        'Could not save image advertisement.',
        onSuccess,
      );
      return;
    }

    const path = adImageStoragePathFromUrl(src, this.base);
    this.reloadOnSuccess(postAd(path), 'Could not save image advertisement.', onSuccess);
  }

  deleteImageAd(id: string): void {
    this.reloadOnSuccess(
      this.http.delete(`${this.base}/api/advertisements/${id}`, { headers: this.hdr() }),
      'Could not delete advertisement.',
      undefined,
      'Deleted successfully.',
    );
  }

  /**
   * Home hero slider — **ImageBanner** table. Upload a file and/or set an external **ImgURL** (see API validation).
   */
  upsertImageBanner(
    record: Omit<ImageBannerRecord, 'id'> & { id?: string },
    onSuccess?: () => void,
  ): void {
    const imgId = record.id && /^\d+$/.test(record.id) ? Number(record.id) : undefined;
    const startDate = record.startDate;
    const endDate = record.endDate;
    const link = (record.imgURL ?? '').trim() || null;
    const src = (record.imageDataUrl ?? '').trim();

    const postBanner = (imgPath: string | null, imgURL: string | null) =>
      this.http.post(`${this.base}/api/ImageBanners`, {
        imgId,
        imgPath,
        imgURL,
        startDate,
        endDate,
        isActive: true,
      }, { headers: this.hdr() });

    if (src.startsWith('data:')) {
      let blob: Blob;
      try {
        blob = dataUrlToBlob(src);
      } catch {
        this.toast.error('Could not read the image file. Try another PNG or JPG.');
        return;
      }
      const ext = blob.type.includes('png') ? 'png' : 'jpg';
      const fd = new FormData();
      fd.append('file', new File([blob], `hero-banner.${ext}`, { type: blob.type || 'image/jpeg' }));
      this.reloadOnSuccess(
        this.http
          .post<unknown>(`${this.base}/api/Documents/upload`, fd, { headers: this.hdr() })
          .pipe(
            switchMap((up) => {
              const path = uploadDocumentPathFromResponse(up);
              return path
                ? postBanner(path, link)
                : throwError(() => new Error('Upload returned no file path.'));
            }),
          ),
        'Could not save image banner.',
        onSuccess,
      );
      return;
    }

    let imgPath: string | null = null;
    let imgURL = link;

    if (src) {
      const isHttp = src.startsWith('http://') || src.startsWith('https://');
      if (isHttp) {
        if (src.includes('/uploads/')) {
          imgPath = adImageStoragePathFromUrl(src, this.base);
        } else if (!imgURL) {
          imgURL = src;
        }
      } else {
        imgPath = adImageStoragePathFromUrl(src, this.base);
      }
    }

    if (!imgPath && !imgURL) {
      this.toast.error('Upload an image or enter an image URL.');
      return;
    }

    this.reloadOnSuccess(postBanner(imgPath, imgURL), 'Could not save image banner.', onSuccess);
  }

  deleteImageBanner(id: string): void {
    this.reloadOnSuccess(
      this.http.delete(`${this.base}/api/ImageBanners/${id}`, { headers: this.hdr() }),
      'Could not delete image banner.',
      undefined,
      'Deleted successfully.',
    );
  }

  fetchRecentDashboardActivity(): Observable<DashboardActivityBundleDto> {
    return this.http.get<DashboardActivityBundleDto>(`${this.base}/api/admin/bookings/recent-activity`, {
      headers: this.hdr(),
    });
  }

  createAdminVenueBooking(
    body: {
      fullName: string;
      mobile: string;
      email: string;
      address: string;
      hall: string;
      category: string;
      purpose: string;
      fromDate: string;
      toDate: string;
    },
    onSuccess?: () => void,
  ): void {
    this.reloadOnSuccess(
      this.http.post<{ bookingRegNo?: string; bookingID?: number }>(
        `${this.base}/api/admin/bookings/admin-venue-booking`,
        {
          fullName: body.fullName,
          mobile: body.mobile,
          email: body.email || null,
          address: body.address || null,
          hall: body.hall,
          category: body.category,
          purpose: body.purpose,
          fromDate: body.fromDate,
          toDate: body.toDate,
        },
        { headers: this.hdr() },
      ),
      'Could not create admin booking.',
      onSuccess,
      'Booking created.',
    );
  }

  setVenueActive(venueId: number, isActive: boolean): void {
    this.reloadOnSuccess(
      this.http.patch(`${this.base}/api/venues/admin/${venueId}/active`, { isActive }, { headers: this.hdr() }),
      'Could not update venue status.',
    );
  }

  cancelBookingBySuperAdmin(bookingId: number, remarks?: string | null): void {
    this.reloadOnSuccess(
      this.http.post(`${this.base}/api/admin/bookings/cancel-super-admin`, { bookingID: bookingId, remarks: remarks ?? null }, { headers: this.hdr() }),
      'Could not cancel booking.',
      undefined,
      'Booking cancelled.',
    );
  }

  addBooking(record: Omit<BookingRecord, 'id' | 'bookingNo' | 'status'>): void {
    const list = [
      ...this.bookings$.value,
      {
        id: uid(),
        bookingNo: `TMP${Date.now()}`,
        status: 'pending' as const,
        ...record,
      },
    ];
    this.bookings$.next(list);
  }

  updateBooking(id: string, patch: Partial<BookingRecord>): void {
    this.bookings$.next(this.bookings$.value.map((b) => (b.id === id ? { ...b, ...patch } : b)));
  }

  deleteBooking(id: string): void {
    this.bookings$.next(this.bookings$.value.filter((b) => b.id !== id));
  }
}

interface BookingRecordDto {
  id: string;
  bookingNo: string;
  fullName: string;
  mobile: string;
  email: string;
  hall: string;
  address: string;
  totalAmount: string;
  category: string;
  purpose: string;
  fromDate: string;
  toDate: string;
  bookingStatusRaw: string;
  level2UserId: number | null;
}

/** API model camelCase — `AdminBookingDetailVm`. */
export interface L1FinalApprovePayload {
  bookingID: number;
  paymentMode: string;
  paymentStatus: string;
  transactionRefNo: string;
  amountPaid: number;
}

export interface AdminBookingDetailDto {
  bookingID: number;
  bookingRegNo: string;
  venueID: number;
  venueName: string;
  categoryID: number;
  categoryName: string;
  identityLabel: string;
  documentLabel: string;
  purposeID: number;
  purposeName: string;
  purposeMaxDays: number;
  identityNumber: string;
  documentPath: string;
  bankName: string;
  accountNumber: string;
  ifscCode: string;
  totalAmount: number;
  bookingStatusRaw: string;
  level1UserID: number | null;
  level2UserID: number | null;
  userFullName: string;
  userMobile: string;
  userEmail: string | null;
  userAddress: string | null;
  bookingFromDate: string;
  bookingToDate: string;
  statusHistory?: BookingStatusLogEntryDto[];
}

export interface BookingStatusLogEntryDto {
  logID: number;
  changedByType: string;
  changedByID: number | null;
  oldStatus: string | null;
  newStatus: string;
  remarks: string | null;
  changedAtIso: string;
}

interface VenueTypeAdminDto {
  venueTypeID: number;
  typeName: string;
  isActive: boolean;
}

interface VenueAdminDto {
  venueID: number;
  venueTypeID: number;
  venueName: string;
  venueCode: string;
  address: string;
  city: string;
  division: string;
  googleMapLink: string | null;
  facilities: string | null;
  isActive: boolean;
  createdAt: string;
  capacity?: number | null;
  areaSqMt?: number | null;
  noOfRooms?: number | null;
  noOfKitchen?: number | null;
  noOfToilet?: number | null;
  noOfBathroom?: number | null;
}

interface CategoryDto {
  categoryID: number;
  categoryName: string;
  identityLabel?: string;
  identityFormat?: string;
  documentLabel?: string;
  isActive?: boolean;
}

interface PurposeDto {
  purposeID: number;
  purposeName: string;
  maxDays?: number;
  isActive?: boolean;
}

interface RateChartDto {
  id: string;
  hallName: string;
  bookingCategory: string;
  capacityFrom: string;
  capacityTo: string;
  rate: string;
  effectiveFrom: string;
  effectiveTo: string;
}

interface AdDto {
  adID: number;
  adTitle: string;
  adImagePath: string | null;
  adURL: string | null;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

interface TextAdDto {
  textAdID: number;
  advertise: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

interface ImageBannerDto {
  imgId?: number;
  ImgId?: number;
  imgPath?: string | null;
  ImgPath?: string | null;
  imgURL?: string | null;
  ImgURL?: string | null;
  startDate?: string;
  StartDate?: string;
  endDate?: string;
  EndDate?: string;
  isActive?: boolean;
  IsActive?: boolean;
}

interface OfficeUserDto {
  officeUserID: number;
  fullName: string;
  username: string;
  role: string;
  roleID?: number | null;
  RoleID?: number | null;
  roleName?: string | null;
  RoleName?: string | null;
  venueIDs?: number[] | null;
  VenueIDs?: number[] | null;
  mobileNumber: string | null;
  emailID: string | null;
  isActive: boolean;
}

interface OfficeUserRoleDto {
  roleID?: number;
  RoleID?: number;
  roleName?: string;
  RoleName?: string;
}

function normalizeBookingStatus(
  raw: string | undefined | null,
  level2UserId?: number | null,
): BookingStatus {
  const s = (raw ?? '').trim();
  if (s === 'Pending' || s === '') {
    if (level2UserId != null && level2UserId > 0) {
      return 'provisionallyApproved';
    }
    return 'pending';
  }
  const sl = s.toLowerCase();
  if (sl === 'forwardedtol2' || sl === 'forwarded' || sl === 'forward') {
    return 'forward';
  }
  if (sl === 'accepted' || sl === 'confirmed' || sl === 'approved' || sl === 'paymentpending') {
    return 'approved';
  }
  if (sl.includes('cancel')) {
    return 'cancelled';
  }
  if (sl.includes('reject')) {
    return 'rejected';
  }
  return 'pending';
}

function mapBooking(b: BookingRecordDto): BookingRecord {
  return {
    id: b.id,
    bookingNo: b.bookingNo,
    fullName: b.fullName,
    mobile: b.mobile,
    email: b.email,
    hall: b.hall,
    address: b.address,
    totalAmount: b.totalAmount,
    category: b.category,
    purpose: b.purpose,
    fromDate: b.fromDate,
    toDate: b.toDate,
    status: normalizeBookingStatus(b.bookingStatusRaw, b.level2UserId),
  };
}

function mapVenue(v: VenueAdminDto): HallDescriptionRecord {
  const extra = parseFacilities(v.facilities);
  return {
    id: String(v.venueID),
    venueTypeId: v.venueTypeID,
    shortCode: v.venueCode,
    name: v.venueName,
    city: (v.city ?? '').trim(),
    gpsLocation: v.googleMapLink ?? '',
    capacity: numOrFacilitiesString(v.capacity, extra, 'capacity'),
    address: v.address,
    areaSqmt: numOrFacilitiesString(v.areaSqMt, extra, 'areaSqmt'),
    rooms: numOrFacilitiesString(v.noOfRooms, extra, 'rooms'),
    kitchen: numOrFacilitiesString(v.noOfKitchen, extra, 'kitchen'),
    toilet: numOrFacilitiesString(v.noOfToilet, extra, 'toilet'),
    bathroom: numOrFacilitiesString(v.noOfBathroom, extra, 'bathroom'),
    facilities: v.facilities ?? '',
    photoDataUrl: '',
    status: v.isActive ? 'Active' : 'Inactive',
  };
}

function numOrFacilitiesString(
  col: number | null | undefined,
  extra: Record<string, string>,
  key: string,
): string {
  if (col != null && !Number.isNaN(Number(col))) {
    return String(col);
  }
  return extra[key] ?? '';
}

function parseOptionalInt(s: string | undefined): number | null {
  if (s == null || !String(s).trim()) {
    return null;
  }
  const n = parseInt(String(s).trim(), 10);
  return Number.isFinite(n) && !Number.isNaN(n) ? n : null;
}

function parseOptionalDecimal(s: string | undefined): number | null {
  if (s == null || !String(s).trim()) {
    return null;
  }
  const n = parseFloat(String(s).trim());
  return Number.isFinite(n) && !Number.isNaN(n) ? n : null;
}

function parseFacilities(json: string | null): Record<string, string> {
  if (!json) {
    return {};
  }
  try {
    const o = JSON.parse(json) as Record<string, unknown>;
    const out: Record<string, string> = {};
    for (const k of ['capacity', 'areaSqmt', 'rooms', 'kitchen', 'toilet', 'bathroom']) {
      if (o[k] != null) {
        out[k] = String(o[k]);
      }
    }
    return out;
  } catch {
    return {};
  }
}

function buildFacilitiesJson(r: Omit<HallDescriptionRecord, 'id' | 'venueTypeId'>): string {
  const o: Record<string, string> = {};
  if (r.capacity) {
    o['capacity'] = r.capacity;
  }
  if (r.areaSqmt) {
    o['areaSqmt'] = r.areaSqmt;
  }
  if (r.rooms) {
    o['rooms'] = r.rooms;
  }
  if (r.kitchen) {
    o['kitchen'] = r.kitchen;
  }
  if (r.toilet) {
    o['toilet'] = r.toilet;
  }
  if (r.bathroom) {
    o['bathroom'] = r.bathroom;
  }
  if (r.facilities) {
    o['notes'] = r.facilities;
  }
  return JSON.stringify(o);
}

function mapRate(r: RateChartDto): RateChartRecord {
  return {
    id: r.id,
    hallName: r.hallName,
    bookingCategory: r.bookingCategory,
    capacityFrom: r.capacityFrom,
    capacityTo: r.capacityTo,
    rate: r.rate,
    effectiveFrom: r.effectiveFrom,
    effectiveTo: r.effectiveTo,
  };
}

function mapOfficeUserRole(d: OfficeUserRoleDto): OfficeUserRoleRecord {
  const rawId = d?.roleID ?? d?.RoleID;
  const id = typeof rawId === 'number' ? rawId : Number(rawId);
  const name = String(d?.roleName ?? d?.RoleName ?? '').trim();
  return { roleID: Number.isFinite(id) && !Number.isNaN(id) ? id : 0, roleName: name };
}

function mapOfficeUser(u: OfficeUserDto): AdminRoleRecord {
  const roleId = u.roleID ?? u.RoleID ?? null;
  const rawVenues = u.venueIDs ?? u.VenueIDs ?? [];
  const venueIds = Array.isArray(rawVenues) ? rawVenues.map((x) => Number(x)).filter((n) => !Number.isNaN(n)) : [];
  const displayRole = (u.roleName ?? u.RoleName ?? '').trim() || (u.role ?? '').trim();
  return {
    id: String(u.officeUserID),
    fullName: u.fullName,
    email: u.emailID ?? u.username,
    mobile: u.mobileNumber ?? '',
    roleName: displayRole,
    roleId,
    venueIds,
    password: '',
  };
}

/** Reads `/api/Documents/upload` JSON whether keys are camelCase or PascalCase. */
function uploadDocumentPathFromResponse(body: unknown): string {
  if (!body || typeof body !== 'object') {
    return '';
  }
  const o = body as Record<string, unknown>;
  const p = o['documentPath'] ?? o['DocumentPath'];
  return typeof p === 'string' ? p.trim() : '';
}

function dataUrlToBlob(dataUrl: string): Blob {
  const i = dataUrl.indexOf(',');
  if (i < 0) {
    throw new Error('invalid data url');
  }
  const meta = dataUrl.slice(0, i);
  const b64 = dataUrl.slice(i + 1);
  const mime = /^data:([^;,]+)/.exec(meta)?.[1] ?? 'application/octet-stream';
  const bin = atob(b64);
  const arr = new Uint8Array(bin.length);
  for (let j = 0; j < bin.length; j++) {
    arr[j] = bin.charCodeAt(j);
  }
  return new Blob([arr], { type: mime });
}

/** Stored path `/uploads/...` from absolute URL or same-origin path. */
function adImageStoragePathFromUrl(src: string, apiBase: string): string {
  const t = src.trim();
  if (t.startsWith('/')) {
    return t.split('?')[0];
  }
  try {
    const u = new URL(t, apiBase || (typeof location !== 'undefined' ? location.origin : 'http://localhost'));
    return u.pathname.split('?')[0];
  } catch {
    return t.split('?')[0];
  }
}

function isoDate(s: string): string {
  if (/^\d{4}-\d{2}-\d{2}$/.test(s)) {
    return s;
  }
  const d = new Date(s);
  if (Number.isNaN(d.getTime())) {
    return s;
  }
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}
