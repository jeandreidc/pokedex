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

  @Input()
  set value(next: FilterToolbarValue) {
    this.draft = { ...next };
    this.lastEmittedSnapshot = JSON.stringify(this.draft);
  }

  @Output() valueChange = new EventEmitter<FilterToolbarValue>();
  @Output() abilitySearch = new EventEmitter<string>();

  draft: FilterToolbarValue = { search: '', type: '', ability: '', generation: '' };
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
    this.draft = { search: '', type: '', ability: '', generation: '' };
    this.lastEmittedSnapshot = '';
    this.valueChange.emit({ ...this.draft });
    this.abilitySearch.emit('');
  }

  get hasActiveFilters(): boolean {
    return !!(this.draft.search || this.draft.type || this.draft.ability || this.draft.generation);
  }

  private emitIfChanged(): void {
    const snapshot = JSON.stringify(this.draft);
    if (snapshot === this.lastEmittedSnapshot) {
      return;
    }

    this.lastEmittedSnapshot = snapshot;
    this.valueChange.emit({ ...this.draft });
  }
}
