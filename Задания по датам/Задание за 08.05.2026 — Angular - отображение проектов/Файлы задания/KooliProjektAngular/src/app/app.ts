import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectService, ProjectSummary } from './project.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly projectsApi = inject(ProjectService);
  readonly projects = signal<ProjectSummary[]>([]);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    try {
      this.loading.set(true);
      this.error.set(null);
      this.projects.set(await this.projectsApi.list());
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : 'Unable to load projects.');
    } finally {
      this.loading.set(false);
    }
  }
}
