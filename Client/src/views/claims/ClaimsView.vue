<script setup lang="ts">
// Overview of all claims (DEC-01). Search, sort and paging happen on the server.
// Without a sort column the server puts the claims to review first.
import { computed, ref, watch } from 'vue'
import { HugeiconsIcon } from '@hugeicons/vue'
import { AlertCircleIcon, FilterRemoveIcon, FolderAddIcon } from '@hugeicons/core-free-icons'
import BaseButton from '@/components/elements/BaseButton.vue'
import PaginationControls from '@/components/elements/PaginationControls.vue'
import BaseSelect from '@/components/elements/BaseSelect.vue'
import SearchField from '@/components/elements/SearchField.vue'
import SelectionBar from '@/components/elements/SelectionBar.vue'
import SortableColumnHeader from '@/components/elements/SortableColumnHeader.vue'
import ClaimStatusBadge from '@/components/claims/ClaimStatusBadge.vue'
import { useFetch } from '@/composables/api/useFetch'
import { endpoints } from '@/services/endpoints'
import {
  ClaimStatus,
  claimStatusOptions,
  type ClaimOverview,
  type ClaimSortField,
} from '@/types/claim'
import type { PagedResult } from '@/types/pagination'

const PAGE_SIZE = 20
const SEARCH_DELAY_MS = 300

const searchInput = ref('')
/** Only claims with this status; null shows all claims. */
const statusFilter = ref<ClaimStatus | null>(null)
const search = ref('')
const sortBy = ref<ClaimSortField | null>(null)
const sortDescending = ref(false)
const page = ref(1)

const {
  data: result,
  loading,
  error,
} = useFetch<PagedResult<ClaimOverview>>(endpoints.claims.overview, () => ({
  search: search.value || undefined,
  status: statusFilter.value ?? undefined,
  sortBy: sortBy.value ?? undefined,
  sortDescending: sortBy.value ? sortDescending.value : undefined,
  page: page.value,
  pageSize: PAGE_SIZE,
}))

// Separate request: the banner counts all claims to review, not only the ones on this page.
const { data: toReviewCount } = useFetch<number>(endpoints.claims.toReviewCount)

const claims = computed(() => result.value?.items ?? [])

// Wait until the user stops typing, so not every key press sends a request.
let searchTimer: ReturnType<typeof setTimeout> | undefined
watch(searchInput, (value) => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    search.value = value.trim()
  }, SEARCH_DELAY_MS)
})

// A new search or sort starts at page 1 again.
watch([search, statusFilter, sortBy, sortDescending], () => {
  page.value = 1
})

// ----- Sorting -----
const columns: { field: ClaimSortField; label: string; alignRight?: boolean }[] = [
  { field: 'Id', label: 'Nummer', alignRight: true },
  { field: 'Status', label: 'Status' },
  { field: 'Patient', label: 'Patiënt' },
  { field: 'PolicyNumber', label: 'Polisnummer' },
  { field: 'LineCount', label: 'Behandelingen', alignRight: true },
  { field: 'TotalAmount', label: 'Totaalbedrag', alignRight: true },
  { field: 'ReviewedBy', label: 'Gecontroleerd door' },
  { field: 'CreatedAt', label: 'Aangemaakt' },
  { field: 'LastModifiedAt', label: 'Laatst gewijzigd' },
]

function toggleSort(field: ClaimSortField) {
  if (sortBy.value === field) {
    sortDescending.value = !sortDescending.value
  } else {
    sortBy.value = field
    sortDescending.value = false
  }
}

// ----- Reset search, status filter and sort -----
const hasFilters = computed(
  () => searchInput.value.trim() !== '' || statusFilter.value !== null || sortBy.value !== null,
)

function resetFilters() {
  clearTimeout(searchTimer)
  searchInput.value = ''
  statusFilter.value = null
  search.value = ''
  sortBy.value = null
  sortDescending.value = false
}

// ----- Selection (for adding claims to a batch, AC-DEC-01_5) -----
const selectedIds = ref<Set<number>>(new Set())

const allOnPageSelected = computed(
  () => claims.value.length > 0 && claims.value.every((c) => selectedIds.value.has(c.id)),
)

function toggleSelected(id: number) {
  const next = new Set(selectedIds.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  selectedIds.value = next
}

function toggleAllOnPage() {
  const next = new Set(selectedIds.value)
  if (allOnPageSelected.value) claims.value.forEach((c) => next.delete(c.id))
  else claims.value.forEach((c) => next.add(c.id))
  selectedIds.value = next
}

function clearSelection() {
  selectedIds.value = new Set()
}

function addSelectionToBatch() {
  // TODO (AC-DEC-01_5): add the selected claims to a batch.
}

function openClaim(claim: ClaimOverview) {
  // TODO: open the claim details page once it exists.
  void claim
}

// ----- Formatting -----
const currency = new Intl.NumberFormat('nl-NL', { style: 'currency', currency: 'EUR' })
const formatAmount = (amount: number) => currency.format(amount)
const formatDate = (value: string) =>
  new Date(value).toLocaleDateString('nl-NL', { day: 'numeric', month: 'short', year: 'numeric' })

const isToReview = (claim: ClaimOverview) => claim.status === ClaimStatus.ToReview
</script>

<template>
  <section class="p-6 text-gray-900" aria-labelledby="claims-title">
    <header class="mb-6">
      <h1 id="claims-title" class="text-2xl font-bold">Declaraties</h1>
      <p
        v-if="toReviewCount"
        class="mt-2 inline-flex items-center gap-2 text-amber-900"
        data-testid="claims-to-review-count"
      >
        <HugeiconsIcon :icon="AlertCircleIcon" class="w-5 h-5" aria-hidden="true" />
        {{ toReviewCount }}
        {{ toReviewCount === 1 ? 'declaratie moet' : 'declaraties moeten' }} nog gecontroleerd
        worden.
      </p>
    </header>

    <div class="mb-4 flex w-full max-w-3xl items-end gap-2">
      <SearchField
        id="claims-search"
        v-model="searchInput"
        placeholder="Nummer, patiënt, polisnummer of medewerker"
        class="flex-1"
        data-testid="claims-search"
      />
      <BaseSelect
        id="claims-status-filter"
        v-model="statusFilter"
        label="Status"
        :options="claimStatusOptions"
        empty-label="Alle statussen"
        class="w-56 shrink-0"
        data-testid="claims-status-filter"
      />
      <button
        type="button"
        :disabled="!hasFilters"
        class="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-gray-300 bg-white text-gray-700 hover:bg-gray-100 cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-white"
        title="Zoeken, statusfilter en sortering wissen"
        aria-label="Zoeken, statusfilter en sortering wissen"
        data-testid="claims-reset-filters"
        @click="resetFilters"
      >
        <HugeiconsIcon :icon="FilterRemoveIcon" class="w-5 h-5" aria-hidden="true" />
      </button>
    </div>

    <p v-if="loading && !result" data-testid="claims-loading">Declaraties worden geladen...</p>
    <p v-else-if="error" role="alert" class="text-red-700" data-testid="claims-error">
      De declaraties konden niet worden geladen. Probeer het later opnieuw.
    </p>
    <p v-else-if="claims.length === 0" data-testid="claims-empty">
      {{
        search
          ? 'Geen declaraties gevonden voor deze zoekterm.'
          : statusFilter !== null
            ? 'Geen declaraties met deze status.'
            : 'Er zijn nog geen declaraties.'
      }}
    </p>

    <template v-else>
      <SelectionBar
        :count="selectedIds.size"
        singular="declaratie"
        plural="declaraties"
        data-testid="claims-selection"
      >
        <BaseButton
          variant="secondary"
          :disabled="selectedIds.size === 0"
          data-testid="claims-clear-selection"
          @click="clearSelection"
        >
          Selectie wissen
        </BaseButton>
        <BaseButton
          :disabled="selectedIds.size === 0"
          data-testid="claims-add-to-batch"
          @click="addSelectionToBatch"
        >
          <HugeiconsIcon :icon="FolderAddIcon" class="w-5 h-5" aria-hidden="true" />
          Toevoegen aan batch
        </BaseButton>
      </SelectionBar>

      <div class="overflow-x-auto bg-white rounded-2xl border border-gray-200 shadow-sm">
        <table class="w-full text-left text-sm" data-testid="claims-table" :aria-busy="loading">
          <caption class="sr-only">
            Declaraties. Klik op een kolomkop om te sorteren.
          </caption>
          <thead class="bg-gray-50 text-gray-700">
            <tr>
              <th scope="col" class="w-12 px-4 py-3">
                <input
                  type="checkbox"
                  class="h-4 w-4 cursor-pointer accent-blue-600"
                  name="claims-select-page"
                  :checked="allOnPageSelected"
                  aria-label="Alle declaraties op deze pagina selecteren"
                  data-testid="claims-select-page"
                  @change="toggleAllOnPage"
                />
              </th>
              <SortableColumnHeader
                v-for="column in columns"
                :key="column.field"
                :label="column.label"
                :active="sortBy === column.field"
                :descending="sortDescending"
                :align-right="column.alignRight"
                :data-testid="`claims-sort-${column.field}`"
                @sort="toggleSort(column.field)"
              />
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="claim in claims"
              :key="claim.id"
              class="border-t border-gray-200 cursor-pointer hover:bg-gray-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-blue-600"
              :class="[
                isToReview(claim) ? 'bg-amber-50 border-l-4 border-l-amber-500' : '',
                selectedIds.has(claim.id) ? 'bg-blue-50!' : '',
              ]"
              tabindex="0"
              :data-testid="`claim-row-${claim.id}`"
              @click="openClaim(claim)"
              @keydown.enter="openClaim(claim)"
            >
              <td class="px-4 py-3" @click.stop>
                <input
                  type="checkbox"
                  class="h-4 w-4 cursor-pointer accent-blue-600"
                  name="selected-claims"
                  :value="claim.id"
                  :checked="selectedIds.has(claim.id)"
                  :aria-label="`Declaratie ${claim.id} selecteren`"
                  :data-testid="`claim-select-${claim.id}`"
                  @change="toggleSelected(claim.id)"
                />
              </td>
              <td
                class="px-4 py-3 text-right tabular-nums"
                :data-testid="`claim-number-${claim.id}`"
              >
                {{ claim.id }}
              </td>
              <td class="px-4 py-3">
                <ClaimStatusBadge
                  :status="claim.status"
                  :data-testid="`claim-status-${claim.id}`"
                />
              </td>
              <td class="px-4 py-3">{{ claim.patientName }}</td>
              <td class="px-4 py-3">{{ claim.policyNumber }}</td>
              <td class="px-4 py-3 text-right tabular-nums">{{ claim.lineCount }}</td>
              <td
                class="px-4 py-3 text-right tabular-nums whitespace-nowrap"
                :data-testid="`claim-total-${claim.id}`"
              >
                {{ formatAmount(claim.totalAmount) }}
              </td>
              <td class="px-4 py-3" :data-testid="`claim-reviewed-by-${claim.id}`">
                {{ claim.reviewedByName ?? '' }}
              </td>
              <td class="px-4 py-3 whitespace-nowrap">{{ formatDate(claim.createdAt) }}</td>
              <td class="px-4 py-3 whitespace-nowrap">{{ formatDate(claim.lastModifiedAt) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <PaginationControls
        v-model:page="page"
        :total-pages="result?.totalPages ?? 0"
        :loading="loading"
        label="Paginering declaraties"
      />
    </template>
  </section>
</template>
