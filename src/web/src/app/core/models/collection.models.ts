export interface AuthResponse {
  token: string;
  username: string;
  expiresAtUtc: string;
}

export interface CollectionEntry {
  pokemonId: number;
  name: string;
  spriteUrl: string;
  isCaught: boolean;
  isFavorite: boolean;
}

export interface CollectionStats {
  totalCaught: number;
  totalFavorites: number;
  totalPokemon: number;
  overallCaughtPercentage: number;
  byGeneration: GenerationStat[];
}

export interface GenerationStat {
  generation: string;
  displayName: string;
  caughtCount: number;
  totalInGeneration: number;
  caughtPercentage: number;
}

export interface UpdateCollectionEntryRequest {
  isCaught?: boolean;
  isFavorite?: boolean;
}

export type CollectionTab = 'favorites' | 'caught' | 'status';

export interface CollectionEntryState {
  isCaught: boolean;
  isFavorite: boolean;
}
