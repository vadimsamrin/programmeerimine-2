import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface ProjectSummary {
  id: number;
  name: string;
  startDate: string;
  deadline: string;
  budget: number;
  currentCost: number;
  taskCount: number;
  completedTaskCount: number;
}

interface LoginResponse { accessToken: string; }
interface OperationResult<T> { value: T; }
interface PagedResult<T> { results: T[]; }

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5179/';

  async list(): Promise<ProjectSummary[]> {
    const login = await firstValueFrom(this.http.post<LoginResponse>(`${this.apiUrl}login?useCookies=false`, {
      email: 'demo@project.local', password: 'Project123!'
    }));
    const headers = new HttpHeaders({ Authorization: `Bearer ${login.accessToken}` });
    const response = await firstValueFrom(this.http.get<OperationResult<PagedResult<ProjectSummary>>>(
      `${this.apiUrl}api/projects?page=1&pageSize=20`, { headers }));
    return response.value.results;
  }
}
