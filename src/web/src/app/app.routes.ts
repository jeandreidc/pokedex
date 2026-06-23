import { Routes } from '@angular/router';
import { PokedexPageComponent } from './features/pokedex/pokedex-page.component';

export const routes: Routes = [
  { path: '', component: PokedexPageComponent },
  { path: '**', redirectTo: '' }
];
