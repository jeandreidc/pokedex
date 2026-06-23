import { Component, Input, inject } from '@angular/core';
import { Router } from '@angular/router';
import { PokemonSummary } from '../../../../core/models/api.models';
import { CollectionEntryState } from '../../../../core/models/collection.models';
import { AuthService } from '../../../../core/services/auth.service';
import { CollectionStore } from '../../../../core/services/collection.store';
import { formatGenLabel, formatPokemonName, typeColor } from '../../../../core/utils/pokemon.utils';

@Component({
  selector: 'app-pokemon-card',
  standalone: true,
  templateUrl: './pokemon-card.component.html',
  styleUrl: './pokemon-card.component.scss'
})
export class PokemonCardComponent {
  private readonly auth = inject(AuthService);
  private readonly collectionStore = inject(CollectionStore);
  private readonly router = inject(Router);

  @Input({ required: true }) pokemon!: PokemonSummary;
  @Input() collectionState: CollectionEntryState = { isCaught: false, isFavorite: false };

  readonly formatName = formatPokemonName;
  readonly typeColor = typeColor;

  get types(): string[] {
    return this.pokemon.types ?? [];
  }

  get abilities(): string[] {
    return this.pokemon.abilities ?? [];
  }

  get genLabel(): string | null {
    return formatGenLabel(this.pokemon.generation);
  }

  get isAuthenticated(): boolean {
    return this.auth.isAuthenticated();
  }

  primaryTypeColor(): string {
    return typeColor(this.types[0] ?? 'normal');
  }

  onFavoriteClick(event: Event): void {
    event.stopPropagation();
    if (!this.isAuthenticated) {
      void this.router.navigate(['/login']);
      return;
    }
    this.collectionStore.toggleFavorite(this.pokemon);
  }

  onCaughtClick(event: Event): void {
    event.stopPropagation();
    if (!this.isAuthenticated) {
      void this.router.navigate(['/login']);
      return;
    }
    this.collectionStore.toggleCaught(this.pokemon);
  }
}
