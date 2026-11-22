<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import api from '@/services/api'
import { useCreate } from '@/composables/api/useCreate'
import {
  Specialism,
  type AppointmentManualRequest,
  type AppointmentResponse,
  type PatientDetail,
  type TimeSlot,
  type TreatmentCodeOption,
} from '@/types/appointment'

/**
 * Plant een afspraak voor een patiënt: /afspraken?patientId=2
 * De verwijzing hoeft niet bestaan: de API maakt er bij het boeken automatisch een aan.
 */
const route = useRoute()
const toId = (value: unknown): number | null => {
  const n = Number(Array.isArray(value) ? value[0] : value)
  return Number.isInteger(n) && n > 0 ? n : null
}
const patientId = toId(route.query.patientId)

// skipErrorRedirect: een 409 (bezet tijdslot) hoort als melding op deze pagina te blijven
const { create, loading: booking } = useCreate<AppointmentResponse, AppointmentManualRequest>(
  '/api/appointments',
  { skipErrorRedirect: true },
)

const patient = ref<PatientDetail | null>(null)
const specialism = ref<Specialism>(Specialism.Algemeen)
const treatmentCode = ref('')
const treatmentCodes = ref<TreatmentCodeOption[]>([])
const reason = ref('Afspraak gepland door medewerker')

const fromDate = ref(new Date().toLocaleDateString('sv-SE')) // yyyy-MM-dd, lokale datum
const slots = ref<TimeSlot[]>([])
const slotsLoaded = ref(false)
const selected = ref<TimeSlot | null>(null)
const message = ref<{ kind: 'success' | 'error'; text: string } | null>(null)

const specialismOptions = Object.entries(Specialism)
  .filter(([, value]) => typeof value === 'number')
  .map(([label, value]) => ({ label, value: value as Specialism }))

const slotsBySpecialist = computed(() => {
  const groups = new Map<number, { specialistId: number; name: string; slots: TimeSlot[] }>()
  for (const slot of slots.value) {
    const group = groups.get(slot.specialistId) ?? {
      specialistId: slot.specialistId,
      name: slot.specialistName,
      slots: [],
    }
    group.slots.push(slot)
    groups.set(slot.specialistId, group)
  }
  return [...groups.values()]
})

const startKey = (slot: TimeSlot) => slot.startTime.slice(0, 16) // 2027-01-12T10:00
const slotLabel = (slot: TimeSlot) =>
  new Date(slot.startTime).toLocaleString('nl-NL', {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  })

onMounted(async () => {
  // Een onbekende patiënt geeft een 404; de interceptor stuurt dan naar de foutpagina
  if (patientId === null) return
  const { data } = await api.get<PatientDetail>(`/api/patients/${patientId}`)
  patient.value = data
  await loadTreatmentCodes()
})

async function loadTreatmentCodes() {
  const { data } = await api.get<TreatmentCodeOption[]>('/api/treatmentcodes', {
    params: { specialism: Specialism[specialism.value] },
  })
  treatmentCodes.value = data
  treatmentCode.value = data[0]?.code ?? ''
  slots.value = []
  slotsLoaded.value = false
  selected.value = null
}

// Een ander specialisme geeft andere behandelcodes
watch(specialism, () => {
  if (patient.value) loadTreatmentCodes()
})

async function loadSlots() {
  slotsLoaded.value = false
  selected.value = null
  const { data } = await api.get<TimeSlot[]>('/api/timeslots', {
    params: {
      specialism: Specialism[specialism.value],
      treatmentCode: treatmentCode.value,
      from: fromDate.value,
      days: 14,
    },
  })
  slots.value = data
  slotsLoaded.value = true
}

/** De controller geeft bij een conflict een tekst terug (Conflict(ex.Message)); die tonen we direct. */
function errorText(err: unknown): string {
  if (axios.isAxiosError(err) && typeof err.response?.data === 'string') return err.response.data
  return 'Er is iets misgegaan bij het opslaan.'
}

async function confirm() {
  if (!selected.value || !patient.value) return
  const slot = selected.value
  message.value = null

  // De API vereist een huisarts bij een afspraak
  if (patient.value.generalPractitionerId === null) {
    message.value = { kind: 'error', text: 'Deze patiënt heeft geen huisarts.' }
    return
  }

  const result = await create({
    patientId: patient.value.id,
    specialistId: slot.specialistId,
    startTime: slot.startTime,
    generalPractitionerId: patient.value.generalPractitionerId,
    specialism: specialism.value,
    preferredSpecialistId: slot.specialistId,
    reason: reason.value,
    questionnaire: null,
    urgency: 'Regulier',
    treatmentCode: treatmentCode.value,
  })

  message.value = result.error
    ? { kind: 'error', text: errorText(result.error) }
    : { kind: 'success', text: `Afspraak ingepland bij ${slot.specialistName} op ${slotLabel(slot)}.` }

  // Altijd verversen: na een conflict verdwijnt het bezette slot, na succes ook
  await loadSlots()
}

const inputClass = 'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2'
const labelClass = 'block text-sm font-medium text-gray-700'
</script>

<template>
  <main class="mx-auto max-w-3xl p-6">
    <h2 class="mb-6 text-2xl font-semibold">Afspraak plannen</h2>

    <p v-if="patientId === null" data-testid="no-context" class="text-gray-600">
      Open deze pagina vanuit een patiënt.
    </p>

    <template v-else-if="patient">
      <p class="mb-6">Patiënt: <strong data-testid="patient-name">{{ patient.name }}</strong></p>

      <section class="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <label for="specialism" :class="labelClass">Specialisme</label>
          <select id="specialism" v-model.number="specialism" data-testid="specialism-select" :class="inputClass">
            <option v-for="option in specialismOptions" :key="option.value" :value="option.value">
              {{ option.label }}
            </option>
          </select>
        </div>

        <div>
          <label for="treatment-code" :class="labelClass">Behandelcode</label>
          <select id="treatment-code" v-model="treatmentCode" data-testid="treatment-code-select" :class="inputClass">
            <option v-for="code in treatmentCodes" :key="code.code" :value="code.code">
              {{ code.code }} – {{ code.description }}
            </option>
          </select>
        </div>

        <div class="sm:col-span-2">
          <label for="reason" :class="labelClass">Reden</label>
          <textarea id="reason" v-model="reason" data-testid="reason" rows="2" maxlength="500" :class="inputClass" />
        </div>
      </section>

      <section class="mb-6 flex items-end gap-4">
        <div>
          <label for="from-date" :class="labelClass">Tijdsloten vanaf</label>
          <input id="from-date" v-model="fromDate" data-testid="from-date" type="date" :class="inputClass" />
        </div>
        <button
          type="button"
          data-testid="show-slots"
          :disabled="!treatmentCode"
          class="rounded-md bg-gray-800 px-4 py-2 font-medium text-white hover:bg-gray-900 disabled:opacity-50"
          @click="loadSlots"
        >
          Toon tijdsloten
        </button>
      </section>

      <p
        v-if="message"
        data-testid="message"
        :role="message.kind === 'error' ? 'alert' : 'status'"
        :class="['mb-6 rounded-md px-4 py-3 text-sm', message.kind === 'error' ? 'bg-red-50 text-red-800' : 'bg-green-50 text-green-800']"
      >
        {{ message.text }}
      </p>

      <section data-testid="slots" :data-loaded="slotsLoaded ? 'true' : 'false'">
        <p v-if="slotsLoaded && slots.length === 0" data-testid="no-slots" class="text-gray-600">
          Geen tijdsloten beschikbaar in deze periode.
        </p>

        <div
          v-for="group in slotsBySpecialist"
          :key="group.specialistId"
          data-testid="specialist-slots"
          :data-specialist="group.name"
          class="mb-6"
        >
          <h3 class="mb-2 font-semibold">{{ group.name }}</h3>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="slot in group.slots"
              :key="slot.startTime"
              type="button"
              data-testid="slot"
              :data-start="startKey(slot)"
              :aria-pressed="selected === slot"
              :class="[
                'rounded-md border px-3 py-1 text-sm',
                selected === slot ? 'border-blue-600 bg-blue-600 text-white' : 'border-gray-300 hover:bg-gray-100',
              ]"
              @click="selected = slot"
            >
              {{ slotLabel(slot) }}
            </button>
          </div>
        </div>
      </section>

      <button
        v-if="slotsLoaded"
        type="button"
        data-testid="confirm"
        :disabled="!selected || booking"
        class="rounded-md bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        @click="confirm"
      >
        {{ booking ? 'Bezig…' : 'Bevestig afspraak' }}
      </button>
    </template>
  </main>
</template>