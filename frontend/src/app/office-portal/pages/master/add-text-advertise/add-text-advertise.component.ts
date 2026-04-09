import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminDataService, TextAdvertiseRecord } from '../../../core/admin-data.service';

@Component({
  selector: 'app-add-text-advertise',
  templateUrl: './add-text-advertise.component.html',
  styleUrls: ['./add-text-advertise.component.css', '../../../shared/admin-forms.css'],
})
export class AddTextAdvertiseComponent implements OnInit, OnDestroy {
  listMode = true;
  rows: TextAdvertiseRecord[] = [];
  editingId: string | null = null;

  startDate = '';
  endDate = '';
  advertise = '';

  private sub?: Subscription;
  private qpSub?: Subscription;

  constructor(
    private data: AdminDataService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.sub = this.data.textAds.subscribe((r) => (this.rows = r));
    this.qpSub = this.route.queryParamMap.subscribe((q) => {
      const edit = q.get('edit');
      if (edit) {
        this.listMode = false;
        const row = this.data.getTextAds().find((x) => x.id === edit);
        if (row) {
          this.editingId = row.id;
          this.startDate = row.startDate;
          this.endDate = row.endDate;
          this.advertise = row.advertise;
        }
      } else {
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
    this.startDate = '';
    this.endDate = '';
    this.advertise = '';
  }

  submit(): void {
    if (!this.startDate || !this.endDate || !this.advertise.trim()) {
      return;
    }
    this.data.upsertTextAd(
      {
        id: this.editingId || undefined,
        startDate: this.startDate,
        endDate: this.endDate,
        advertise: this.advertise.trim(),
      },
      () => {
        this.resetForm();
        void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
      },
    );
  }

  editRow(row: TextAdvertiseRecord): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { edit: row.id } });
  }

  deleteRow(row: TextAdvertiseRecord): void {
    this.data.deleteTextAd(row.id);
  }
}
