import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminDataService, RateChartRecord } from '../../../core/admin-data.service';

@Component({
  selector: 'app-add-rate-chart',
  templateUrl: './add-rate-chart.component.html',
  styleUrls: ['./add-rate-chart.component.css', '../../../shared/admin-forms.css'],
})
export class AddRateChartComponent implements OnInit, OnDestroy {
  listMode = true;
  rows: RateChartRecord[] = [];
  hallOptions: string[] = [];
  editingId: string | null = null;

  hallName = '';
  bookingCategory = '';
  capacityFrom = '';
  capacityTo = '';
  rate = '';
  effectiveFrom = '';
  effectiveTo = '';

  private sub?: Subscription;
  private qpSub?: Subscription;

  constructor(
    private data: AdminDataService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.sub = this.data.rateCharts.subscribe((r) => (this.rows = r));
    this.refreshHalls();
    this.sub.add(this.data.halls.subscribe(() => this.refreshHalls()));
    this.qpSub = this.route.queryParamMap.subscribe((q) => {
      const edit = q.get('edit');
      const add = q.get('add');
      if (edit) {
        this.listMode = false;
        const row = this.data.getRateCharts().find((x) => x.id === edit);
        if (row) {
          this.editingId = row.id;
          this.hallName = row.hallName;
          this.bookingCategory = row.bookingCategory;
          this.capacityFrom = row.capacityFrom;
          this.capacityTo = row.capacityTo;
          this.rate = row.rate;
          this.effectiveFrom = row.effectiveFrom;
          this.effectiveTo = row.effectiveTo;
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

  private refreshHalls(): void {
    this.hallOptions = this.data.getHallOptions();
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
    this.hallName = '';
    this.bookingCategory = '';
    this.capacityFrom = '';
    this.capacityTo = '';
    this.rate = '';
    this.effectiveFrom = '';
    this.effectiveTo = '';
  }

  submit(): void {
    if (!this.hallName.trim()) {
      return;
    }
    this.data.upsertRateChart({
      id: this.editingId || undefined,
      hallName: this.hallName.trim(),
      bookingCategory: this.bookingCategory.trim(),
      capacityFrom: this.capacityFrom.trim(),
      capacityTo: this.capacityTo.trim(),
      rate: this.rate.trim(),
      effectiveFrom: this.effectiveFrom,
      effectiveTo: this.effectiveTo,
    });
    this.resetForm();
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  editRow(row: RateChartRecord): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { edit: row.id } });
  }

  deleteRow(row: RateChartRecord): void {
    this.data.deleteRateChart(row.id);
  }
}
