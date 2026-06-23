import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CollectionStore } from '../../core/services/collection.store';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.component.html',
  styleUrl: './auth-page.component.scss'
})
export class LoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly collectionStore = inject(CollectionStore);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  error: string | null = null;
  loading = false;

  form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    this.error = null;
    const { username, password } = this.form.getRawValue();

    this.auth.login(username, password).subscribe({
      next: () => {
        this.collectionStore.loadForUser();
        void this.router.navigate(['/']);
      },
      error: () => {
        this.loading = false;
        this.error = 'Invalid username or password.';
      },
      complete: () => {
        this.loading = false;
      }
    });
  }
}
