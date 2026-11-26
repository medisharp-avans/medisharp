<script setup lang="ts">
// Coloured badge with the Dutch name of a claim status. Claims to review also get a warning icon.
import { HugeiconsIcon } from '@hugeicons/vue'
import { AlertCircleIcon } from '@hugeicons/core-free-icons'
import { ClaimStatus, claimStatusClasses, claimStatusLabels } from '@/types/claim'

defineProps<{ status: ClaimStatus }>()
</script>

<template>
  <span
    class="inline-flex items-center gap-1 px-2 py-1 rounded-full border text-xs font-semibold whitespace-nowrap"
    :class="claimStatusClasses[status]"
  >
    <HugeiconsIcon
      v-if="status === ClaimStatus.ToReview"
      :icon="AlertCircleIcon"
      class="w-4 h-4"
      aria-hidden="true"
    />
    {{ claimStatusLabels[status] }}
  </span>
</template>
