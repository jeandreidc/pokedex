import { Routes } from '@angular/router';
import { LoginPageComponent } from './features/auth/login-page.component';
import { RegisterPageComponent } from './features/auth/register-page.component';
import { PokedexPageComponent } from './features/pokedex/pokedex-page.component';

export const routes: Routes = [
  { path: '', component: PokedexPageComponent },
  { path: 'login', component: LoginPageComponent },
  { path: 'register', component: RegisterPageComponent },
  { path: '**', redirectTo: '' }
];
