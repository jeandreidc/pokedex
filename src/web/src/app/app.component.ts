import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { CollectionStore } from './core/services/collection.store';
import { CollectionSidebarComponent } from './features/collection/collection-sidebar/collection-sidebar.component';
import { AppHeaderComponent } from './shared/components/app-header/app-header.component';
import { ToastContainerComponent } from './shared/components/toast/toast-container.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, AppHeaderComponent, CollectionSidebarComponent, ToastContainerComponent],
  templateUrl: './app.component.html',
  styles: [':host { display: block; min-height: 100%; }']
})
export class AppComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly collectionStore = inject(CollectionStore);

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.collectionStore.loadForUser();
    }
  }
}
