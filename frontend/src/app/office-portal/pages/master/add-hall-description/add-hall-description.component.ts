import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminDataService, HallDescriptionRecord } from '../../../core/admin-data.service';
import { AuthService } from '../../../core/auth.service';

@Component({
  selector: 'app-add-hall-description',
  templateUrl: './add-hall-description.component.html',
  styleUrls: ['./add-hall-description.component.css', '../../../shared/admin-forms.css'],
})
export class AddHallDescriptionComponent implements OnInit, OnDestroy {
  listMode = true;
  rows: HallDescriptionRecord[] = [];
  editingId: string | null = null;

  shortCode = '';
  /** Selected VenueType.VenueTypeID */
  venueTypeId = 1;
  name = '';
  gpsLocation = '';
  capacity = '';
  address = '';
  areaSqmt = '';
  rooms = '';
  kitchen = '';
  toilet = '';
  bathroom = '';
  facilities = '';
  photoDataUrl = '';
  status = 'Active';
  /** Last chosen file name for UI; cleared on reset */
  pickedPhotoName = '';

  private sub?: Subscription;
  private qpSub?: Subscription;

  constructor(
    private data: AdminDataService,
    private route: ActivatedRoute,
    private router: Router,
    readonly auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.sub = this.data.halls.subscribe((r) => {
      this.rows = [...r].sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }));
      const edit = this.route.snapshot.queryParamMap.get('edit');
      if (edit && !this.listMode) {
        const row = this.rows.find((x) => x.id === edit);
        if (row) {
          this.applyRow(row);
        }
      }
    });
    this.qpSub = this.route.queryParamMap.subscribe((q) => {
      const edit = q.get('edit');
      const add = q.get('add');
      const superOnly = edit || add === '1';
      if (superOnly && !this.auth.isSuperAdmin()) {
        this.listMode = true;
        this.editingId = null;
        void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
        return;
      }
      if (edit) {
        this.listMode = false;
        const row = this.data.getHalls().find((x) => x.id === edit);
        if (row) {
          this.applyRow(row);
        }
      } else if (add === '1') {
        this.listMode = false;
        this.editingId = null;
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

  private applyRow(row: HallDescriptionRecord): void {
    this.editingId = row.id;
    this.shortCode = row.shortCode;
    this.venueTypeId = row.venueTypeId > 0 ? row.venueTypeId : 1;
    this.name = row.name;
    this.gpsLocation = row.gpsLocation;
    this.capacity = row.capacity;
    this.address = row.address;
    this.areaSqmt = row.areaSqmt;
    this.rooms = row.rooms;
    this.kitchen = row.kitchen;
    this.toilet = row.toilet;
    this.bathroom = row.bathroom;
    this.facilities = row.facilities;
    this.photoDataUrl = row.photoDataUrl;
    this.status = row.status;
  }

  get photoPickLabel(): string {
    if (this.pickedPhotoName) {
      return this.pickedPhotoName;
    }
    if (this.photoDataUrl) {
      return 'Current photo attached';
    }
    return '';
  }

  onPhotoChange(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      this.pickedPhotoName = '';
      return;
    }
    this.loadPhotoFile(file);
  }

  onPhotoDragOver(ev: DragEvent): void {
    ev.preventDefault();
    ev.stopPropagation();
    if (ev.dataTransfer) {
      ev.dataTransfer.dropEffect = 'copy';
    }
  }

  onPhotoDrop(ev: DragEvent): void {
    ev.preventDefault();
    ev.stopPropagation();
    const file = ev.dataTransfer?.files?.[0];
    if (!file || !file.type.startsWith('image/')) {
      return;
    }
    this.loadPhotoFile(file);
    const input = document.getElementById('h-photo') as HTMLInputElement | null;
    if (input) {
      input.value = '';
    }
  }

  private loadPhotoFile(file: File): void {
    this.pickedPhotoName = file.name;
    const reader = new FileReader();
    reader.onload = () => {
      this.photoDataUrl = typeof reader.result === 'string' ? reader.result : '';
    };
    reader.readAsDataURL(file);
  }

  showView(): void {
    this.listMode = true;
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  goAddVenue(): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { add: '1' } });
  }

  resetForm(): void {
    this.editingId = null;
    this.shortCode = '';
    this.venueTypeId = this.defaultVenueTypeId();
    this.name = '';
    this.gpsLocation = '';
    this.capacity = '';
    this.address = '';
    this.areaSqmt = '';
    this.rooms = '';
    this.kitchen = '';
    this.toilet = '';
    this.bathroom = '';
    this.facilities = '';
    this.photoDataUrl = '';
    this.pickedPhotoName = '';
    this.status = 'Active';
  }

  /** Venue types for dropdown (Super Admin); falls back to empty — save still uses venueTypeId. */
  get venueTypeOptions() {
    const all = this.data.getVenueTypes();
    if (!this.editingId) {
      const active = all.filter((t) => t.isActive);
      return active.length ? active : all;
    }
    const row = this.data.getHalls().find((x) => x.id === this.editingId);
    const tid = row?.venueTypeId;
    const active = all.filter((t) => t.isActive);
    if (tid != null && tid > 0 && !active.some((t) => Number(t.id) === tid)) {
      const cur = all.find((t) => Number(t.id) === tid);
      if (cur) {
        return [...active, cur];
      }
    }
    return active.length ? active : all;
  }

  private defaultVenueTypeId(): number {
    const active = this.data.getVenueTypes().filter((t) => t.isActive);
    const first = (active.length ? active : this.data.getVenueTypes())[0];
    return first ? Number(first.id) : 1;
  }

  submit(): void {
    if (!this.auth.isSuperAdmin()) {
      return;
    }
    if (!this.shortCode.trim() || !this.name.trim()) {
      return;
    }
    if (!this.venueTypeId || this.venueTypeId < 1) {
      return;
    }
    this.data.upsertHall({
      id: this.editingId || undefined,
      venueTypeId: this.venueTypeId,
      shortCode: this.shortCode.trim(),
      name: this.name.trim(),
      gpsLocation: this.gpsLocation.trim(),
      capacity: this.capacity.trim(),
      address: this.address.trim(),
      areaSqmt: this.areaSqmt.trim(),
      rooms: this.rooms.trim(),
      kitchen: this.kitchen.trim(),
      toilet: this.toilet.trim(),
      bathroom: this.bathroom.trim(),
      facilities: this.facilities.trim(),
      photoDataUrl: this.photoDataUrl,
      status: this.status,
    });
    this.resetForm();
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  editRow(row: HallDescriptionRecord): void {
    if (!this.auth.isSuperAdmin()) {
      return;
    }
    void this.router.navigate([], { relativeTo: this.route, queryParams: { edit: row.id } });
  }

  deleteRow(row: HallDescriptionRecord): void {
    if (!this.auth.isSuperAdmin()) {
      return;
    }
    this.data.deleteHall(row.id);
  }

  toggleVenueActive(v: HallDescriptionRecord): void {
    if (!this.auth.isSuperAdmin()) {
      return;
    }
    const id = Number(v.id);
    if (!Number.isFinite(id) || id <= 0) {
      return;
    }
    const next = v.status !== 'Active';
    this.data.setVenueActive(id, next);
  }
}
