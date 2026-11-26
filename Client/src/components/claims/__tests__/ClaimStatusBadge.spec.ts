import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ClaimStatusBadge from '@/components/claims/ClaimStatusBadge.vue'
import { ClaimStatus } from '@/types/claim'

describe('ClaimStatusBadge', () => {
  it('shows the Dutch label with a warning icon for claims to review', () => {
    const wrapper = mount(ClaimStatusBadge, { props: { status: ClaimStatus.ToReview } })

    expect(wrapper.text()).toBe('Te controleren')
    expect(wrapper.classes()).toContain('bg-amber-100')
    expect(wrapper.find('svg').exists()).toBe(true)
  })

  it('shows no warning icon for other statuses', () => {
    const wrapper = mount(ClaimStatusBadge, { props: { status: ClaimStatus.Processed } })

    expect(wrapper.text()).toBe('Verwerkt')
    expect(wrapper.find('svg').exists()).toBe(false)
  })
})
