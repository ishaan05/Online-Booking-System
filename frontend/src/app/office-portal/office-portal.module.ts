import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OfficePortalRoutingModule } from './office-portal-routing.module';
import { LoginComponent } from './pages/login/login.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { DashboardHomeComponent } from './pages/dashboard/dashboard-home/dashboard-home.component';
import { SidenavComponent } from './layout/sidenav/sidenav.component';
import { CardsComponent } from './layout/cards/cards.component';
import { NavbarComponent } from './layout/navbar/navbar.component';
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
import { OverlayModule } from '@angular/cdk/overlay';
import { PortalModule } from '@angular/cdk/portal';
import { AdminDateFieldComponent } from './shared/admin-date-field.component';
import { MotionModule } from '../layout/motion.module';
import { LuDeferredSkeletonDirective } from '../core/loading-ux/lu-deferred-skeleton.directive';

@NgModule({
  declarations: [
    LoginComponent,
    ForgotPasswordComponent,
    DashboardComponent,
    DashboardHomeComponent,
    SidenavComponent,
    CardsComponent,
    NavbarComponent,
    AddEmployeeComponent,
    AddRateChartComponent,
    AddCategoryComponent,
    AddAccountDetailsComponent,
    AddHallDescriptionComponent,
    AddVenueTypeComponent,
    AddTextAdvertiseComponent,
    AddImageAdvertiseComponent,
    AddImageBannerComponent,
    PendingBookingsComponent,
    ProvisionallyApprovedBookingsComponent,
    ForwardBookingsComponent,
    ApprovedBookingsComponent,
    RejectedBookingsComponent,
    CancelledBookingsComponent,
    AdminBookingsComponent,
    ChangePasswordComponent,
    TotalBookingsComponent,
    CancelBookingComponent,
    AdminDateFieldComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    OfficePortalRoutingModule,
    MotionModule,
    LuDeferredSkeletonDirective,
    OverlayModule,
    PortalModule,
  ],
})
export class OfficePortalModule {}
