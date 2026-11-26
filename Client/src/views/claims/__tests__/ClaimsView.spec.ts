import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick, ref, toValue, type MaybeRefOrGetter } from 'vue'
import { ClaimStatus, type ClaimOverview } from '@/types/claim'
import type { PagedResult } from '@/types/pagination'

const state = {
  data: ref<PagedResult<ClaimOverview> | null>(null),
  loading: ref(false),
  error: ref<string | null>(null),
}
const toReviewCount = ref<number | null>(null)
let lastParams: MaybeRefOrGetter<Record<string, unknown>> = () => ({})

vi.mock('@/composables/api/useFetch', () => ({
  useFetch: (url: string, params: MaybeRefOrGetter<Record<string, unknown>> = () => ({})) => {
    const refetch = vi.fn<() => Promise<void>>()
    if (url === 'api/Claims/to-review-count') {
      return { data: toReviewCount, loading: ref(false), error: ref(null), refetch }
    }
    lastParams = params
    return { ...state, refetch }
  },
}))

import ClaimsView from '@/views/claims/ClaimsView.vue'

const claim = (
  id: number,
  status: ClaimStatus,
  overrides: Partial<ClaimOverview> = {},
): ClaimOverview => ({
  id,
  patientId: 1,
  patientName: 'Jan Jansen',
  policyNumber: 'ABC123456',
  status,
  lineCount: 1,
  totalAmount: 75,
  reviewedByName: null,
  createdAt: '2026-11-20T09:00:00Z',
  submittedAt: null,
  lastModifiedAt: '2026-11-21T09:00:00Z',
  ...overrides,
})

const pageOf = (
  items: ClaimOverview[],
  overrides: Partial<PagedResult<ClaimOverview>> = {},
): PagedResult<ClaimOverview> => ({
  items,
  page: 1,
  pageSize: 20,
  totalCount: items.length,
  totalPages: items.length > 0 ? 1 : 0,
  ...overrides,
})

const params = () => toValue(lastParams)

describe('ClaimsView', () => {
  beforeEach(() => {
    state.data.value = null
    state.loading.value = false
    state.error.value = null
    toReviewCount.value = null
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('marks claims that still need to be reviewed', () => {
    state.data.value = pageOf([claim(1, ClaimStatus.ToReview), claim(2, ClaimStatus.Processed)])
    toReviewCount.value = 1
    const wrapper = mount(ClaimsView)

    expect(wrapper.find('[data-testid="claim-row-1"]').classes()).toContain('bg-amber-50')
    expect(wrapper.find('[data-testid="claim-row-2"]').classes()).not.toContain('bg-amber-50')
    expect(wrapper.find('[data-testid="claim-status-1"]').text()).toBe('Te controleren')
    expect(wrapper.find('[data-testid="claims-to-review-count"]').text()).toContain(
      '1 declaratie moet nog gecontroleerd worden',
    )
  })

  it('shows the number right-aligned and the reviewer, empty when not reviewed', () => {
    state.data.value = pageOf([
      claim(1, ClaimStatus.ReadyToSend, { reviewedByName: 'Sanne Medewerker' }),
      claim(2, ClaimStatus.ToReview),
    ])
    const wrapper = mount(ClaimsView)

    expect(wrapper.find('[data-testid="claim-number-1"]').classes()).toContain('text-right')
    expect(wrapper.find('[data-testid="claim-reviewed-by-1"]').text()).toBe('Sanne Medewerker')
    expect(wrapper.find('[data-testid="claim-reviewed-by-2"]').text()).toBe('')
  })

  it('formats the total amount in euros', () => {
    state.data.value = pageOf([claim(1, ClaimStatus.ToReview, { totalAmount: 420 })])
    const wrapper = mount(ClaimsView)

    expect(wrapper.find('[data-testid="claim-total-1"]').text().replace(/\s/g, ' ')).toBe(
      '€ 420,00',
    )
  })

  it('selects one claim, all claims on the page, and clears the selection', async () => {
    state.data.value = pageOf([claim(1, ClaimStatus.ToReview), claim(2, ClaimStatus.Processed)])
    const wrapper = mount(ClaimsView)
    const count = () => wrapper.find('[data-testid="claims-selected-count"]').text()

    await wrapper.find('[data-testid="claim-select-1"]').trigger('change')
    expect(count()).toBe('1 geselecteerd')

    await wrapper.find('[data-testid="claims-select-page"]').trigger('change')
    expect(count()).toBe('2 geselecteerd')

    await wrapper.find('[data-testid="claims-clear-selection"]').trigger('click')
    expect(count()).toBe('0 geselecteerd')
  })

  it('enables the batch button only when claims are selected', async () => {
    state.data.value = pageOf([claim(1, ClaimStatus.ToReview)])
    const wrapper = mount(ClaimsView)
    const button = () => wrapper.find('[data-testid="claims-add-to-batch"]')

    expect(button().attributes('disabled')).toBeDefined()
    await wrapper.find('[data-testid="claim-select-1"]').trigger('change')
    expect(button().attributes('disabled')).toBeUndefined()
  })

  it('sorts on a column and toggles the direction', async () => {
    state.data.value = pageOf([claim(1, ClaimStatus.ToReview)])
    const wrapper = mount(ClaimsView)

    expect(params().sortBy).toBeUndefined()

    await wrapper.find('[data-testid="claims-sort-Patient"]').trigger('click')
    expect(params()).toMatchObject({ sortBy: 'Patient', sortDescending: false })

    await wrapper.find('[data-testid="claims-sort-Patient"]').trigger('click')
    expect(params()).toMatchObject({ sortBy: 'Patient', sortDescending: true })
  })

  it('clears the search and sort with the reset button', async () => {
    vi.useFakeTimers()
    state.data.value = pageOf([claim(1, ClaimStatus.ToReview)])
    const wrapper = mount(ClaimsView)
    const reset = () => wrapper.find('[data-testid="claims-reset-filters"]')

    expect(reset().attributes('disabled')).toBeDefined()

    await wrapper.find('[data-testid="claims-sort-Patient"]').trigger('click')
    expect(reset().attributes('disabled')).toBeUndefined()
    await wrapper.find('[data-testid="claims-search"]').setValue('Jansen')
    vi.advanceTimersByTime(300)
    await nextTick()
    expect(params()).toMatchObject({ search: 'Jansen', sortBy: 'Patient' })

    await reset().trigger('click')
    expect(params().search).toBeUndefined()
    expect(params().sortBy).toBeUndefined()
    expect((wrapper.find('[data-testid="claims-search"]').element as HTMLInputElement).value).toBe(
      '',
    )
    expect(reset().attributes('disabled')).toBeDefined()
  })

  it('sends the search term after the user stops typing and goes back to page 1', async () => {
    vi.useFakeTimers()
    state.data.value = pageOf([claim(1, ClaimStatus.ToReview)], { totalPages: 3, page: 2 })
    const wrapper = mount(ClaimsView)

    await wrapper.find('[data-testid="pagination-next"]').trigger('click')
    expect(params().page).toBe(2)

    await wrapper.find('[data-testid="claims-search"]').setValue('  Jansen ')
    expect(params().search).toBeUndefined()

    vi.advanceTimersByTime(300)
    await nextTick()
    expect(params()).toMatchObject({ search: 'Jansen', page: 1 })
  })

  it('shows a message when the search finds nothing', async () => {
    vi.useFakeTimers()
    state.data.value = pageOf([])
    const wrapper = mount(ClaimsView)

    await wrapper.find('[data-testid="claims-search"]').setValue('bestaat niet')
    vi.advanceTimersByTime(300)
    await nextTick()

    expect(wrapper.find('[data-testid="claims-empty"]').text()).toBe(
      'Geen declaraties gevonden voor deze zoekterm.',
    )
  })

  it('hides the to-review banner when nothing needs to be reviewed', () => {
    state.data.value = pageOf([claim(1, ClaimStatus.Processed)])
    toReviewCount.value = 0
    const wrapper = mount(ClaimsView)

    expect(wrapper.find('[data-testid="claims-to-review-count"]').exists()).toBe(false)
  })

  it('shows an error message when loading fails', () => {
    state.error.value = 'Er is iets misgegaan.'
    const wrapper = mount(ClaimsView)

    expect(wrapper.find('[data-testid="claims-error"]').exists()).toBe(true)
  })
})
