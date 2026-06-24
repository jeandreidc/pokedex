import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, filter, map, of, race, switchMap, take, timer } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BootstrapPayload } from '../models/api.models';
import { normalizePagedResult } from '../utils/paged-result.utils';

const READY_POLL_MS = 400;
const READY_TIMEOUT_MS = 45_000;

@Injectable({ providedIn: 'root' })
export class BootstrapApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/bootstrap`;
  private readonly readyUrl = `${environment.apiUrl}/ready`;

  load(abilityPageSize = 50): Observable<BootstrapPayload> {
    const params = new HttpParams().set('abilityPageSize', abilityPageSize);

    return this.waitForReady().pipe(
      switchMap(() => this.http.get<Record<string, unknown>>(this.baseUrl, { params })),
      map(raw => this.normalizeBootstrap(raw))
    );
  }

  private normalizeBootstrap(raw: Record<string, unknown>): BootstrapPayload {
    return {
      types: (raw['types'] ?? raw['Types'] ?? []) as BootstrapPayload['types'],
      generations: (raw['generations'] ?? raw['Generations'] ?? []) as BootstrapPayload['generations'],
      abilities: normalizePagedResult(
        (raw['abilities'] ?? raw['Abilities'] ?? { items: [] }) as never
      ) as BootstrapPayload['abilities'],
      pokemonTotalCount: readCount(raw, 'pokemonTotalCount', 'PokemonTotalCount')
    };
  }

  private waitForReady(): Observable<void> {
    const poll = timer(0, READY_POLL_MS).pipe(
      switchMap(() =>
        this.http.get(this.readyUrl, { observe: 'response' }).pipe(catchError(() => of(null)))
      ),
      filter((response): response is HttpResponse<Object> => response !== null && response.status === 200),
      take(1),
      map(() => void 0)
    );

    const timeout = timer(READY_TIMEOUT_MS).pipe(map(() => void 0));

    return race(poll, timeout);
  }
}

function readCount(source: Record<string, unknown>, ...keys: string[]): number {
  for (const key of keys) {
    const value = source[key];
    if (typeof value === 'number' && Number.isFinite(value)) {
      return value;
    }
  }

  return 0;
}
