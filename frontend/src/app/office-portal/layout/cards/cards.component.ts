import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminDataService } from '../../core/admin-data.service';
import { AuthService } from '../../core/auth.service';

/** Skeleton slots — max tiles we show on dashboard. */
const KPI_SKEL = [0, 1, 2, 3, 4, 5, 6, 7];

export interface DashboardCard {
  id: 'venues' | 'total' | 'pending' | 'provisional' | 'forward' | 'approved' | 'rejected' | 'cancelled';
  title: string;
  value: number;
  accent:
    | 'indigo'
    | 'ocean'
    | 'amber'
    | 'plum'
    | 'teal'
    | 'emerald'
    | 'rose'
    | 'slate';
  performanceLine: string;
}

@Component({
  selector: 'app-office-cards',
  templateUrl: './cards.component.html',
  styleUrls: ['./cards.component.css'],
})
export class CardsComponent implements OnInit, OnDestroy {
  cards: DashboardCard[] = [];
  readonly kpiSkelSlots = KPI_SKEL;

  private sub?: Subscription;

  get officeDataReady() {
    return this.data.dataHydrated;
  }

  constructor(
    private data: AdminDataService,
    private router: Router,
    private auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.refresh();
    this.sub = this.data.bookings.subscribe(() => this.refresh());
    this.sub.add(this.data.halls.subscribe(() => this.refresh()));
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private refresh(): void {
    const c = this.data.getBookingCounts();
    const venues = this.data.getHalls().filter((h) => h.status !== 'Inactive').length;
    const rid = this.auth.getOfficeRoleId();
    const venuesCard: DashboardCard = {
      id: 'venues',
      title: 'Venues live',
      value: venues,
      accent: 'indigo',
      performanceLine: 'Halls available for booking',
    };
    const totalCard: DashboardCard = {
      id: 'total',
      title: 'Total bookings',
      value: c.total,
      accent: 'ocean',
      performanceLine: 'All statuses in your scope',
    };
    const pendingCard: DashboardCard = {
      id: 'pending',
      title: 'Pending review',
      value: c.pending,
      accent: 'amber',
      performanceLine: 'Awaiting verifying authority',
    };
    const provisionalCard: DashboardCard = {
      id: 'provisional',
      title: 'Provisionally approved',
      value: c.provisional,
      accent: 'plum',
      performanceLine: 'Returned from acceptance — final L1 step',
    };
    const forwardCard: DashboardCard = {
      id: 'forward',
      title: 'Forwarded',
      value: c.forward,
      accent: 'teal',
      performanceLine: 'Awaiting acceptance review',
    };
    const approvedCard: DashboardCard = {
      id: 'approved',
      title: 'Approved',
      value: c.approved,
      accent: 'emerald',
      performanceLine: 'Accepted / confirmed in workflow',
    };
    const rejectedCard: DashboardCard = {
      id: 'rejected',
      title: 'Rejected bookings',
      value: c.rejected,
      accent: 'rose',
      performanceLine: 'Declined in verifying or acceptance review',
    };
    const cancelledCard: DashboardCard = {
      id: 'cancelled',
      title: 'Cancelled bookings',
      value: c.cancelled,
      accent: 'slate',
      performanceLine: 'Cancelled by super admin or customer',
    };

    let cards: DashboardCard[];
    if (rid <= 1) {
      cards = [
        venuesCard,
        totalCard,
        pendingCard,
        provisionalCard,
        forwardCard,
        approvedCard,
        rejectedCard,
        cancelledCard,
      ];
    } else if (rid === 2) {
      cards = [venuesCard, totalCard, pendingCard, provisionalCard, rejectedCard, cancelledCard];
    } else if (rid === 3) {
      cards = [venuesCard, totalCard, forwardCard, approvedCard, rejectedCard, cancelledCard];
    } else {
      cards = [venuesCard, totalCard, pendingCard, forwardCard, approvedCard];
    }
    this.cards = cards;
  }

  trackByCardId(_index: number, card: DashboardCard): string {
    return card.id;
  }

  openCard(card: DashboardCard): void {
    if (card.id === 'venues') {
      void this.router.navigateByUrl('/admin/dashboard/master/venue');
      return;
    }
    if (card.id === 'total') {
      void this.router.navigateByUrl('/admin/dashboard/admin/total-bookings');
      return;
    }
    const map: Record<string, string> = {
      pending: '/admin/dashboard/admin/pending-bookings',
      provisional: '/admin/dashboard/admin/provisionally-approved-bookings',
      forward: '/admin/dashboard/admin/forward-bookings',
      approved: '/admin/dashboard/admin/approved-bookings',
      rejected: '/admin/dashboard/admin/rejected-bookings',
      cancelled: '/admin/dashboard/admin/cancelled-bookings',
    };
    const path = map[card.id];
    if (path) {
      void this.router.navigateByUrl(path);
    }
  }
}
