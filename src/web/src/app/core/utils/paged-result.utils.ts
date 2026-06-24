import { PagedResult } from '../models/api.models';
import { POKEMON_PAGE_SIZE } from '../constants/pokemon-pagination.constants';
import { computeTotalPages } from './pokemon.utils';

type PagedResultLike<T> = PagedResult<T> & Record<string, unknown>;

export function normalizePagedResult<T>(raw: PagedResultLike<T>): PagedResult<T> {
  const totalCount = readNumber(raw, ['totalCount', 'TotalCount']);
  const page = readNumber(raw, ['page', 'Page'], 1);
  const items = (raw.items ?? raw['Items'] ?? []) as T[];

  return {
    items,
    page,
    pageSize: POKEMON_PAGE_SIZE,
    totalCount,
    totalPages: computeTotalPages(totalCount, POKEMON_PAGE_SIZE)
  };
}

function readNumber(source: Record<string, unknown>, keys: string[], fallback = 0): number {
  for (const key of keys) {
    const value = source[key];
    if (typeof value === 'number' && Number.isFinite(value)) {
      return value;
    }
  }

  return fallback;
}
