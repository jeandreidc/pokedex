import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  switchMap,
  takeUntil
} from 'rxjs';
import { POKEMON_PAGE_SIZE } from '../../core/constants/pokemon-pagination.constants';
import { ActiveFilters, FilterOption, PagedResult, PokemonSummary } from '../../core/models/api.models';
import { BootstrapApiService } from '../../core/services/bootstrap-api.service';
import { CollectionStore } from '../../core/services/collection.store';
import { FilterApiService } from '../../core/services/filter-api.service';
import { PokemonApiService } from '../../core/services/pokemon-api.service';
import { computeTotalPages } from '../../core/utils/pokemon.utils';
import { FilterToolbarComponent, FilterToolbarValue } from './components/filter-toolbar/filter-toolbar.component';
import { PokemonCardComponent } from './components/pokemon-card/pokemon-card.component';

type PageLoadRequest = { page: number; refreshCatalogTotal: boolean };

const EMPTY_FILTERS: FilterToolbarValue = { search: '', type: '', ability: '', generation: '' };

@Component({
  selector: 'app-pokedex-page',
  standalone: true,
  imports: [FilterToolbarComponent, PokemonCardComponent],
  templateUrl: './pokedex-page.component.html',
  styleUrl: './pokedex-page.component.scss'
})
export class PokedexPageComponent implements OnInit, OnDestroy {
  private readonly bootstrapApi = inject(BootstrapApiService);
  private readonly pokemonApi = inject(PokemonApiService);
  private readonly filterApi = inject(FilterApiService);
  private readonly collectionStore = inject(CollectionStore);
  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();
  private readonly abilitySearch$ = new Subject<string>();
  private readonly pageLoad$ = new Subject<PageLoadRequest>();
  private filterSnapshot = JSON.stringify(EMPTY_FILTERS);
  private loadGeneration = 0;

  types: FilterOption[] = [];
  generations: FilterOption[] = [];
  abilities: FilterOption[] = [];
  loadingAbilities = false;

  filterValue: FilterToolbarValue = { ...EMPTY_FILTERS };
  activeFilters: ActiveFilters = {};

  results: PagedResult<PokemonSummary> | null = null;
  catalogTotalCount = 0;
  page = 1;
  readonly pageSize = POKEMON_PAGE_SIZE;
  loading = false;
  error: string | null = null;

  get totalPages(): number {
    return computeTotalPages(this.catalogTotalCount, this.pageSize);
  }

  get visibleCount(): number {
    return this.results?.items.length ?? 0;
  }

  ngOnInit(): void {
    this.setupPageLoader();
    this.setupFilterPipeline();
    this.setupAbilitySearch();
    this.loadInitial();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onFiltersChange(value: FilterToolbarValue): void {
    const nextSnapshot = JSON.stringify(value);
    if (nextSnapshot === this.filterSnapshot) {
      return;
    }

    this.filterSnapshot = nextSnapshot;
    this.filterValue = value;
    this.page = 1;
    this.filterChange$.next();
  }

  onAbilitySearch(term: string): void {
    this.abilitySearch$.next(term);
  }

  goToPage(nextPage: number): void {
    if (!this.results || nextPage < 1 || nextPage > this.totalPages || nextPage === this.page) {
      return;
    }

    this.page = nextPage;
    this.pageLoad$.next({ page: nextPage, refreshCatalogTotal: false });
  }

  private loadInitial(): void {
    const generation = ++this.loadGeneration;
    this.loading = true;
    this.error = null;

    this.bootstrapApi
      .load()
      .pipe(
        switchMap(metadata =>
          this.pokemonApi.search(this.currentSearchParams(1)).pipe(
            map(page => ({ metadata, page }))
          )
        ),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: ({ metadata, page }) => {
          if (generation !== this.loadGeneration) {
            return;
          }

          this.types = metadata.types;
          this.generations = metadata.generations;
          this.abilities = metadata.abilities.items;
          this.catalogTotalCount = metadata.pokemonTotalCount;
          this.filterSnapshot = JSON.stringify(this.filterValue);
          this.applyPageResult(page, 1);
          this.activeFilters = this.buildActiveFilters();
          this.loading = false;
        },
        error: () => {
          if (generation !== this.loadGeneration) {
            return;
          }

          this.loading = false;
          this.error = 'Failed to load Pokedex. Is the API running?';
        }
      });
  }

  private setupPageLoader(): void {
    this.pageLoad$
      .pipe(
        switchMap(request => {
          const generation = ++this.loadGeneration;
          this.loading = true;
          this.error = null;

          return this.pokemonApi.search(this.currentSearchParams(request.page)).pipe(
            map(page => ({ generation, request, page })),
            catchError(() => of({ generation, request, page: null }))
          );
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(({ generation, request, page }) => {
        if (generation !== this.loadGeneration) {
          return;
        }

        if (!page) {
          this.loading = false;
          this.error = 'Failed to load Pokémon. Check that the API is running.';
          return;
        }

        if (request.refreshCatalogTotal) {
          this.catalogTotalCount = page.totalCount;
        }

        this.applyPageResult(page, request.page);
        this.loading = false;
      });
  }

  private setupFilterPipeline(): void {
    this.filterChange$
      .pipe(
        debounceTime(300),
        map(() => JSON.stringify(this.filterValue)),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.page = 1;
        this.pageLoad$.next({ page: 1, refreshCatalogTotal: true });
      });
  }

  private setupAbilitySearch(): void {
    this.abilitySearch$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap(term => {
          this.loadingAbilities = true;
          return this.filterApi.getAbilities(term || undefined, 1);
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: page => {
          this.abilities = page.items;
          this.loadingAbilities = false;
        },
        error: () => {
          this.loadingAbilities = false;
        }
      });
  }

  private currentSearchParams(page: number) {
    return {
      search: this.filterValue.search || undefined,
      type: this.filterValue.type || undefined,
      ability: this.filterValue.ability || undefined,
      generation: this.filterValue.generation || undefined,
      page,
      pageSize: this.pageSize
    };
  }

  private applyPageResult(result: PagedResult<PokemonSummary>, page: number): void {
    this.results = result;
    this.page = page;
  }

  getCollectionState(pokemonId: number) {
    return this.collectionStore.getState(pokemonId);
  }

  private buildActiveFilters(): ActiveFilters {
    const type = this.types.find(t => t.name === this.filterValue.type);
    const ability = this.abilities.find(a => a.name === this.filterValue.ability);
    const generation = this.generations.find(g => g.name === this.filterValue.generation);

    return {
      search: this.filterValue.search || undefined,
      type: this.filterValue.type || undefined,
      typeLabel: type?.displayName,
      ability: this.filterValue.ability || undefined,
      abilityLabel: ability?.displayName,
      generation: this.filterValue.generation || undefined,
      generationLabel: generation?.displayName
    };
  }
}
