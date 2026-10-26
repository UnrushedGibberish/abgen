import type { CreateProjectRequest, ProjectResponse } from './generated/types.gen'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || ''

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, init)
  if (!response.ok) {
    throw new Error(`API request failed (${response.status}).`)
  }

  return (await response.json()) as T
}

export function getProjects(signal?: AbortSignal): Promise<ProjectResponse[]> {
  return request<ProjectResponse[]>('/api/projects', { signal })
}

export function createProject(name: CreateProjectRequest['name']): Promise<ProjectResponse> {
  const body: CreateProjectRequest = { name }
  return request<ProjectResponse>('/api/projects', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
