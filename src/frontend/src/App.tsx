import { useCallback, useEffect, useState } from 'react'
import { getJobs } from './api'
import type { Job } from './types'
import './App.css'

const refreshIntervalMs = 1_500

function App() {
  const [jobs, setJobs] = useState<Job[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [sidebarExpanded, setSidebarExpanded] = useState(false)

  const refresh = useCallback(async (signal?: AbortSignal) => {
    try {
      const currentJobs = await getJobs(signal)
      setJobs(currentJobs)
      setError(null)
    } catch (requestError) {
      if (requestError instanceof DOMException && requestError.name === 'AbortError') {
        return
      }
      setError(requestError instanceof Error ? requestError.message : 'Unable to load jobs.')
    } finally {
      if (!signal?.aborted) {
        setLoading(false)
      }
    }
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    queueMicrotask(() => void refresh(controller.signal))
    const intervalId = window.setInterval(() => void refresh(), refreshIntervalMs)
    return () => {
      controller.abort()
      window.clearInterval(intervalId)
    }
  }, [refresh])

  const latestJobs = jobs

  return (
    <main className="app-shell">
      <aside className={`sidebar${sidebarExpanded ? ' sidebar-expanded' : ''}`}>
        <a className="wordmark" href="#jobs" aria-label="AB Gen home">
          <span className="wordmark-icon" aria-hidden="true">A</span>
          <span className="wordmark-label">AB Gen</span>
        </a>
        <button
          className="sidebar-toggle"
          type="button"
          aria-label={sidebarExpanded ? 'Collapse sidebar' : 'Expand sidebar'}
          aria-expanded={sidebarExpanded}
          onClick={() => setSidebarExpanded((expanded) => !expanded)}
        >
          <span aria-hidden="true">{sidebarExpanded ? '‹' : '›'}</span>
        </button>
        <nav className="sidebar-nav" aria-label="Main navigation">
          <a className="sidebar-link sidebar-link-active" href="#jobs" aria-label="Library">
            <span className="sidebar-link-icon" aria-hidden="true">▤</span>
            <span className="sidebar-link-label">Library</span>
          </a>
        </nav>
      </aside>

      <div className="page-content">
        <section className="library" id="jobs" aria-labelledby="jobs-title">
          <div className="section-heading">
            <h2 id="jobs-title">Your library <span>{latestJobs.length.toString().padStart(2, '0')}</span></h2>
          </div>

          {error && <div className="error-banner" role="alert">{error}</div>}

          {loading ? (
            <p className="empty-state">Connecting to your library…</p>
          ) : latestJobs.length === 0 ? (
            <div className="empty-state">
              <span className="empty-icon" aria-hidden="true">✳</span>
              <p>Your next listen starts here.</p>
              <span>There are no jobs yet.</span>
            </div>
          ) : (
            <div className="job-list">
              {latestJobs.map((job) => (
                <article className="job-card" key={job.id}>
                  <div className={`job-art job-art-${job.status.toLowerCase()}`} aria-hidden="true"><span>✳</span></div>
                  <div className="job-info">
                    <div className="job-title-row">
                      <h3>{job.jobType}</h3>
                      <span className={`status-pill status-${job.status.toLowerCase()}`}>{job.status}</span>
                    </div>
                    <p className="job-step">{job.currentStep}</p>
                    <div className="progress-track" role="progressbar" aria-label="Job progress" aria-valuenow={job.progressPercent} aria-valuemin={0} aria-valuemax={100}>
                      <div className="progress-value" style={{ width: `${job.progressPercent}%` }} />
                    </div>
                    <div className="job-meta">
                      <span>{job.progressPercent}% complete</span>
                      <span>Attempt {job.attemptCount}</span>
                    </div>
                    {job.errorMessage && <p className="job-error">{job.errorMessage}</p>}
                  </div>
                  <details className="event-details">
                    <summary>Activity <span>{job.events.length}</span></summary>
                    <ol className="event-list">
                      {job.events.map((event) => (
                        <li className={`event-${event.level.toLowerCase()}`} key={`${job.id}-${event.sequenceNumber}`}>
                          <span>{event.message}</span>
                          <time dateTime={event.occurredAt}>{new Date(event.occurredAt).toLocaleTimeString()}</time>
                        </li>
                      ))}
                    </ol>
                  </details>
                </article>
              ))}
            </div>
          )}
        </section>
      </div>
    </main>
  )
}

export default App
