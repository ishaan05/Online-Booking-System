import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminDataService, ImageAdvertiseRecord } from '../../../core/admin-data.service';

@Component({
  selector: 'app-add-image-advertise',
  templateUrl: './add-image-advertise.component.html',
  styleUrls: ['./add-image-advertise.component.css', '../../../shared/admin-forms.css'],
})
export class AddImageAdvertiseComponent implements OnInit, OnDestroy {
  listMode = true;
  rows: ImageAdvertiseRecord[] = [];
  editingId: string | null = null;

  startDate = '';
  endDate = '';
  imageDataUrl = '';
  pickedImageName = '';

  private sub?: Subscription;
  private qpSub?: Subscription;

  constructor(
    private data: AdminDataService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.sub = this.data.imageAds.subscribe((r) => (this.rows = r));
    this.qpSub = this.route.queryParamMap.subscribe((q) => {
      const edit = q.get('edit');
      const add = q.get('add');
      if (edit) {
        this.listMode = false;
        const row = this.data.getImageAds().find((x) => x.id === edit);
        if (row) {
          this.editingId = row.id;
          this.startDate = row.startDate;
          this.endDate = row.endDate;
          this.imageDataUrl = row.imageDataUrl;
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

  get imagePickLabel(): string {
    if (this.pickedImageName) {
      return this.pickedImageName;
    }
    if (this.imageDataUrl) {
      return 'Current image attached';
    }
    return '';
  }

  onFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      this.pickedImageName = '';
      return;
    }
    this.loadImageFile(file);
  }

  onBannerDragOver(ev: DragEvent): void {
    ev.preventDefault();
    ev.stopPropagation();
    if (ev.dataTransfer) {
      ev.dataTransfer.dropEffect = 'copy';
    }
  }

  onBannerDrop(ev: DragEvent): void {
    ev.preventDefault();
    ev.stopPropagation();
    const file = ev.dataTransfer?.files?.[0];
    if (!file || !file.type.startsWith('image/')) {
      return;
    }
    this.loadImageFile(file);
    const input = document.getElementById('ia-f') as HTMLInputElement | null;
    if (input) {
      input.value = '';
    }
  }

  private loadImageFile(file: File): void {
    this.pickedImageName = file.name;
    const reader = new FileReader();
    reader.onload = () => {
      this.imageDataUrl = typeof reader.result === 'string' ? reader.result : '';
    };
    reader.readAsDataURL(file);
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
    this.imageDataUrl = '';
    this.pickedImageName = '';
  }

  submit(): void {
    if (!this.startDate || !this.endDate || !this.imageDataUrl) {
      return;
    }
    this.data.upsertImageAd(
      {
        id: this.editingId || undefined,
        startDate: this.startDate,
        endDate: this.endDate,
        imageDataUrl: this.imageDataUrl,
      },
      () => {
        this.resetForm();
        void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
      },
    );
  }

  editRow(row: ImageAdvertiseRecord): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { edit: row.id } });
  }

  deleteRow(row: ImageAdvertiseRecord): void {
    this.data.deleteImageAd(row.id);
  }
}
