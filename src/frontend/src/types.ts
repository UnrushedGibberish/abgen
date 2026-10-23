export type JobStatus = 'Queued' | 'Running' | 'Succeeded' | 'Failed'

export type JobEventLevel = 'Information' | 'Warning' | 'Error'

export interface JobEvent {
  sequenceNumber: number
  level: JobEventLevel
  code: string
  message: string
  occurredAt: string
}

export interface Job {
  id: string
  jobType: string
  status: JobStatus
  progressPercent: number
  currentStep: string
  createdAt: string
  updatedAt: string
  startedAt: string | null
  completedAt: string | null
  attemptCount: number
  errorCode: string | null
  errorMessage: string | null
  events: JobEvent[]
}
