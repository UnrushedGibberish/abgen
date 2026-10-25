import type { Project } from './types'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || ''

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, init)
  if (!response.ok) {
    throw new Error(`API request failed (${response.status}).`)
  }

  return (await response.json()) as T
}

export async function getProjects(signal?: AbortSignal): Promise<Project[]> {
  const response = await request<Project[] | { value: Project[] }>('/api/projects', { signal })
  return Array.isArray(response) ? response : response.value
}

export function createProject(name: string): Promise<Project> {
  return request<Project>('/api/projects', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  })
}
