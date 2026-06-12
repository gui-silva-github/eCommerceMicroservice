import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-coming-soon',
  imports: [MatIconModule],
  template: `
    <div class="empty-state" role="status">
      <span class="empty-state__icon" aria-hidden="true">
        <mat-icon>{{ icon }}</mat-icon>
      </span>
      <p class="empty-state__title">{{ title }}</p>
      <p class="empty-state__hint">{{ message }}</p>
    </div>
  `,
})
export class ComingSoon implements OnInit {
  private readonly route = inject(ActivatedRoute);

  icon = 'construction';
  title = 'Em breve';
  message = 'Esta funcionalidade será disponibilizada em uma próxima versão.';

  ngOnInit(): void {
    const data = this.route.snapshot.data;
    this.icon = data['icon'] ?? this.icon;
    this.title = data['title'] ?? this.title;
    this.message = data['message'] ?? this.message;
  }
}
