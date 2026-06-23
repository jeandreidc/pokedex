import { Injectable, computed, signal } from '@angular/core';
import { PokemonSummary } from '../models/api.models';
import {
  CollectionEntry,
  CollectionEntryState,
  CollectionStats,
  CollectionTab
} from '../models/collection.models';
import { formatPokemonName } from '../utils/pokemon.utils';
import { AuthService } from './auth.service';
import { CollectionApiService } from './collection-api.service';
import { ToastService } from './toast.service';

@Injectable({ providedIn: 'root' })
export class CollectionStore {
  private readonly _entries = signal<Map<number, CollectionEntry>>(new Map());
  private readonly _stats = signal<CollectionStats | null>(null);
  private readonly _sidebarOpen = signal(false);
  private readonly _activeTab = signal<CollectionTab>('favorites');

  readonly sidebarOpen = this._sidebarOpen.asReadonly();
  readonly activeTab = this._activeTab.asReadonly();
  readonly stats = this._stats.asReadonly();

  readonly favorites = computed(() =>
    [...this._entries().values()].filter(e => e.isFavorite).sort((a, b) => a.pokemonId - b.pokemonId)
  );

  readonly caught = computed(() =>
    [...this._entries().values()].filter(e => e.isCaught).sort((a, b) => a.pokemonId - b.pokemonId)
  );

  readonly cartCount = computed(() => {
    const entries = this._entries();
    let count = 0;
    for (const entry of entries.values()) {
      if (entry.isFavorite || entry.isCaught) count++;
    }
    return count;
  });

  constructor(
    private readonly auth: AuthService,
    private readonly collectionApi: CollectionApiService,
    private readonly toast: ToastService
  ) {}

  loadForUser(): void {
    if (!this.auth.isAuthenticated()) {
      this.clear();
      return;
    }

    this.collectionApi.getCollection().subscribe({
      next: entries => {
        const map = new Map<number, CollectionEntry>();
        for (const entry of entries) {
          map.set(entry.pokemonId, entry);
        }
        this._entries.set(map);
      },
      error: () => this.toast.show('Failed to load collection', 'error')
    });

    this.collectionApi.getStats().subscribe({
      next: stats => this._stats.set(stats),
      error: () => {}
    });
  }

  clear(): void {
    this._entries.set(new Map());
    this._stats.set(null);
    this._sidebarOpen.set(false);
  }

  getState(pokemonId: number): CollectionEntryState {
    const entry = this._entries().get(pokemonId);
    return {
      isCaught: entry?.isCaught ?? false,
      isFavorite: entry?.isFavorite ?? false
    };
  }

  openSidebar(tab: CollectionTab): void {
    this._activeTab.set(tab);
    this._sidebarOpen.set(true);
  }

  closeSidebar(): void {
    this._sidebarOpen.set(false);
  }

  toggleSidebar(): void {
    this._sidebarOpen.update(open => !open);
  }

  setActiveTab(tab: CollectionTab): void {
    this._activeTab.set(tab);
  }

  toggleFavorite(pokemon: PokemonSummary): void {
    if (!this.auth.isAuthenticated()) return;

    const current = this.getState(pokemon.id);
    const next = !current.isFavorite;
    const name = formatPokemonName(pokemon.name);

    this.applyLocalUpdate(pokemon, { isFavorite: next, isCaught: current.isCaught });
    this.openSidebar('favorites');
    this.toast.show(next ? `${name} added to favorites` : `${name} removed from favorites`);

    this.collectionApi.updateEntry(pokemon.id, { isFavorite: next }).subscribe({
      next: entry => this.applyServerEntry(entry),
      error: () => {
        this.applyLocalUpdate(pokemon, current);
        this.toast.show('Failed to save favorite — try again', 'error');
      }
    });
  }

  toggleCaught(pokemon: PokemonSummary): void {
    if (!this.auth.isAuthenticated()) return;

    const current = this.getState(pokemon.id);
    const next = !current.isCaught;
    const name = formatPokemonName(pokemon.name);

    this.applyLocalUpdate(pokemon, { isFavorite: current.isFavorite, isCaught: next });
    this.openSidebar('caught');
    this.toast.show(next ? `${name} marked as caught` : `${name} unmarked as caught`);

    this.collectionApi.updateEntry(pokemon.id, { isCaught: next }).subscribe({
      next: entry => {
        this.applyServerEntry(entry);
        this.refreshStats();
      },
      error: () => {
        this.applyLocalUpdate(pokemon, current);
        this.toast.show('Failed to save caught status — try again', 'error');
      }
    });
  }

  removeFromTab(entry: CollectionEntry, tab: CollectionTab): void {
    const name = formatPokemonName(entry.name);
    const current = this.getState(entry.pokemonId);

    if (tab === 'favorites') {
      this.applyLocalUpdateFromEntry(entry, { isFavorite: false, isCaught: current.isCaught });
      this.collectionApi.updateEntry(entry.pokemonId, { isFavorite: false }).subscribe({
        next: serverEntry => this.applyServerEntry(serverEntry),
        error: () => {
          this.applyLocalUpdateFromEntry(entry, current);
          this.toast.show('Failed to remove favorite', 'error');
        }
      });
      this.toast.show(`${name} removed from favorites`);
      return;
    }

    this.applyLocalUpdateFromEntry(entry, { isFavorite: current.isFavorite, isCaught: false });
    this.collectionApi.updateEntry(entry.pokemonId, { isCaught: false }).subscribe({
      next: serverEntry => {
        this.applyServerEntry(serverEntry);
        this.refreshStats();
      },
      error: () => {
        this.applyLocalUpdateFromEntry(entry, current);
        this.toast.show('Failed to remove caught status', 'error');
      }
    });
    this.toast.show(`${name} removed from caught`);
  }

  private applyLocalUpdate(pokemon: PokemonSummary, state: CollectionEntryState): void {
    this._entries.update(map => {
      const next = new Map(map);
      if (!state.isFavorite && !state.isCaught) {
        next.delete(pokemon.id);
      } else {
        next.set(pokemon.id, {
          pokemonId: pokemon.id,
          name: pokemon.name,
          spriteUrl: pokemon.spriteUrl,
          isCaught: state.isCaught,
          isFavorite: state.isFavorite
        });
      }
      return next;
    });
  }

  private applyLocalUpdateFromEntry(entry: CollectionEntry, state: CollectionEntryState): void {
    this.applyLocalUpdate(
      {
        id: entry.pokemonId,
        name: entry.name,
        spriteUrl: entry.spriteUrl,
        types: [],
        abilities: []
      },
      state
    );
  }

  private applyServerEntry(entry: CollectionEntry): void {
    this._entries.update(map => {
      const next = new Map(map);
      if (!entry.isFavorite && !entry.isCaught) {
        next.delete(entry.pokemonId);
      } else {
        next.set(entry.pokemonId, entry);
      }
      return next;
    });
  }

  private refreshStats(): void {
    this.collectionApi.getStats().subscribe({
      next: stats => this._stats.set(stats)
    });
  }
}
