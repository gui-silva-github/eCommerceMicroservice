import { Component, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { NavigationEnd, Router, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatToolbarModule } from "@angular/material/toolbar";
import { RouterModule } from "@angular/router";
import { UsersService } from './services/users.service';
import { CartService } from './services/cart.service';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule, RouterOutlet, MatButtonModule, MatIconModule, MatInputModule,
    MatFormFieldModule, MatToolbarModule, RouterModule, RouterLinkActive, ReactiveFormsModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class AppComponent {
  readonly usersService = inject(UsersService);
  readonly cartService = inject(CartService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly navOpen = signal(false);
  readonly isAuthRoute = signal(false);
  readonly currentYear = new Date().getFullYear();

  searchForm: FormGroup = this.fb.group({
    searchStr: ['', []]
  });

  constructor() {
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed(),
    ).subscribe((event) => {
      this.isAuthRoute.set(event.urlAfterRedirects.startsWith('/auth'));
      this.navOpen.set(false);
    });

    this.isAuthRoute.set(this.router.url.startsWith('/auth'));
  }

  toggleNav(): void {
    this.navOpen.update((open) => !open);
  }

  closeNav(): void {
    this.navOpen.set(false);
  }

  logout() {
    this.usersService.logout();
    this.cartService.clear();
    this.closeNav();
    this.router.navigate(['/products', 'showcase']);
  }

  search() {
    const term = (this.searchForm.value.searchStr ?? '').trim();

    if (!term) {
      this.router.navigate(['/products', 'showcase']);
      return;
    }

    this.router.navigate(['/products', 'search', term]);
  }
}
