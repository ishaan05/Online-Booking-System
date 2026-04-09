import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subject, Subscription, combineLatest } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs/operators';
import { ThemeService } from '../../../core/theme.service';
import {
  AdminDataService,
  AdminRoleRecord,
  BookingRecord,
  HallDescriptionRecord,
} from '../../core/admin-data.service';
import { AuthService } from '../../core/auth.service';

/** Named browsing context: reuses / focuses the public booking tab when possible. */
const ONLINE_BOOKING_WINDOW = 'SECO_OnlineBooking';

export type OfficeSearchHitKind = 'booking' | 'hall' | 'staff';

export interface OfficeSearchHit {
  kind: OfficeSearchHitKind;
  title: string;
  sub: string;
  route: string;
}

@Component({
  selector: 'app-office-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'],
})
export class NavbarComponent implements OnInit, OnDestroy {
  @ViewChild('searchWrap') searchWrap?: ElementRef<HTMLElement>;

  pageTitle = 'Dashboard';
  searchQuery = '';
  searchOpen = false;
  searchHits: OfficeSearchHit[] = [];
  isDarkTheme = false;

  private themeSub?: Subscription;
  private routerSub?: Subscription;
  private dataSub?: Subscription;
  private searchSub?: Subscription;

  private admins: AdminRoleRecord[] = [];
  private halls: HallDescriptionRecord[] = [];
  private bookings: BookingRecord[] = [];

  private readonly searchInput$ = new Subject<string>();

  constructor(
    private router: Router,
    readonly theme: ThemeService,
    readonly auth: AuthService,
    private data: AdminDataService,
  ) {}

  get portalHeaderName(): string {
    const r = this.auth.getOfficeRoleId();
    if (r === 1) {
      return 'Super Admin Portal';
    }
    if (r === 2) {
      return 'Verifying Admin Portal';
    }
    if (r === 3) {
      return 'Approving Admin Portal';
    }
    return 'Admin Portal';
  }

  ngOnInit(): void {
    this.themeSub = this.theme.isDark$.subscribe((d) => (this.isDarkTheme = d));
    this.syncPageTitle(this.router.url);
    this.routerSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => this.syncPageTitle(e.urlAfterRedirects));

    this.dataSub = combineLatest([this.data.admins, this.data.halls, this.data.bookings]).subscribe(([a, h, b]) => {
      this.admins = a;
      this.halls = h;
      this.bookings = b;
      this.runSearch();
    });

    this.searchSub = this.searchInput$.pipe(debounceTime(180), distinctUntilChanged()).subscribe(() => this.runSearch());
  }

  ngOnDestroy(): void {
    this.themeSub?.unsubscribe();
    this.routerSub?.unsubscribe();
    this.dataSub?.unsubscribe();
    this.searchSub?.unsubscribe();
  }

  onSearchInput(): void {
    const t = this.searchQuery.trim();
    this.searchOpen = t.length >= 2;
    this.searchInput$.next(this.searchQuery);
  }

  onSearchFocus(): void {
    const t = this.searchQuery.trim();
    if (t.length >= 2) {
      this.searchOpen = true;
      this.runSearch();
    }
  }

  closeSearch(): void {
    this.searchOpen = false;
  }

  @HostListener('document:click', ['$event'])
  onDocClick(ev: MouseEvent): void {
    const el = this.searchWrap?.nativeElement;
    if (el && !el.contains(ev.target as Node)) {
      this.searchOpen = false;
    }
  }

  openHit(hit: OfficeSearchHit): void {
    void this.router.navigateByUrl(hit.route);
    this.searchQuery = '';
    this.searchHits = [];
    this.searchOpen = false;
  }

  filterClick(ev: Event): void {
    ev.preventDefault();
    const t = this.searchQuery.trim();
    if (t.length >= 2) {
      this.searchOpen = true;
      this.runSearch();
    }
  }

  private runSearch(): void {
    const q = this.searchQuery.trim().toLowerCase();
    if (q.length < 2) {
      this.searchHits = [];
      return;
    }

    const hits: OfficeSearchHit[] = [];
    const pushUnique = (h: OfficeSearchHit) => {
      if (hits.length >= 12) {
        return;
      }
      if (!hits.some((x) => x.route === h.route && x.title === h.title)) {
        hits.push(h);
      }
    };

    for (const b of this.bookings) {
      const hay = [b.bookingNo, b.fullName, b.mobile, b.email, b.hall, b.id].join(' ').toLowerCase();
      if (hay.includes(q)) {
        pushUnique({
          kind: 'booking',
          title: b.bookingNo || `Booking ${b.id}`,
          sub: `${b.fullName} · ${b.hall}`,
          route: `/admin/dashboard/admin/admin-booking?booking=${encodeURIComponent(b.id)}`,
        });
      }
    }

    for (const h of this.halls) {
      const hay = [h.name, h.shortCode, h.address, h.city ?? '', h.id].join(' ').toLowerCase();
      if (hay.includes(q)) {
        pushUnique({
          kind: 'hall',
          title: h.name,
          sub: h.shortCode ? `${h.shortCode}${h.city ? ' · ' + h.city : ''}` : 'Hall / venue',
          route: `/admin/dashboard/master/venue?edit=${encodeURIComponent(h.id)}`,
        });
      }
    }

    for (const a of this.admins) {
      const hay = [a.fullName, a.email, a.mobile, a.roleName].join(' ').toLowerCase();
      if (hay.includes(q)) {
        pushUnique({
          kind: 'staff',
          title: a.fullName,
          sub: `${a.roleName} · ${a.email || a.mobile || 'Staff'}`,
          route: `/admin/dashboard/master/add-employee?edit=${encodeURIComponent(a.id)}`,
        });
      }
    }

    this.searchHits = hits;
  }

  quickCreate(): void {
    if (!this.auth.isSuperAdmin()) {
      return;
    }
    void this.router.navigateByUrl('/admin/dashboard/admin/admin-booking');
  }

  openOnlineBooking(): void {
    const url = `${window.location.origin}/`;
    const w = window.open(url, ONLINE_BOOKING_WINDOW);
    if (w) {
      try {
        w.location.assign(url);
      } catch {
        /* cross-origin or restricted — focus only */
      }
      try {
        w.focus();
      } catch {
        /* noop */
      }
    }
  }

  private syncPageTitle(url: string): void {
    const u = url.split('?')[0].replace(/\/$/, '') || '/';
    const routes: { prefix: string; title: string }[] = [
      { prefix: '/admin/dashboard/admin/total-bookings', title: 'Total bookings' },
      { prefix: '/admin/dashboard/admin/pending-bookings', title: 'Pending bookings' },
      { prefix: '/admin/dashboard/admin/provisionally-approved-bookings', title: 'Provisionally approved' },
      { prefix: '/admin/dashboard/admin/rejected-bookings', title: 'Rejected bookings' },
      { prefix: '/admin/dashboard/admin/cancelled-bookings', title: 'Cancelled bookings' },
      { prefix: '/admin/dashboard/admin/forward-bookings', title: 'Forward bookings' },
      { prefix: '/admin/dashboard/admin/approved-bookings', title: 'Approved bookings' },
      { prefix: '/admin/dashboard/admin/admin-booking', title: 'Admin booking' },
      { prefix: '/admin/dashboard/admin/change-password', title: 'Settings' },
      { prefix: '/admin/dashboard/master/add-venue-type', title: 'Add/Modify Venue Type' },
      { prefix: '/admin/dashboard/master/venue', title: 'Add/Modify Venue' },
      { prefix: '/admin/dashboard/master/add-employee', title: 'Add/Modify Role' },
      { prefix: '/admin/dashboard/master/add-rate-chart', title: 'Add/Modify Rates & Capacity' },
      { prefix: '/admin/dashboard/master/add-category', title: 'Add/Modify Category & Purpose' },
      { prefix: '/admin/dashboard/master/add-account-details', title: 'Add/Modify Bank Details' },
      { prefix: '/admin/dashboard/master/add-text-advertise', title: 'Add/Modify Text Advertisement' },
      { prefix: '/admin/dashboard/master/add-image-advertise', title: 'Add/Modify Image Advertisement' },
      { prefix: '/admin/dashboard/master/add-image-banner', title: 'Add Dashboard Banner' },
      { prefix: '/admin/dashboard/admin/cancel-bookings', title: 'Cancel booking' },
      { prefix: '/admin/dashboard', title: 'Dashboard' },
    ];
    for (const r of routes) {
      if (u === r.prefix || u.startsWith(r.prefix + '/')) {
        this.pageTitle = r.title;
        return;
      }
    }
    this.pageTitle = 'Super Admin';
  }
}
