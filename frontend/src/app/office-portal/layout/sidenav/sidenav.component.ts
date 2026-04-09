import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { AdminDataService } from '../../core/admin-data.service';
import { AuthService } from '../../core/auth.service';
import { LayoutService } from '../../core/layout.service';

export interface MasterNavLink {
  path: string;
  label: string;
}

export interface AdminNavLink {
  path: string;
  label: string;
  icon: 'pending' | 'provisionally' | 'forward' | 'approved' | 'rejected' | 'cancelled' | 'booking' | 'cancel';
  /** Show pending count badge */
  pendingBadge?: boolean;
  /** Only RoleId 1 (super admin) */
  superOnly?: boolean;
}

@Component({
  selector: 'app-office-sidenav',
  templateUrl: './sidenav.component.html',
  styleUrls: ['./sidenav.component.css'],
})
export class SidenavComponent implements OnInit, OnDestroy {
  sidebarCollapsed = false;
  masterOpen = false;
  isMasterRoute = false;
  pendingCount = 0;
  userDisplayName = '';
  userInitials = '?';

  private sub?: Subscription;

  readonly masterLinks: MasterNavLink[] = [
    { path: '/admin/dashboard/master/add-venue-type', label: 'Add/Modify Venue Type' },
    { path: '/admin/dashboard/master/venue', label: 'Add/Modify Venue' },
    { path: '/admin/dashboard/master/add-employee', label: 'Add/Modify Role' },
    { path: '/admin/dashboard/master/add-rate-chart', label: 'Add/Modify Rates & Capacity' },
    { path: '/admin/dashboard/master/add-category', label: 'Add/Modify Category & Purpose' },
    { path: '/admin/dashboard/master/add-account-details', label: 'Add/Modify Bank Details' },
    { path: '/admin/dashboard/master/add-text-advertise', label: 'Add/Modify Text Advertisement' },
    { path: '/admin/dashboard/master/add-image-advertise', label: 'Add/Modify Image Advertisement' },
    { path: '/admin/dashboard/master/add-image-banner', label: 'Add Dashboard Banner' },
  ];

  readonly adminLinks: AdminNavLink[] = [
    {
      path: '/admin/dashboard/admin/pending-bookings',
      label: 'Pending Bookings',
      icon: 'pending',
      pendingBadge: true,
    },
    {
      path: '/admin/dashboard/admin/provisionally-approved-bookings',
      label: 'Provisionally approved',
      icon: 'provisionally',
    },
    { path: '/admin/dashboard/admin/forward-bookings', label: 'Forward Bookings', icon: 'forward' },
    { path: '/admin/dashboard/admin/approved-bookings', label: 'Approved Bookings', icon: 'approved' },
    { path: '/admin/dashboard/admin/rejected-bookings', label: 'Rejected bookings', icon: 'rejected' },
    { path: '/admin/dashboard/admin/cancelled-bookings', label: 'Cancelled bookings', icon: 'cancelled' },
    { path: '/admin/dashboard/admin/admin-booking', label: 'Admin Booking', icon: 'booking', superOnly: true },
    { path: '/admin/dashboard/admin/cancel-bookings', label: 'Cancel Booking', icon: 'cancel', superOnly: true },
  ];

  constructor(
    readonly router: Router,
    private layout: LayoutService,
    private auth: AuthService,
    private data: AdminDataService,
  ) {}

  ngOnInit(): void {
    this.syncUser();
    this.syncPending();
    this.syncMasterFromUrl(this.router.url);
    this.sub = this.layout.sidebarCollapsed.subscribe((c) => {
      this.sidebarCollapsed = c;
    });
    this.sub.add(
      this.router.events
        .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
        .subscribe((e) => {
          this.syncMasterFromUrl(e.urlAfterRedirects);
        }),
    );
    this.sub.add(this.data.bookings.subscribe(() => this.syncPending()));
  }

  private syncUser(): void {
    const full = this.auth.getOfficeFullName();
    this.userDisplayName = full || this.auth.getWelcomeName();
    const parts = this.userDisplayName.trim().split(/\s+/).filter(Boolean);
    if (!parts.length) {
      this.userInitials = '?';
      return;
    }
    if (parts.length === 1) {
      this.userInitials = parts[0].slice(0, 2).toUpperCase();
      return;
    }
    this.userInitials = (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  private syncPending(): void {
    this.pendingCount = this.data.getBookingCounts().pending;
  }

  private syncMasterFromUrl(url: string): void {
    this.isMasterRoute = url.includes('/admin/dashboard/master/');
    this.masterOpen = this.isMasterRoute;
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  get visibleMasterLinks(): MasterNavLink[] {
    if (this.auth.isSuperAdmin()) {
      return this.masterLinks;
    }
    return this.masterLinks.filter((l) => l.path === '/admin/dashboard/master/venue');
  }

  get visibleAdminLinks(): AdminNavLink[] {
    const rid = this.auth.getOfficeRoleId();
    if (rid === 0) {
      return [];
    }
    const withRole = (pred: (l: AdminNavLink) => boolean): AdminNavLink[] =>
      this.adminLinks.filter((l) => (!l.superOnly || this.auth.isSuperAdmin()) && pred(l));
    if (rid === 1) {
      return withRole(() => true);
    }
    if (rid === 2) {
      return withRole(
        (l) =>
          l.icon === 'pending' ||
          l.icon === 'provisionally' ||
          l.icon === 'rejected' ||
          l.icon === 'cancelled',
      );
    }
    if (rid === 3) {
      return withRole(
        (l) =>
          l.icon === 'forward' || l.icon === 'approved' || l.icon === 'rejected' || l.icon === 'cancelled',
      );
    }
    return withRole(() => true);
  }

  masterToggleClick(): void {
    if (this.sidebarCollapsed) {
      this.layout.setSidebarCollapsed(false);
      if (!this.isMasterRoute) {
        this.masterOpen = true;
      }
      return;
    }
    if (this.isMasterRoute) {
      return;
    }
    this.masterOpen = !this.masterOpen;
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/admin/login');
  }

  toggleSidebar(): void {
    this.layout.toggleSidebarCollapsed();
  }
}
