import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Observable } from 'rxjs';
import { CurrentUser } from '@core/models/auth.model';
import { AuthService } from '@core/services/auth.service';

interface NavItem {
  label: string;
  icon: string;
  link: string;
  requiresAuth?: boolean;
  /** Use exact matching for routerLinkActive (e.g. Discover vs nested routes). */
  exact?: boolean;
}

@Component({
  selector: 'ps-sidebar',
  templateUrl: './sidebar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class SidebarComponent {
  readonly user$: Observable<CurrentUser | null>;
  readonly items: NavItem[] = [
    { label: 'Discover', icon: 'travel_explore', link: '/feed', exact: true },
    { label: 'Trending', icon: 'trending_up', link: '/trending' },
    { label: 'Library', icon: 'collections_bookmark', link: '/library', requiresAuth: true },
    { label: 'My skills', icon: 'inventory_2', link: '/skills/mine', requiresAuth: true },
    { label: 'New skill', icon: 'add_circle', link: '/skills/new', requiresAuth: true }
  ];

  constructor(private readonly auth: AuthService) {
    this.user$ = this.auth.currentUser$;
  }

  isVisible(item: NavItem, user: CurrentUser | null): boolean {
    return !item.requiresAuth || !!user;
  }
}
