import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminDataService, CategoryRecord, PurposeRecord } from '../../../core/admin-data.service';

@Component({
  selector: 'app-add-category',
  templateUrl: './add-category.component.html',
  styleUrls: ['./add-category.component.css', '../../../shared/admin-forms.css'],
})
export class AddCategoryComponent implements OnInit, OnDestroy {
  listMode = true;
  /** Which form is shown when not in list mode */
  formKind: 'category' | 'purpose' | null = null;

  categoryRows: CategoryRecord[] = [];
  purposeRows: PurposeRecord[] = [];

  catEditId: string | null = null;
  categoryType = '';
  identityLabel = '';
  identityFormat = '';
  documentLabel = '';
  categoryIsActive = true;

  purEditId: string | null = null;
  purposeName = '';
  maxDays = 3;
  purposeIsActive = true;

  private sub?: Subscription;
  private qpSub?: Subscription;

  constructor(
    private data: AdminDataService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.sub = this.data.categories.subscribe((r) => {
      this.categoryRows = r;
      const edit = this.route.snapshot.queryParamMap.get('edit');
      if (edit && !this.listMode && this.formKind === 'category') {
        const row = r.find((x) => x.id === edit);
        if (row) {
          this.applyCategory(row);
        }
      }
    });
    this.sub.add(
      this.data.purposesRows.subscribe((r) => {
        this.purposeRows = r;
        const purEdit = this.route.snapshot.queryParamMap.get('purEdit');
        if (purEdit && !this.listMode && this.formKind === 'purpose') {
          const row = r.find((x) => x.id === purEdit);
          if (row) {
            this.applyPurpose(row);
          }
        }
      }),
    );
    this.qpSub = this.route.queryParamMap.subscribe((q) => {
      const edit = q.get('edit');
      const purEdit = q.get('purEdit');
      const add = q.get('add');
      if (edit) {
        this.listMode = false;
        this.formKind = 'category';
        this.purEditId = null;
        const row = this.data.getCategories().find((x) => x.id === edit);
        if (row) {
          this.applyCategory(row);
        }
      } else if (purEdit) {
        this.listMode = false;
        this.formKind = 'purpose';
        this.catEditId = null;
        const row = this.data.getPurposes().find((x) => x.id === purEdit);
        if (row) {
          this.applyPurpose(row);
        }
      } else if (add === 'category') {
        this.listMode = false;
        this.formKind = 'category';
        this.resetCategoryForm();
      } else if (add === 'purpose') {
        this.listMode = false;
        this.formKind = 'purpose';
        this.resetPurposeForm();
      } else {
        this.listMode = true;
        this.formKind = null;
        this.catEditId = null;
        this.purEditId = null;
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.qpSub?.unsubscribe();
  }

  private applyCategory(row: CategoryRecord): void {
    this.catEditId = row.id;
    this.categoryType = row.categoryType;
    this.identityLabel = row.identityLabel;
    this.identityFormat = row.identityFormat;
    this.documentLabel = row.documentLabel;
    this.categoryIsActive = row.isActive;
  }

  private applyPurpose(row: PurposeRecord): void {
    this.purEditId = row.id;
    this.purposeName = row.purposeName;
    this.maxDays = row.maxDays;
    this.purposeIsActive = row.isActive;
  }

  showView(): void {
    this.listMode = true;
    this.formKind = null;
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  goAddCategory(): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { add: 'category' } });
  }

  goAddPurpose(): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { add: 'purpose' } });
  }

  resetCategoryForm(): void {
    this.catEditId = null;
    this.categoryType = '';
    this.identityLabel = '';
    this.identityFormat = '';
    this.documentLabel = '';
    this.categoryIsActive = true;
  }

  resetPurposeForm(): void {
    this.purEditId = null;
    this.purposeName = '';
    this.maxDays = 3;
    this.purposeIsActive = true;
  }

  submitCategory(): void {
    if (!this.categoryType.trim()) {
      return;
    }
    this.data.upsertCategory({
      id: this.catEditId || undefined,
      categoryType: this.categoryType.trim(),
      identityLabel: this.identityLabel.trim(),
      identityFormat: this.identityFormat.trim(),
      documentLabel: this.documentLabel.trim(),
      isActive: this.categoryIsActive,
    });
    this.resetCategoryForm();
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  submitPurpose(): void {
    if (!this.purposeName.trim()) {
      return;
    }
    const md = Number(this.maxDays);
    const maxD = Number.isFinite(md) && md >= 1 ? Math.min(365, Math.floor(md)) : 1;
    this.data.upsertPurpose({
      id: this.purEditId || undefined,
      purposeName: this.purposeName.trim(),
      maxDays: maxD,
      isActive: this.purposeIsActive,
    });
    this.resetPurposeForm();
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  editCategoryRow(row: CategoryRecord): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { edit: row.id } });
  }

  editPurposeRow(row: PurposeRecord): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { purEdit: row.id } });
  }

  deleteCategoryRow(row: CategoryRecord): void {
    this.data.deleteCategory(row.id);
  }

  deletePurposeRow(row: PurposeRecord): void {
    this.data.deletePurpose(row.id);
  }
}
