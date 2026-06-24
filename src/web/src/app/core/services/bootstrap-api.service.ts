import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BootstrapPayload } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class BootstrapApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/bootstrap`;

  load(pageSize: number, abilityPageSize = 50): Observable<BootstrapPayload> {
    const params = new HttpParams()
      .set('pageSize', pageSize)
      .set('abilityPageSize', abilityPageSize);
    return this.http.get<BootstrapPayload>(this.baseUrl, { params });
  }
}
