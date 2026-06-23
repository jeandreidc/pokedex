import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject
} from '@angular/core';
import { Subject, debounceTime, distinctUntilChanged, forkJoin, switchMap, takeUntil } from 'rxjs';
import { ActiveFilters, FilterOption, PagedResult, PokemonSummary } from '../../core/models/api.models';
import { CollectionStore } from '../../core/services/collection.store';
import { FilterApiService } from '../../core/services/filter-api.service';
import { PokemonApiService } from '../../core/services/pokemon-api.service';
import { computePageSize } from '../../core/utils/pokemon.utils';
import { FilterToolbarComponent, FilterToolbarValue } from './components/filter-toolbar/filter-toolbar.component';
import { PokemonCardComponent } from './components/pokemon-card/pokemon-card.component';

@Component({
  selector: 'app-pokedex-page',
  standalone: true,
  imports: [FilterToolbarComponent, PokemonCardComponent],
  templateUrl: './pokedex-page.component.html',
  styleUrl: './pokedex-page.component.scss'
})
export class PokedexPageComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly pokemonApi = inject(PokemonApiService);
  private readonly filterApi = inject(FilterApiService);
  private readonly collectionStore = inject(CollectionStore);
  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();
  private readonly abilitySearch$ = new Subject<string>();
  private resizeObserver?: ResizeObserver;

  @ViewChild('gridHost') gridHost?: ElementRef<HTMLElement>;

  types: FilterOption[] = [];
  generations: FilterOption[] = [];
  abilities: FilterOption[] = [];
  loadingAbilities = false;

  filterValue: FilterToolbarValue = { search: '', type: '', ability: '', generation: '' };
  activeFilters: ActiveFilters = {};

  results: PagedResult<PokemonSummary> | null = null;
  page = 1;
  pageSize = 12;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.loadFilterMetadata();
    this.setupFilterPipeline();
    this.setupAbilitySearch();
  }

  ngAfterViewInit(): void {
    this.setupResizeObserver();
    queueMicrotask(() => this.loadPokemon(true));
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

  private loadFilterMetadata(): void {
    forkJoin({
      types: this.filterApi.getTypes(),
      generations: this.filterApi.getGenerations(),
      abilities: this.filterApi.getAllAbilities()
    }).subscribe({
      next: ({ types, generations, abilities }) => {
        this.types = types;
        this.generations = generations;
        this.abilities = abilities;
      },
      error: () => {
        this.error = 'Failed to load filter options. Is the API running?';
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
          return this.filterApi.getAllAbilities(term || undefined);
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: abilities => {
          this.abilities = abilities;
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
          this.error = 'Failed to load Pokémon. Check that the API is running on port 5164.';
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
