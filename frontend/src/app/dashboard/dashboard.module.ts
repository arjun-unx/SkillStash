import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CoreModule } from '@core/core.module';
import { AuthGuard } from '@core/guards/auth.guard';
import { MainLayoutComponent } from '@core/layout/main-layout/main-layout.component';

import { FeedComponent } from './feed/feed.component';
import { LibraryComponent } from './library/library.component';
import { MySkillsComponent } from './my-skills/my-skills.component';
import { ProfileComponent } from './profile/profile.component';
import { SkillDetailComponent } from './skill-detail/skill-detail.component';
import { SkillEditComponent } from './skill-edit/skill-edit.component';
import { TrendingBrowseComponent } from './trending/trending-browse.component';
import { TrendingHubComponent } from './trending/trending-hub.component';
import { TrendingSkillCardComponent } from './trending/trending-skill-card/trending-skill-card.component';

const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'feed' },
      { path: 'feed', component: FeedComponent },
      { path: 'trending', component: TrendingHubComponent },
      { path: 'trending/:slug', component: TrendingBrowseComponent },
      { path: 'library', component: LibraryComponent, canActivate: [AuthGuard] },
      { path: 'skills/mine', component: MySkillsComponent, canActivate: [AuthGuard] },
      { path: 'skills/new', component: SkillEditComponent, canActivate: [AuthGuard] },
      { path: 'skills/:id/edit', component: SkillEditComponent, canActivate: [AuthGuard] },
      { path: 'skills/:id', component: SkillDetailComponent },
      { path: 'u/:userName', component: ProfileComponent },
      { path: '**', redirectTo: 'feed' }
    ]
  }
];

@NgModule({
  declarations: [
    FeedComponent,
    LibraryComponent,
    MySkillsComponent,
    SkillDetailComponent,
    SkillEditComponent,
    ProfileComponent,
    TrendingHubComponent,
    TrendingBrowseComponent,
    TrendingSkillCardComponent
  ],
  imports: [CoreModule, RouterModule.forChild(routes)]
})
export class DashboardModule {}
