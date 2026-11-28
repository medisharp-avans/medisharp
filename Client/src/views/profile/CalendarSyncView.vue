<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useCreate } from '@/composables/api/useCreate'
import { useDelete } from '@/composables/api/useDelete'
import { useFetch } from '@/composables/api/useFetch'
import type { ConnectIcalRequest, ConnectIcalResponse, CalendarStatus } from '@/types/calendar'
import PrimaryButton from '@/components/elements/PrimaryButton.vue'

const props = defineProps<{
  specialistId: number
}>()

// Bestaat er al een koppeling? (ook na het herladen van de pagina)
const { data: status, loading: isLoadingStatus } = useFetch<CalendarStatus>(
  `api/calendar/status/${props.specialistId}`,
)

const isConnected = ref(false)
watch(
  status,
  (value) => {
    if (value) isConnected.value = value.connected
  },
  { immediate: true },
)

const {
  create: connectIcal,
  loading: isSubmitting,
  error: submitError,
} = useCreate<ConnectIcalResponse, ConnectIcalRequest>('api/calendar/connect', {
  skipErrorRedirect: true,
})

const {
  deleteItem: disconnectIcal,
  loading: isDisconnecting,
  error: disconnectError,
} = useDelete(`api/calendar/disconnect/${props.specialistId}`, {
  skipErrorRedirect: true,
})

const icalUrl = ref<string>('')
const successMessage = ref<string>('')
const errorMessage = computed(() => submitError.value ?? disconnectError.value)

async function handleConnect(): Promise<void> {
  const trimmedUrl = icalUrl.value.trim()
  if (!trimmedUrl) return

  successMessage.value = ''

  const result = await connectIcal({
    specialistId: props.specialistId,
    icalUrl: trimmedUrl,
  })

  if (!result.error && result.data) {
    successMessage.value = result.data.message || 'Agenda gekoppeld en gesynchroniseerd.'
    icalUrl.value = ''
    isConnected.value = true
  }
}

async function handleDisconnect(): Promise<void> {
  successMessage.value = ''

  const result = await disconnectIcal()

  if (!result.error) {
    successMessage.value = 'Agenda ontkoppeld en alle gegevens verwijderd.'
    isConnected.value = false
  }
}
</script>

<template>
  <div class="max-w-xl" data-testid="ical-section">
    <p class="mt-1 text-sm text-slate-700">
      Plak de geheime iCal/ICS-link van je Google-, Outlook- of Apple-agenda om je bezette momenten
      automatisch te synchroniseren.
    </p>
    <p class="mt-1 text-sm text-red-700" data-testid="ical-privacy-warning">
      Let op! Met het delen van deze iCal/ICS-link, geef je ons systeem toegang om in je
      persoonlijke agenda te kijken inclusief mogelijke persoonlijke gegevens. Het is altijd
      mogelijk om uw agenda weer te ontkoppelen en de verkregen data te verwijderen!
    </p>

    <p v-if="isLoadingStatus" class="mt-4 text-sm text-slate-700">Status ophalen...</p>

    <!-- Gekoppeld -->
    <div v-else-if="isConnected" class="mt-4">
      <p class="text-sm font-medium text-slate-900" data-testid="ical-connected">
        Je agenda is gekoppeld.
      </p>
      <div class="mt-2">
        <PrimaryButton
          type="button"
          data-testid="ical-disconnect"
          :loading="isDisconnecting"
          loading-label="Ontkoppelen..."
          @click="handleDisconnect"
        >
          Ontkoppelen
        </PrimaryButton>
      </div>
    </div>

    <!-- Niet gekoppeld -->
    <form v-else class="mt-4" @submit.prevent="handleConnect">
      <label for="ical-url" class="block text-sm font-medium text-slate-900"> iCal-link </label>

      <div class="mt-1.5 flex flex-col gap-2 sm:flex-row">
        <input
          id="ical-url"
          data-testid="ical-url"
          v-model="icalUrl"
          type="url"
          required
          :disabled="isSubmitting"
          :aria-invalid="Boolean(submitError)"
          :aria-describedby="submitError ? 'ical-error' : undefined"
          placeholder="https://google.com"
          class="min-h-11 flex-1 rounded-md border border-slate-500 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-slate-500 focus-visible:border-blue-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700 disabled:bg-slate-100"
        />

        <PrimaryButton
          data-testid="ical-submit"
          :loading="isSubmitting"
          loading-label="Koppelen..."
          :disabled="!icalUrl.trim()"
        >
          Koppelen
        </PrimaryButton>
      </div>
    </form>

    <p
      v-if="errorMessage"
      id="ical-error"
      data-testid="message"
      role="alert"
      class="mt-3 text-sm font-medium text-red-700"
    >
      {{ errorMessage }}
    </p>

    <p
      v-else-if="successMessage"
      data-testid="message"
      role="status"
      class="mt-3 text-sm font-medium text-emerald-800"
    >
      {{ successMessage }}
    </p>
  </div>
</template>
