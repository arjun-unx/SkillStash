import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { ModuleWithProviders, NgModule, Optional, SkipSelf } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { AuthLayoutComponent } from './components/auth-layout/auth-layout.component';
import { EmptyStateComponent } from './components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from './components/loading-spinner/loading-spinner.component';
import { SkillCardComponent } from './components/skill-card/skill-card.component';
import { ToastContainerComponent } from './components/toast/toast.component';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { ErrorInterceptor } from './interceptors/error.interceptor';
import { HeaderComponent } from './layout/header/header.component';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { SidebarComponent } from './layout/sidebar/sidebar.component';
import { TimeAgoPipe } from './pipes/time-ago.pipe';

const SHARED_DECLARATIONS = [
  AuthLayoutComponent,
  EmptyStateComponent,
  LoadingSpinnerComponent,
  SkillCardComponent,
  ToastContainerComponent,
  TimeAgoPipe
];

const LAYOUT_DECLARATIONS = [MainLayoutComponent, HeaderComponent, SidebarComponent];

@NgModule({
  declarations: [...SHARED_DECLARATIONS, ...LAYOUT_DECLARATIONS],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  exports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ...SHARED_DECLARATIONS,
    MainLayoutComponent
  ]
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() parent?: CoreModule) {
    if (parent) {
      // eslint-disable-next-line no-console
      console.debug('CoreModule re-imported by a feature module — that is fine.');
    }
  }

  static forRoot(): ModuleWithProviders<CoreModule> {
    return {
      ngModule: CoreModule,
      providers: [
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true }
      ]
    };
  }
}
