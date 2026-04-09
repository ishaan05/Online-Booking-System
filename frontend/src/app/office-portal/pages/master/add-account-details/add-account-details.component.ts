import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminDataService, AccountDetailsRecord } from '../../../core/admin-data.service';

@Component({
  selector: 'app-add-account-details',
  templateUrl: './add-account-details.component.html',
  styleUrls: ['./add-account-details.component.css', '../../../shared/admin-forms.css'],
})
export class AddAccountDetailsComponent implements OnInit, OnDestroy {
  listMode = true;
  rows: AccountDetailsRecord[] = [];
  hallOptions: string[] = [];
  editingId: string | null = null;

  hallName = '';
  bankName = '';
  accountNo = '';
  bankAddress = '';
  ifsc = '';
  contactName = '';
  mobile = '';
  chequeInFavour = '';

  private sub?: Subscription;
  private qpSub?: Subscription;

  constructor(
    private data: AdminDataService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.sub = this.data.accounts.subscribe((r) => (this.rows = r));
    this.refreshHalls();
    this.sub.add(this.data.halls.subscribe(() => this.refreshHalls()));
    this.qpSub = this.route.queryParamMap.subscribe((q) => {
      const edit = q.get('edit');
      const add = q.get('add');
      if (edit) {
        this.listMode = false;
        const row = this.data.getAccounts().find((x) => x.id === edit);
        if (row) {
          this.editingId = row.id;
          this.hallName = row.hallName;
          this.bankName = row.bankName;
          this.accountNo = row.accountNo;
          this.bankAddress = row.bankAddress;
          this.ifsc = row.ifsc;
          this.contactName = row.contactName;
          this.mobile = row.mobile;
          this.chequeInFavour = row.chequeInFavour;
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
    this.bankName = '';
    this.accountNo = '';
    this.bankAddress = '';
    this.ifsc = '';
    this.contactName = '';
    this.mobile = '';
    this.chequeInFavour = '';
  }

  submit(): void {
    if (!this.hallName.trim()) {
      return;
    }
    this.data.upsertAccount({
      id: this.editingId || undefined,
      hallName: this.hallName.trim(),
      bankName: this.bankName.trim(),
      accountNo: this.accountNo.trim(),
      bankAddress: this.bankAddress.trim(),
      ifsc: this.ifsc.trim(),
      contactName: this.contactName.trim(),
      mobile: this.mobile.trim(),
      chequeInFavour: this.chequeInFavour.trim(),
    });
    this.resetForm();
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  editRow(row: AccountDetailsRecord): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { edit: row.id } });
  }

  deleteRow(row: AccountDetailsRecord): void {
    this.data.deleteAccount(row.id);
  }
}
