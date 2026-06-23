import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CollectionEntry } from '../../../core/models/collection.models';
import { formatPokemonName } from '../../../core/utils/pokemon.utils';

@Component({
  selector: 'app-collection-avatar',
  standalone: true,
  templateUrl: './collection-avatar.component.html',
  styleUrl: './collection-avatar.component.scss'
})
export class CollectionAvatarComponent {
  @Input({ required: true }) entry!: CollectionEntry;
  @Output() remove = new EventEmitter<CollectionEntry>();

  readonly formatName = formatPokemonName;
  readonly confirmOpen = signal(false);

  onDeleteClick(event: Event): void {
    event.stopPropagation();
    this.confirmOpen.set(true);
  }

  confirmRemove(event: Event): void {
    event.stopPropagation();
    this.remove.emit(this.entry);
    this.confirmOpen.set(false);
  }

  cancelRemove(event: Event): void {
    event.stopPropagation();
    this.confirmOpen.set(false);
  }

  onMouseLeave(): void {
    if (this.confirmOpen()) return;
  }
}
