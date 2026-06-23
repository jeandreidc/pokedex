import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CollectionStore } from '../../../core/services/collection.store';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.scss'
})
export class AppHeaderComponent {
  readonly isAuthenticated;
  readonly cartCount;

  constructor(
    private readonly auth: AuthService,
    private readonly collectionStore: CollectionStore,
    private readonly router: Router
  ) {
    this.isAuthenticated = this.auth.isAuthenticated;
    this.cartCount = this.collectionStore.cartCount;
  }

  username(): string | null {
    return this.auth.username;
  }

  logout(): void {
    this.collectionStore.clear();
    this.auth.logout();
  }

  openCart(): void {
    if (!this.auth.isAuthenticated()) {
      void this.router.navigate(['/login']);
      return;
    }
    this.collectionStore.toggleSidebar();
  }
}
