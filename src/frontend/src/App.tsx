import { useEffect, useState } from 'react'
import { createProject, getProjects } from './api'
import type { Project } from './types'
import './App.css'

function App() {
  const [projects, setProjects] = useState<Project[]>([])
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [sidebarExpanded, setSidebarExpanded] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    void getProjects(controller.signal)
      .then(setProjects)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof Error ? requestError.message : 'Unable to load projects.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [])

  async function handleCreateProject() {
    setCreating(true)
    setError(null)
    try {
      const project = await createProject('New project')
      setProjects((currentProjects) => [project, ...currentProjects])
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to create project.')
    } finally {
      setCreating(false)
    }
  }

  return (
    <main className="app-shell">
      <aside className={`sidebar${sidebarExpanded ? ' sidebar-expanded' : ''}`}>
        <a className="wordmark" href="#projects" aria-label="AB Gen home">
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
          <a className="sidebar-link sidebar-link-active" href="#projects" aria-label="Projects">
            <span className="sidebar-link-icon" aria-hidden="true">▤</span>
            <span className="sidebar-link-label">Projects</span>
          </a>
        </nav>
      </aside>

      <div className="page-content">
        <section className="library" id="projects" aria-labelledby="projects-title">
          <div className="section-heading">
            <h2 id="projects-title">Your projects</h2>
            <button className="create-project-button" type="button" disabled={creating} onClick={() => void handleCreateProject()}>
              {creating ? 'Creating…' : 'New project'}
            </button>
          </div>

          {error && <div className="error-banner" role="alert">{error}</div>}

          {loading ? (
            <p className="empty-state">Connecting to your projects…</p>
          ) : projects.length === 0 ? (
            <div className="empty-state">
              <span className="empty-icon" aria-hidden="true">✳</span>
              <p>Your next listen starts here.</p>
              <span>Create a project to get started.</span>
            </div>
          ) : (
            <div className="project-list">
              {projects.map((project) => (
                <article className="project-card" key={project.id}>
                  <div className="project-art" aria-hidden="true"><span>✳</span></div>
                  <div className="project-info">
                    <h3>{project.name}</h3>
                    <p>Created {new Date(project.createdAt).toLocaleDateString()}</p>
                  </div>
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
