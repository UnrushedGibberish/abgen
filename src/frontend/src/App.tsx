import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { createProject, getProject, getProjects } from './api'
import type { Project } from './types'
import './App.css'

function getProjectIdFromPath(): string | null {
  const match = window.location.pathname.match(/^\/project\/([^/]+)\/?$/)
  return match ? match[1] : null
}

function App() {
  const [projects, setProjects] = useState<Project[]>([])
  const [loading, setLoading] = useState(true)
  const [projectId, setProjectId] = useState<string | null>(() => getProjectIdFromPath())
  const [project, setProject] = useState<Project | null>(null)
  const [projectLoading, setProjectLoading] = useState(() => getProjectIdFromPath() !== null)
  const [projectError, setProjectError] = useState<string | null>(null)
  const [newProjectModalOpen, setNewProjectModalOpen] = useState(false)
  const [projectName, setProjectName] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [sidebarExpanded, setSidebarExpanded] = useState(false)
  const projectNameInputRef = useRef<HTMLInputElement>(null)
  const newProjectButtonRef = useRef<HTMLButtonElement>(null)
  const modalWasOpen = useRef(false)

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

  useEffect(() => {
    const handlePopState = () => setProjectId(getProjectIdFromPath())
    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  useEffect(() => {
    if (!projectId) {
      setProject(null)
      setProjectLoading(false)
      setProjectError(null)
      return
    }

    const controller = new AbortController()
    setProjectLoading(true)
    setProjectError(null)
    void getProject(projectId, controller.signal)
      .then(setProject)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setProjectError(requestError instanceof Error ? requestError.message : 'Unable to load this project.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setProjectLoading(false)
      })

    return () => controller.abort()
  }, [projectId])

  useEffect(() => {
    if (newProjectModalOpen) {
      projectNameInputRef.current?.focus()
    } else if (modalWasOpen.current) {
      newProjectButtonRef.current?.focus()
    }
    modalWasOpen.current = newProjectModalOpen
  }, [newProjectModalOpen])

  useEffect(() => {
    if (!newProjectModalOpen) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !creating) setNewProjectModalOpen(false)
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [newProjectModalOpen, creating])

  function openNewProjectModal() {
    setProjectName('')
    setCreateError(null)
    setNewProjectModalOpen(true)
  }

  function closeNewProjectModal() {
    if (!creating) setNewProjectModalOpen(false)
  }

  async function handleSaveProject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const name = projectName.trim()
    if (!name) {
      setCreateError('Enter a project name.')
      projectNameInputRef.current?.focus()
      return
    }

    setCreating(true)
    setCreateError(null)
    try {
      const createdProject = await createProject(name)
      setProjects((currentProjects) => [createdProject, ...currentProjects])
      window.history.pushState({}, '', `/project/${createdProject.id}`)
      setProjectId(createdProject.id)
      setNewProjectModalOpen(false)
    } catch (requestError) {
      setCreateError(requestError instanceof Error ? requestError.message : 'Unable to create project.')
    } finally {
      setCreating(false)
    }
  }

  return (
    <main className="app-shell">
      <aside className={`sidebar${sidebarExpanded ? ' sidebar-expanded' : ''}`}>
        <a className="wordmark" href="/" aria-label="AB Gen home">
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
          <a className="sidebar-link sidebar-link-active" href="/" aria-label="Projects">
            <span className="sidebar-link-icon" aria-hidden="true">▤</span>
            <span className="sidebar-link-label">Projects</span>
          </a>
        </nav>
      </aside>

      <div className="page-content">
        {projectId ? (
          <section className="project-page" aria-labelledby="project-title">
            {projectLoading && <p className="project-status" role="status">Loading project…</p>}
            {projectError && <div className="error-banner" role="alert">{projectError}</div>}
            {project?.id === projectId && (
              <>
                <h1 className="project-title" id="project-title">{project.name}</h1>
                <hr className="project-divider" />
                <label className="ebook-upload">
                  <input
                    className="ebook-upload-input"
                    type="file"
                    accept=".epub,.pdf,.mobi,.azw,.azw3"
                    multiple
                    aria-label="Upload ebooks"
                  />
                  <span className="ebook-upload-icon" aria-hidden="true">↑</span>
                  <span>Upload ebooks</span>
                </label>
              </>
            )}
          </section>
        ) : (
          <section className="library" id="projects" aria-labelledby="projects-title">
            <div className="section-heading">
              <h2 id="projects-title">Your projects <span>{projects.length.toString().padStart(2, '0')}</span></h2>
              <button
                ref={newProjectButtonRef}
                className="create-project-button"
                type="button"
                onClick={openNewProjectModal}
              >
                New project
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
                {projects.map((listedProject) => (
                  <a className="project-card" href={`/project/${listedProject.id}`} key={listedProject.id}>
                    <div className="project-art" aria-hidden="true"><span>✳</span></div>
                    <div className="project-info">
                      <h3>{listedProject.name}</h3>
                      <p>Created {new Date(listedProject.createdAt).toLocaleDateString()}</p>
                    </div>
                  </a>
                ))}
              </div>
            )}
          </section>
        )}
      </div>

      {newProjectModalOpen && (
        <div
          className="modal-backdrop"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) closeNewProjectModal()
          }}
        >
          <section className="project-modal" role="dialog" aria-modal="true" aria-labelledby="new-project-title">
            <div className="modal-heading">
              <h2 id="new-project-title">New project</h2>
              <button
                className="modal-close-button"
                type="button"
                aria-label="Close new project dialog"
                disabled={creating}
                onClick={closeNewProjectModal}
              >
                ×
              </button>
            </div>
            <form className="project-form" onSubmit={(event) => void handleSaveProject(event)}>
              <label htmlFor="project-name">Project name</label>
              <input
                ref={projectNameInputRef}
                id="project-name"
                className="project-name-input"
                type="text"
                value={projectName}
                maxLength={200}
                required
                autoComplete="off"
                aria-invalid={Boolean(createError)}
                aria-describedby={createError ? 'project-name-error' : undefined}
                onChange={(event) => {
                  setProjectName(event.target.value)
                  setCreateError(null)
                }}
              />
              {createError && <div className="error-banner" id="project-name-error" role="alert">{createError}</div>}
              <div className="modal-actions">
                <button className="modal-button modal-button-secondary" type="button" disabled={creating} onClick={closeNewProjectModal}>
                  Cancel
                </button>
                <button className="modal-button modal-button-primary" type="submit" disabled={creating}>
                  {creating ? 'Saving…' : 'Save'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </main>
  )
}

export default App
