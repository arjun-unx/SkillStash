import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { UserProfileDto } from '@core/models/skill.model';
import { AuthService } from '@core/services/auth.service';
import { NotificationService } from '@core/services/notification.service';
import { UserService } from '@core/services/user.service';
import { UI } from '@core/ui/ui-classes';

@Component({
  selector: 'ps-profile',
  templateUrl: './profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class ProfileComponent implements OnInit {
  readonly ui = UI;
  loading = true;
  profile: UserProfileDto | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly users: UserService,
    private readonly auth: AuthService,
    private readonly notify: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const userName = this.route.snapshot.paramMap.get('userName')!;
    this.users.getProfile(userName).subscribe({
      next: profile => {
        this.profile = profile;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  toggleFollow(): void {
    if (!this.profile) return;
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.users.toggleFollow(this.profile.userName).subscribe(res => {
      if (!this.profile) return;
      this.profile.isFollowedByCurrentUser = res.isFollowing;
      this.profile.followersCount = res.followersCount;
      this.notify.info(res.isFollowing ? 'Following' : 'Unfollowed');
      this.cdr.markForCheck();
    });
  }
}
