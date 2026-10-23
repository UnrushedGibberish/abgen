import { useCallback, useEffect, useState } from 'react'
import { enqueuePlaceholder, getJobs } from './api'
import type { Job } from './types'
import './App.css'

const refreshIntervalMs = 1_500

function App() {
  const [jobs, setJobs] = useState<Job[]>([])
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [lastRefreshedAt, setLastRefreshedAt] = useState<Date | null>(null)

  const refresh = useCallback(async (signal?: AbortSignal) => {
    try {
      const currentJobs = await getJobs(signal)
      setJobs(currentJobs)
      setLastRefreshedAt(new Date())
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

  async function handleCreateJob() {
    setSubmitting(true)
    setError(null)
    try {
      const newJob = await enqueuePlaceholder()
      setJobs((currentJobs) => [newJob, ...currentJobs.filter((job) => job.id !== newJob.id)])
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to create a job.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <a className="wordmark" href="#jobs" aria-label="Abgen home">
          <span className="wordmark-icon" aria-hidden="true">A</span>
          <span>abgen</span>
        </a>
        <span className="topbar-note">AUDIOBOOK WORKSPACE</span>
        <span className="service-state"><span className="state-dot" /> DURABLE WORKER</span>
      </header>

      <section className="intro" aria-labelledby="page-title">
        <p className="eyebrow">YOUR PERSONAL AUDIO STUDIO</p>
        <h1 id="page-title">A good story<br />deserves a <em>voice.</em></h1>
        <p className="intro-copy">
          An audiobook studio is taking shape. This first step proves your background work
          finishes safely and stays here, even after you step away.
        </p>
        <div className="intro-actions">
          <button className="primary-button" type="button" onClick={handleCreateJob} disabled={submitting}>
            <span aria-hidden="true">＋</span> {submitting ? 'Starting…' : 'Run a demo job'}
          </button>
          <p>Durable pipeline proof <span aria-hidden="true">·</span> no book required</p>
        </div>
        <div className="shelf-decoration" aria-hidden="true">
          <span className="book book-1" /><span className="book book-2" />
          <span className="book book-3" /><span className="book book-4" />
          <span className="book book-5" /><span className="book book-6" />
          <span className="book book-7" /><span className="book book-8" />
          <span className="shelf-line" />
        </div>
        <div className="trust-row">
          <span><strong>01</strong> YOUR REQUEST, SAVED</span>
          <span><strong>02</strong> PROCESSED IN THE BACKGROUND</span>
          <span><strong>03</strong> PROGRESS, ALWAYS HERE</span>
        </div>
      </section>

      <section className="library" id="jobs" aria-labelledby="jobs-title">
        <div className="section-heading">
          <div>
            <p className="eyebrow">THE WORK QUEUE</p>
            <h2 id="jobs-title">Your library <span>{latestJobs.length.toString().padStart(2, '0')}</span></h2>
          </div>
          <p className="refresh-note"><span className="refresh-icon" aria-hidden="true">↻</span> {lastRefreshedAt ? `Synced ${lastRefreshedAt.toLocaleTimeString()}` : 'Syncing status…'}</p>
        </div>

        {error && <div className="error-banner" role="alert">{error}</div>}

        {loading ? (
          <p className="empty-state">Connecting to your library…</p>
        ) : latestJobs.length === 0 ? (
          <div className="empty-state">
            <span className="empty-icon" aria-hidden="true">✳</span>
            <p>Your next listen starts here.</p>
            <span>There are no jobs yet. Try the demo above.</span>
          </div>
        ) : (
          <div className="job-list">
            {latestJobs.map((job) => (
              <article className="job-card" key={job.id}>
                <div className={`job-art job-art-${job.status.toLowerCase()}`} aria-hidden="true"><span>✳</span></div>
                <div className="job-info">
                  <div className="job-title-row">
                    <h3>Placeholder job</h3>
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

      <footer className="footer">
        <span>MADE FOR STORIES WORTH HEARING</span>
        <span>STATUS LIVES ON THE SERVER, NOT IN THIS TAB.</span>
      </footer>
    </main>
  )
}

export default App
