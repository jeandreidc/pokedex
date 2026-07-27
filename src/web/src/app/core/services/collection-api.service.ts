import { HttpClient } from '@angular/common/http';
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

  getCollection(): Observable<CollectionEntry[]> {
    return this.http.get<CollectionEntry[]>(this.baseUrl);
  }

  getStats(): Observable<CollectionStats> {
    return this.http.get<CollectionStats>(`${this.baseUrl}/stats`);
  }

  updateEntry(pokemonId: number, body: UpdateCollectionEntryRequest): Observable<CollectionEntry> {
    return this.http.put<CollectionEntry>(`${this.baseUrl}/${pokemonId}`, body);
  }
}
