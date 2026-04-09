import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/auth.guard';
import { OfficePortalRoleGuard } from './core/office-portal-role.guard';
import { LoginComponent } from './pages/login/login.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { DashboardHomeComponent } from './pages/dashboard/dashboard-home/dashboard-home.component';
import { AddEmployeeComponent } from './pages/master/add-employee/add-employee.component';
import { AddRateChartComponent } from './pages/master/add-rate-chart/add-rate-chart.component';
import { AddCategoryComponent } from './pages/master/add-category/add-category.component';
import { AddAccountDetailsComponent } from './pages/master/add-account-details/add-account-details.component';
import { AddHallDescriptionComponent } from './pages/master/add-hall-description/add-hall-description.component';
import { AddVenueTypeComponent } from './pages/master/add-venue-type/add-venue-type.component';
import { AddTextAdvertiseComponent } from './pages/master/add-text-advertise/add-text-advertise.component';
import { AddImageAdvertiseComponent } from './pages/master/add-image-advertise/add-image-advertise.component';
import { AddImageBannerComponent } from './pages/master/add-image-banner/add-image-banner.component';
import { PendingBookingsComponent } from './pages/admin-actions/pending-bookings/pending-bookings.component';
import { ProvisionallyApprovedBookingsComponent } from './pages/admin-actions/provisionally-approved-bookings/provisionally-approved-bookings.component';
import { ForwardBookingsComponent } from './pages/admin-actions/forward-bookings/forward-bookings.component';
import { ApprovedBookingsComponent } from './pages/admin-actions/approved-bookings/approved-bookings.component';
import { RejectedBookingsComponent } from './pages/admin-actions/rejected-bookings/rejected-bookings.component';
import { CancelledBookingsComponent } from './pages/admin-actions/cancelled-bookings/cancelled-bookings.component';
import { AdminBookingsComponent } from './pages/admin-actions/admin-bookings/admin-bookings.component';
import { ChangePasswordComponent } from './pages/admin-actions/change-password/change-password.component';
import { TotalBookingsComponent } from './pages/admin-actions/total-bookings/total-bookings.component';
import { CancelBookingComponent } from './pages/admin-actions/cancel-booking/cancel-booking.component';

/** RoleID from OfficeUser: 1 = Super Admin, 2 = Verifying Authority, 3 = Approving Authority */
const R_SUPER = 1;
const R_VERIFY = 2;
const R_APPROVE = 3;

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'create-new-account', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [AuthGuard],
    children: [
      { path: '', pathMatch: 'full', component: DashboardHomeComponent },
      {
        path: 'master/venues',
        redirectTo: 'master/venue',
        pathMatch: 'full',
      },
      {
        path: 'master/add-hall-description',
        redirectTo: 'master/venue',
        pathMatch: 'full',
      },
      {
        path: 'master/venue',
        component: AddHallDescriptionComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_VERIFY, R_APPROVE] },
      },
      {
        path: 'master/add-venue-type',
        component: AddVenueTypeComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'master/add-employee',
        component: AddEmployeeComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'master/add-rate-chart',
        component: AddRateChartComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'master/add-category',
        component: AddCategoryComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'master/add-account-details',
        component: AddAccountDetailsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'master/add-text-advertise',
        component: AddTextAdvertiseComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'master/add-image-advertise',
        component: AddImageAdvertiseComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'master/add-image-banner',
        component: AddImageBannerComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'admin/pending-bookings',
        component: PendingBookingsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_VERIFY] },
      },
      {
        path: 'admin/provisionally-approved-bookings',
        component: ProvisionallyApprovedBookingsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_VERIFY] },
      },
      {
        path: 'admin/forward-bookings',
        component: ForwardBookingsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_APPROVE] },
      },
      {
        path: 'admin/approved-bookings',
        component: ApprovedBookingsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_APPROVE] },
      },
      {
        path: 'admin/rejected-bookings',
        component: RejectedBookingsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_VERIFY, R_APPROVE] },
      },
      {
        path: 'admin/cancelled-bookings',
        component: CancelledBookingsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_VERIFY, R_APPROVE] },
      },
      {
        path: 'admin/admin-booking',
        component: AdminBookingsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'admin/cancel-bookings',
        component: CancelBookingComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER] },
      },
      {
        path: 'admin/change-password',
        component: ChangePasswordComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_VERIFY, R_APPROVE] },
      },
      {
        path: 'admin/total-bookings',
        component: TotalBookingsComponent,
        canActivate: [OfficePortalRoleGuard],
        data: { officeAllowedRoleIds: [R_SUPER, R_VERIFY, R_APPROVE] },
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class OfficePortalRoutingModule {}
