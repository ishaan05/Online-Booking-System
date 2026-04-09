import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminDataService, VenueTypeRecord } from '../../../core/admin-data.service';

@Component({
  selector: 'app-add-venue-type',
  templateUrl: './add-venue-type.component.html',
  styleUrls: ['./add-venue-type.component.css', '../../../shared/admin-forms.css'],
})
export class AddVenueTypeComponent implements OnInit, OnDestroy {
  listMode = true;
  rows: VenueTypeRecord[] = [];
  editingId: string | null = null;
  typeName = '';
  /** Checked = active in VenueType table */
  isActive = true;

  private sub?: Subscription;
  private qpSub?: Subscription;

  constructor(
    private data: AdminDataService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.sub = this.data.venueTypes.subscribe((r) => (this.rows = r));
    this.qpSub = this.route.queryParamMap.subscribe((q) => {
      const edit = q.get('edit');
      const add = q.get('add');
      if (edit) {
        this.listMode = false;
        const row = this.data.getVenueTypes().find((x) => x.id === edit);
        if (row) {
          this.editingId = row.id;
          this.typeName = row.typeName;
          this.isActive = row.isActive;
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
    this.qpSub?.unsubscribe();
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
    this.typeName = '';
    this.isActive = true;
  }

  submit(): void {
    if (!this.typeName.trim()) {
      return;
    }
    this.data.upsertVenueType({
      id: this.editingId || undefined,
      typeName: this.typeName.trim(),
      isActive: this.isActive,
    });
    this.resetForm();
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  editRow(row: VenueTypeRecord): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { edit: row.id } });
  }
}
