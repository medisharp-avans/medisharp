import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { handleKeyboardShortcut } from '@/composables/general/useKeyboardShortcuts'

describe('handleKeyboardShortcut', () => {
  beforeEach(() => {
    document.body.innerHTML = `
            <header>
                <a id="profile-button" href="/profiel">Profile</a>
            </header>
            <aside id="main-navigation">
                <a id="first-nav-link" href="/">Home</a>
                <a href="/afspraken">Appointments</a>
            </aside>
            <main id="main-content" tabindex="-1">
                Main content
            </main>
        `
  })

  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('focuses the first navigation link on Alt + N', () => {
    const event = new KeyboardEvent('keydown', { key: 'n', altKey: true })
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault')

    handleKeyboardShortcut(event)

    const navLink = document.getElementById('first-nav-link')
    expect(preventDefaultSpy).toHaveBeenCalled()
    expect(document.activeElement).toBe(navLink)
  })

  it('focuses the profile button on Alt + P', () => {
    const event = new KeyboardEvent('keydown', { key: 'P', altKey: true })
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault')

    handleKeyboardShortcut(event)

    const profileButton = document.getElementById('profile-button')
    expect(preventDefaultSpy).toHaveBeenCalled()
    expect(document.activeElement).toBe(profileButton)
  })

  it('focuses the main content on Alt + M', () => {
    const event = new KeyboardEvent('keydown', { key: 'm', altKey: true })
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault')

    handleKeyboardShortcut(event)

    const mainContent = document.getElementById('main-content')
    expect(preventDefaultSpy).toHaveBeenCalled()
    expect(document.activeElement).toBe(mainContent)
  })

  it('does nothing when Alt is not pressed', () => {
    const event = new KeyboardEvent('keydown', { key: 'n', altKey: false })
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault')

    handleKeyboardShortcut(event)

    expect(preventDefaultSpy).not.toHaveBeenCalled()
    expect(document.activeElement).toBe(document.body)
  })
})
