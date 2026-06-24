import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FilterOption } from '../../../../core/models/api.models';

export interface FilterToolbarValue {
  search: string;
  type: string;
  ability: string;
  generation: string;
}

@Component({
  selector: 'app-filter-toolbar',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './filter-toolbar.component.html',
  styleUrl: './filter-toolbar.component.scss'
})
export class FilterToolbarComponent {
  @Input() types: FilterOption[] = [];
  @Input() generations: FilterOption[] = [];
  @Input() abilities: FilterOption[] = [];
  @Input() loadingAbilities = false;

  @Input() value: FilterToolbarValue = { search: '', type: '', ability: '', generation: '' };
  @Output() valueChange = new EventEmitter<FilterToolbarValue>();
  @Output() abilitySearch = new EventEmitter<string>();

  abilityQuery = '';
  private lastEmittedSnapshot = '';

  onFieldChange(): void {
    this.emitIfChanged();
  }

  onSearchInput(): void {
    this.emitIfChanged();
  }

  onAbilityQueryInput(): void {
    this.abilitySearch.emit(this.abilityQuery.trim());
  }

  clearFilters(): void {
    this.abilityQuery = '';
    this.lastEmittedSnapshot = '';
    this.valueChange.emit({ search: '', type: '', ability: '', generation: '' });
    this.abilitySearch.emit('');
  }

  get hasActiveFilters(): boolean {
    return !!(this.value.search || this.value.type || this.value.ability || this.value.generation);
  }

  private emitIfChanged(): void {
    const snapshot = JSON.stringify(this.value);
    if (snapshot === this.lastEmittedSnapshot) {
      return;
    }

    this.lastEmittedSnapshot = snapshot;
    this.valueChange.emit({ ...this.value });
  }
}
