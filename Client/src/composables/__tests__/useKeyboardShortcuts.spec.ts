import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import useKeyboardShortcuts from '@/composables/useKeyboardShortcuts'

describe('useKeyboardShortcuts', () => {
  beforeEach(() => {
    document.body.innerHTML = `
            <header>
                <a id="profile-button" href="/profiel">Profiel</a>
            </header>
            <aside id="main-navigation">
                <a id="first-nav-link" href="/">Home</a>
                <a href="/afspraken">Afspraken</a>
            </aside>
            <main id="main-content" tabindex="-1">
                Hoofdinhoud
            </main>
        `
  })

  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('focust op de eerste link in de navigatie bij Alt + N', () => {
    const event = new KeyboardEvent('keydown', { key: 'n', altKey: true })
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault')

    useKeyboardShortcuts(event)

    const navLink = document.getElementById('first-nav-link')
    expect(preventDefaultSpy).toHaveBeenCalled()
    expect(document.activeElement).toBe(navLink)
  })

  it('focust op de profielknop bij Alt + P', () => {
    const event = new KeyboardEvent('keydown', { key: 'P', altKey: true })
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault')

    useKeyboardShortcuts(event)

    const profileButton = document.getElementById('profile-button')
    expect(preventDefaultSpy).toHaveBeenCalled()
    expect(document.activeElement).toBe(profileButton)
  })

  it('focust op de main content bij Alt + M', () => {
    const event = new KeyboardEvent('keydown', { key: 'm', altKey: true })
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault')

    useKeyboardShortcuts(event)

    const mainContent = document.getElementById('main-content')
    expect(preventDefaultSpy).toHaveBeenCalled()
    expect(document.activeElement).toBe(mainContent)
  })

  it('doet niets wanneer Alt niet is ingedrukt', () => {
    const event = new KeyboardEvent('keydown', { key: 'n', altKey: false })
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault')

    useKeyboardShortcuts(event)

    expect(preventDefaultSpy).not.toHaveBeenCalled()
    expect(document.activeElement).toBe(document.body)
  })
})
