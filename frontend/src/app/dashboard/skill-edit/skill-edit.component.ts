import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AGENT_SLUGS } from '@core/models/app.constants';
import { CreateSkillRequest, SkillVisibility, UpdateSkillRequest } from '@core/models/skill.model';
import { NotificationService } from '@core/services/notification.service';
import { SkillService } from '@core/services/skill.service';
import { UI } from '@core/ui/ui-classes';

@Component({
  selector: 'ps-skill-edit',
  templateUrl: './skill-edit.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class SkillEditComponent implements OnInit {
  readonly ui = UI;
  readonly agents = AGENT_SLUGS;
  readonly visibilities = [SkillVisibility.Public, SkillVisibility.Unlisted, SkillVisibility.Private];
  id: string | null = null;
  loading = false;
  saving = false;

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(120)]],
    body: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(100_000)]],
    description: ['', Validators.maxLength(280)],
    agentSlug: ['any', Validators.maxLength(32)],
    visibility: [SkillVisibility.Private, Validators.required],
    tags: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly skills: SkillService,
    private readonly notify: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id');
    if (!this.id) return;
    this.loading = true;
    this.skills.byId(this.id).subscribe({
      next: p => {
        this.form.patchValue({
          title: p.title,
          body: p.body,
          description: p.description ?? '',
          agentSlug: p.agentSlug,
          visibility: p.visibility,
          tags: (p.tags ?? []).join(', ')
        });
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  generateBody(): void {
    const title = this.form.controls.title.value.trim();
    if (!title) {
      this.notify.error('Add a title before generating a skill.');
      this.form.controls.title.markAsTouched();
      return;
    }

    const description = this.form.controls.description.value.trim();
    const agent = this.form.controls.agentSlug.value.trim() || 'any';
    const agentLine = agent === 'any' ? '' : `\nTarget agent: ${agent}`;

    const body = `# ${title}
${description ? `\n${description}\n` : ''}${agentLine}

## When to use
Describe when this skill should be invoked.

## Instructions
- Be specific and actionable.
- Follow project conventions.

## Output
Describe the expected output format.`;

    this.form.patchValue({ body });
    this.form.controls.body.markAsTouched();
    this.cdr.markForCheck();
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const tags = (v.tags ?? '')
      .split(',')
      .map(t => t.trim().toLowerCase())
      .filter(Boolean);

    this.saving = true;

    if (this.id) {
      const payload: UpdateSkillRequest = {
        id: this.id,
        title: v.title,
        body: v.body,
        description: v.description || null,
        agentSlug: v.agentSlug,
        visibility: v.visibility,
        tags
      };
      this.skills.update(this.id, payload).subscribe({
        next: () => {
          this.saving = false;
          this.notify.success('Skill updated');
          this.router.navigate(['/skills', this.id]);
        },
        error: () => {
          this.saving = false;
          this.cdr.markForCheck();
        }
      });
      return;
    }

    const payload: CreateSkillRequest = {
      title: v.title,
      body: v.body,
      description: v.description || null,
      agentSlug: v.agentSlug,
      visibility: v.visibility,
      tags
    };
    this.skills.create(payload).subscribe({
      next: created => {
        this.notify.success('Skill created');
        this.router.navigate(['/skills', created.id]);
      },
      error: () => {
        this.saving = false;
        this.cdr.markForCheck();
      }
    });
  }
}
