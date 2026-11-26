<script setup lang="ts">
// Previous / next buttons with "Pagina x van y". Same idea as PaginationControls in RevalliDesk.
// Hidden when there is only one page.
const props = withDefaults(
  defineProps<{
    page: number
    totalPages: number
    label?: string
    /** True while a page is loading: both buttons are disabled, so a double click does not skip a page. */
    loading?: boolean
  }>(),
  { label: 'Paginering', loading: false },
)

const emit = defineEmits<{ 'update:page': [page: number] }>()

function goTo(page: number) {
  if (page >= 1 && page <= props.totalPages) emit('update:page', page)
}

const buttonClass =
  'inline-flex items-center gap-1 rounded-xl border border-gray-200 bg-white px-4 py-2 text-gray-900 shadow-sm hover:bg-gray-100 cursor-pointer disabled:cursor-not-allowed disabled:opacity-40 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2'
</script>

<template>
  <nav
    v-if="totalPages > 1"
    :aria-label="label"
    :aria-busy="loading"
    class="mt-4 flex items-center justify-between gap-3 text-sm"
  >
    <button
      type="button"
      :class="buttonClass"
      :disabled="loading || page <= 1"
      data-testid="pagination-previous"
      @click="goTo(page - 1)"
    >
      <span aria-hidden="true">←</span> Vorige
    </button>
    <span class="text-gray-600" aria-live="polite" data-testid="pagination-status">
      Pagina {{ page }} van {{ totalPages }}
    </span>
    <button
      type="button"
      :class="buttonClass"
      :disabled="loading || page >= totalPages"
      data-testid="pagination-next"
      @click="goTo(page + 1)"
    >
      Volgende <span aria-hidden="true">→</span>
    </button>
  </nav>
</template>
