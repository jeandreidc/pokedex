import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult, PokemonSearchParams, PokemonSummary } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class PokemonApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/pokemon`;

  search(params: PokemonSearchParams): Observable<PagedResult<PokemonSummary>> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.type) httpParams = httpParams.set('type', params.type);
    if (params.ability) httpParams = httpParams.set('ability', params.ability);
    if (params.generation) httpParams = httpParams.set('generation', params.generation);

    return this.http.get<PagedResult<PokemonSummary>>(this.baseUrl, { params: httpParams });
  }
}
