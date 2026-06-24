import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject
} from '@angular/core';
import { Subject, debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs';
import { ActiveFilters, FilterOption, PagedResult, PokemonSummary } from '../../core/models/api.models';
import { BootstrapApiService } from '../../core/services/bootstrap-api.service';
import { CollectionStore } from '../../core/services/collection.store';
import { FilterApiService } from '../../core/services/filter-api.service';
import { PokemonApiService } from '../../core/services/pokemon-api.service';
import { computePageSize } from '../../core/utils/pokemon.utils';
import { FilterToolbarComponent, FilterToolbarValue } from './components/filter-toolbar/filter-toolbar.component';
import { PokemonCardComponent } from './components/pokemon-card/pokemon-card.component';

const DEFAULT_PAGE_SIZE = 12;

@Component({
  selector: 'app-pokedex-page',
  standalone: true,
  imports: [FilterToolbarComponent, PokemonCardComponent],
  templateUrl: './pokedex-page.component.html',
  styleUrl: './pokedex-page.component.scss'
})
export class PokedexPageComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly bootstrapApi = inject(BootstrapApiService);
  private readonly pokemonApi = inject(PokemonApiService);
  private readonly filterApi = inject(FilterApiService);
  private readonly collectionStore = inject(CollectionStore);
  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();
  private readonly abilitySearch$ = new Subject<string>();
  private resizeObserver?: ResizeObserver;
  private initialLoadComplete = false;

  @ViewChild('gridHost') gridHost?: ElementRef<HTMLElement>;

  types: FilterOption[] = [];
  generations: FilterOption[] = [];
  abilities: FilterOption[] = [];
  loadingAbilities = false;

  filterValue: FilterToolbarValue = { search: '', type: '', ability: '', generation: '' };
  activeFilters: ActiveFilters = {};

  results: PagedResult<PokemonSummary> | null = null;
  page = 1;
  pageSize = DEFAULT_PAGE_SIZE;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.loadBootstrap(DEFAULT_PAGE_SIZE);
    this.setupFilterPipeline();
    this.setupAbilitySearch();
  }

  ngAfterViewInit(): void {
    this.setupResizeObserver();
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.destroy$.next();
    this.destroy$.complete();
  }

  onFiltersChange(value: FilterToolbarValue): void {
    this.filterValue = value;
    this.page = 1;
    this.filterChange$.next();
  }

  onAbilitySearch(term: string): void {
    this.abilitySearch$.next(term);
  }

  goToPage(nextPage: number): void {
    if (!this.results || nextPage < 1 || nextPage > this.results.totalPages) return;
    this.page = nextPage;
    this.loadPokemon(false);
  }

  private loadBootstrap(pageSize: number): void {
    this.loading = true;
    this.error = null;

    this.bootstrapApi.load(pageSize).subscribe({
      next: payload => {
        this.types = payload.types;
        this.generations = payload.generations;
        this.abilities = payload.abilities.items;
        this.results = payload.pokemon;
        this.page = payload.pokemon.page;
        this.pageSize = payload.pokemon.pageSize;
        this.loading = false;
        this.initialLoadComplete = true;
        this.activeFilters = this.buildActiveFilters();
      },
      error: () => {
        this.loading = false;
        this.error = 'Failed to load Pokedex. Is the API running?';
      }
    });
  }

  private setupFilterPipeline(): void {
    this.filterChange$.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => {
      this.loadPokemon(false);
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

  private setupResizeObserver(): void {
    if (!this.gridHost?.nativeElement || typeof ResizeObserver === 'undefined') return;

    this.resizeObserver = new ResizeObserver(entries => {
      if (!this.initialLoadComplete) return;

      const { width, height } = entries[0].contentRect;
      const nextSize = computePageSize(width, height);
      if (nextSize !== this.pageSize) {
        this.pageSize = nextSize;
        this.page = 1;
        this.loadPokemon(false);
      }
    });

    this.resizeObserver.observe(this.gridHost.nativeElement);
  }

  private loadPokemon(recalculatePageSize: boolean): void {
    if (recalculatePageSize && this.gridHost?.nativeElement) {
      const { clientWidth, clientHeight } = this.gridHost.nativeElement;
      if (clientWidth > 0 && clientHeight > 0) {
        this.pageSize = computePageSize(clientWidth, clientHeight);
      }
    }

    this.loading = true;
    this.error = null;
    this.activeFilters = this.buildActiveFilters();

    this.pokemonApi
      .search({
        search: this.filterValue.search || undefined,
        type: this.filterValue.type || undefined,
        ability: this.filterValue.ability || undefined,
        generation: this.filterValue.generation || undefined,
        page: this.page,
        pageSize: this.pageSize
      })
      .subscribe({
        next: result => {
          this.results = result;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.error = 'Failed to load Pokémon. Check that the API is running.';
          this.results = null;
        }
      });
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
