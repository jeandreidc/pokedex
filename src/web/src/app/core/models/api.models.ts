export interface PokemonSummary {
  id: number;
  name: string;
  spriteUrl: string;
  types: string[];
  abilities?: string[];
  generation?: string | null;
}

export interface FilterOption {
  id: number;
  name: string;
  displayName: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PokemonSearchParams {
  search?: string;
  type?: string;
  ability?: string;
  generation?: string;
  page: number;
  pageSize: number;
}

export interface BootstrapPayload {
  types: FilterOption[];
  generations: FilterOption[];
  abilities: PagedResult<FilterOption>;
  pokemonTotalCount: number;
}
