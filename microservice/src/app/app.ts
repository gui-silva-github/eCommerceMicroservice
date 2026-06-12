import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Router, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatToolbarModule } from "@angular/material/toolbar";
import { RouterModule } from "@angular/router";
import { UsersService } from './services/users.service';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';

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
  searchForm: FormGroup;

  constructor (
    public usersService: UsersService,
    private router: Router,
    private fb: FormBuilder,
  ) {
    this.searchForm = this.fb.group({
      searchStr: ['', []]
    });
  }

  logout() {
    this.usersService.logout();
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
