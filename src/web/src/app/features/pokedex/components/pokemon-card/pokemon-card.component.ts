import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input
} from '@angular/core';
import { Router } from '@angular/router';
import { PokemonSummary } from '../../../../core/models/api.models';
import { AuthService } from '../../../../core/services/auth.service';
import { CollectionStore } from '../../../../core/services/collection.store';
import { formatGenLabel, formatPokemonName, typeColor } from '../../../../core/utils/pokemon.utils';

@Component({
  selector: 'app-pokemon-card',
  standalone: true,
  templateUrl: './pokemon-card.component.html',
  styleUrl: './pokemon-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PokemonCardComponent {
  private readonly auth = inject(AuthService);
  private readonly collectionStore = inject(CollectionStore);
  private readonly router = inject(Router);

  readonly pokemon = input.required<PokemonSummary>();

  readonly formatName = formatPokemonName;
  readonly typeColor = typeColor;

  readonly collectionState = computed(() => {
    // Depend on entries signal so toggles refresh OnPush cards.
    this.collectionStore.entries();
    return this.collectionStore.stateFor(this.pokemon().id);
  });

  readonly types = computed(() => this.pokemon().types ?? []);
  readonly abilities = computed(() => this.pokemon().abilities ?? []);
  readonly genLabel = computed(() => formatGenLabel(this.pokemon().generation));
  readonly isAuthenticated = computed(() => this.auth.isAuthenticated());
  readonly primaryTypeColor = computed(() => typeColor(this.types()[0] ?? 'normal'));

  onFavoriteClick(event: Event): void {
    event.stopPropagation();
    if (!this.isAuthenticated()) {
      void this.router.navigate(['/login']);
      return;
    }
    this.collectionStore.toggleFavorite(this.pokemon());
  }

  onCaughtClick(event: Event): void {
    event.stopPropagation();
    if (!this.isAuthenticated()) {
      void this.router.navigate(['/login']);
      return;
    }
    this.collectionStore.toggleCaught(this.pokemon());
  }
}
