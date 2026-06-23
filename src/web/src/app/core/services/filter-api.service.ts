import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, of, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FilterOption, PagedResult } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class FilterApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/filters`;
  private readonly abilityPageSize = 100;

  getTypes(): Observable<FilterOption[]> {
    return this.http.get<FilterOption[]>(`${this.baseUrl}/types`);
  }

  getGenerations(): Observable<FilterOption[]> {
    return this.http.get<FilterOption[]>(`${this.baseUrl}/generations`);
  }

  getAbilities(search?: string, page = 1, pageSize = 50): Observable<PagedResult<FilterOption>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<FilterOption>>(`${this.baseUrl}/abilities`, { params });
  }

  /** Loads every ability page from the backend cache (full ~367 list). */
  getAllAbilities(search?: string): Observable<FilterOption[]> {
    return this.getAbilities(search, 1, this.abilityPageSize).pipe(
      switchMap(first => this.collectAllPages(first, search))
    );
  }

  private collectAllPages(first: PagedResult<FilterOption>, search?: string): Observable<FilterOption[]> {
    if (first.totalPages <= 1) {
      return of(first.items);
    }

    const remaining = Array.from({ length: first.totalPages - 1 }, (_, index) =>
      this.getAbilities(search, index + 2, this.abilityPageSize)
    );

    return forkJoin(remaining).pipe(
      map(pages => [...first.items, ...pages.flatMap(page => page.items)])
    );
  }
}
