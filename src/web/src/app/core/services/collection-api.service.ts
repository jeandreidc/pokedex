import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CollectionEntry,
  CollectionStats,
  UpdateCollectionEntryRequest
} from '../models/collection.models';

@Injectable({ providedIn: 'root' })
export class CollectionApiService {
  private readonly baseUrl = `${environment.apiUrl}/collection`;

  constructor(private readonly http: HttpClient) {}

  getCollection(favoritesOnly?: boolean, caughtOnly?: boolean): Observable<CollectionEntry[]> {
    let params = new HttpParams();
    if (favoritesOnly) params = params.set('favoritesOnly', 'true');
    if (caughtOnly) params = params.set('caughtOnly', 'true');
    return this.http.get<CollectionEntry[]>(this.baseUrl, { params });
  }

  getStats(): Observable<CollectionStats> {
    return this.http.get<CollectionStats>(`${this.baseUrl}/stats`);
  }

  updateEntry(pokemonId: number, body: UpdateCollectionEntryRequest): Observable<CollectionEntry> {
    return this.http.put<CollectionEntry>(`${this.baseUrl}/${pokemonId}`, body);
  }

  removeEntry(pokemonId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${pokemonId}`);
  }
}
