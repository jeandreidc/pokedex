import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-generation-ring',
  standalone: true,
  templateUrl: './generation-ring.component.html',
  styleUrl: './generation-ring.component.scss'
})
export class GenerationRingComponent {
  @Input({ required: true }) displayName!: string;
  @Input({ required: true }) caughtCount!: number;
  @Input({ required: true }) totalInGeneration!: number;
  @Input({ required: true }) caughtPercentage!: number;

  readonly size = 72;
  readonly stroke = 6;

  get radius(): number {
    return (this.size - this.stroke) / 2;
  }

  get circumference(): number {
    return 2 * Math.PI * this.radius;
  }

  get dashOffset(): number {
    const progress = Math.min(100, Math.max(0, this.caughtPercentage));
    return this.circumference - (progress / 100) * this.circumference;
  }

  get ringColor(): string {
    const pct = this.caughtPercentage;
    if (pct >= 75) return '#16a34a';
    if (pct >= 50) return '#2563eb';
    if (pct >= 25) return '#d97706';
    return '#dc2626';
  }
}
