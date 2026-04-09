import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import {
  AdminDataService,
  AdminRoleRecord,
  HallDescriptionRecord,
  OfficeUserRoleRecord,
} from '../../../core/admin-data.service';

@Component({
  selector: 'app-add-employee',
  templateUrl: './add-employee.component.html',
  styleUrls: ['../../../shared/admin-forms.css', './add-employee.component.css'],
})
export class AddEmployeeComponent implements OnInit, OnDestroy {
  listMode = true;
  rows: AdminRoleRecord[] = [];
  editingId: string | null = null;

  fullName = '';
  email = '';
  mobile = '';
  /** Bound to OfficeUserRole.RoleID in the dropdown. */
  roleId: number | null = null;
  venueIds: number[] = [];
  password = '';

  activeVenues: HallDescriptionRecord[] = [];
  officeRoles: OfficeUserRoleRecord[] = [];

  private sub?: Subscription;
  private hallsSub?: Subscription;
  private rolesSub?: Subscription;
  private qpSub?: Subscription;

  constructor(
    private data: AdminDataService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.data.refreshOfficeUserRoles();
    this.sub = this.data.admins.subscribe((r) => (this.rows = r));
    this.hallsSub = this.data.halls.subscribe((h) => {
      this.activeVenues = h
        .filter((x) => x.status === 'Active')
        .sort((a, b) => a.name.localeCompare(b.name));
    });
    this.rolesSub = this.data.officeUserRoles.subscribe((r) => (this.officeRoles = r));
    this.qpSub = this.route.queryParamMap.subscribe((q) => {
      const edit = q.get('edit');
      const add = q.get('add');
      if (edit) {
        this.listMode = false;
        const row = this.data.getAdmins().find((x) => x.id === edit);
        if (row) {
          this.editingId = row.id;
          this.fullName = row.fullName;
          this.email = row.email;
          this.mobile = row.mobile;
          this.roleId = row.roleId;
          this.venueIds = [...(row.venueIds ?? [])];
          this.password = row.password;
        }
      } else if (add === '1') {
        this.listMode = false;
        this.resetForm();
      } else {
        this.listMode = true;
        this.editingId = null;
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.hallsSub?.unsubscribe();
    this.rolesSub?.unsubscribe();
    this.qpSub?.unsubscribe();
  }

  /** Text after the venue name (comma + city + code), for bold name + normal suffix in the template. */
  venueLabelSuffix(h: HallDescriptionRecord): string {
    const city = (h.city ?? '').trim();
    const code = (h.shortCode ?? '').trim();
    if (city && code) {
      return `, ${city} (${code})`;
    }
    if (city) {
      return `, ${city}`;
    }
    return '';
  }

  venueLabel(h: HallDescriptionRecord): string {
    return `${h.name.trim()}${this.venueLabelSuffix(h)}`;
  }

  /** Super Admin (RoleID 1) has all halls; hall checkboxes apply only to Verifying / Approving roles. */
  get showVenueAssignment(): boolean {
    return this.roleId === 2 || this.roleId === 3;
  }

  isVenueChecked(venueId: number): boolean {
    return this.venueIds.includes(venueId);
  }

  toggleVenue(venueId: number): void {
    const i = this.venueIds.indexOf(venueId);
    if (i >= 0) {
      this.venueIds = [...this.venueIds.slice(0, i), ...this.venueIds.slice(i + 1)];
    } else {
      this.venueIds = [...this.venueIds, venueId];
    }
  }

  venueSummary(row: AdminRoleRecord): string {
    const ids = row.venueIds ?? [];
    if (!ids.length) {
      return '—';
    }
    const halls = this.data.getHalls();
    return ids
      .map((id) => {
        const h = halls.find((x) => x.id === String(id));
        return h ? this.venueLabel(h) : `#${id}`;
      })
      .join('; ');
  }

  showView(): void {
    this.listMode = true;
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  goAdd(): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { add: '1' } });
  }

  resetForm(): void {
    this.editingId = null;
    this.fullName = '';
    this.email = '';
    this.mobile = '';
    this.roleId = null;
    this.venueIds = [];
    this.password = '';
  }

  submit(): void {
    if (!this.fullName.trim() || !this.email.trim() || this.roleId == null) {
      return;
    }
    const role = this.officeRoles.find((r) => r.roleID === this.roleId);
    const vids = this.roleId === 1 ? [] : [...this.venueIds];
    this.data.upsertAdmin({
      id: this.editingId || undefined,
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      mobile: this.mobile.trim(),
      roleName: role?.roleName ?? '',
      roleId: this.roleId,
      venueIds: vids,
      password: this.password,
    });
    this.resetForm();
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  editRow(row: AdminRoleRecord): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { edit: row.id },
    });
  }

  deleteRow(row: AdminRoleRecord): void {
    this.data.deleteAdmin(row.id);
  }
}
