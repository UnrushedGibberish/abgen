import type { Job } from './types'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || ''

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, init)
  if (!response.ok) {
    throw new Error(`API request failed (${response.status}).`)
  }

  return (await response.json()) as T
}

export async function getJobs(signal?: AbortSignal): Promise<Job[]> {
  const response = await request<Job[] | { value: Job[] }>('/api/jobs', { signal })
  return Array.isArray(response) ? response : response.value
}

export function enqueuePlaceholder(signal?: AbortSignal): Promise<Job> {
  return request<Job>('/api/jobs', {
    method: 'POST',
    signal,
  })
}
