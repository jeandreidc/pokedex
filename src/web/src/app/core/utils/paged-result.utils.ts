import { PagedResult } from '../models/api.models';
import { POKEMON_PAGE_SIZE } from '../constants/pokemon-pagination.constants';
import { computeTotalPages } from './pokemon.utils';

export function normalizePagedResult<T>(raw: Partial<PagedResult<T>> | null | undefined): PagedResult<T> {
  const totalCount = typeof raw?.totalCount === 'number' && Number.isFinite(raw.totalCount) ? raw.totalCount : 0;
  const page = typeof raw?.page === 'number' && Number.isFinite(raw.page) ? raw.page : 1;
  const items = (raw?.items ?? []) as T[];

  return {
    items,
    page,
    pageSize: POKEMON_PAGE_SIZE,
    totalCount,
    totalPages: computeTotalPages(totalCount, POKEMON_PAGE_SIZE)
  };
}
