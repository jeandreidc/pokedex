import { Component, Input } from '@angular/core';
import { PokemonSummary } from '../../../../core/models/api.models';
import { formatGenLabel, formatPokemonName, typeColor } from '../../../../core/utils/pokemon.utils';

@Component({
  selector: 'app-pokemon-card',
  standalone: true,
  templateUrl: './pokemon-card.component.html',
  styleUrl: './pokemon-card.component.scss'
})
export class PokemonCardComponent {
  @Input({ required: true }) pokemon!: PokemonSummary;

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

  primaryTypeColor(): string {
    return typeColor(this.types[0] ?? 'normal');
  }
}
