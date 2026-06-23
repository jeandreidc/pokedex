import { Component, inject } from '@angular/core';
import { CollectionEntry, CollectionTab } from '../../../core/models/collection.models';
import { CollectionStore } from '../../../core/services/collection.store';
import { CollectionAvatarComponent } from '../collection-avatar/collection-avatar.component';
import { GenerationRingComponent } from '../generation-ring/generation-ring.component';

@Component({
  selector: 'app-collection-sidebar',
  standalone: true,
  imports: [CollectionAvatarComponent, GenerationRingComponent],
  templateUrl: './collection-sidebar.component.html',
  styleUrl: './collection-sidebar.component.scss'
})
export class CollectionSidebarComponent {
  readonly store = inject(CollectionStore);

  close(): void {
    this.store.closeSidebar();
  }

  setTab(tab: CollectionTab): void {
    this.store.setActiveTab(tab);
  }

  remove(entry: CollectionEntry): void {
    const tab = this.store.activeTab();
    if (tab === 'status') return;
    this.store.removeFromTab(entry, tab);
  }

  activeEntries(): CollectionEntry[] {
    const tab = this.store.activeTab();
    if (tab === 'favorites') return this.store.favorites();
    if (tab === 'caught') return this.store.caught();
    return [];
  }

  isListTab(): boolean {
    const tab = this.store.activeTab();
    return tab === 'favorites' || tab === 'caught';
  }

  generationStats() {
    return this.store.stats()?.byGeneration ?? [];
  }
}
