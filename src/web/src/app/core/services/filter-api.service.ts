import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FilterOption, PagedResult } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class FilterApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/filters`;
  private readonly abilityPageSize = 50;

  getTypes(): Observable<FilterOption[]> {
    return this.http.get<FilterOption[]>(`${this.baseUrl}/types`);
  }

  getGenerations(): Observable<FilterOption[]> {
    return this.http.get<FilterOption[]>(`${this.baseUrl}/generations`);
  }

  getAbilities(search?: string, page = 1, pageSize = this.abilityPageSize): Observable<PagedResult<FilterOption>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<FilterOption>>(`${this.baseUrl}/abilities`, { params });
  }
}
