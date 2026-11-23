<script setup>
import { ref } from 'vue'
import axios from 'axios'
import { useCreate } from '@/composables/api/useCreate'
import { useFetch } from '@/composables/api/useFetch'

const { create, loading } = useCreate(
  '/api/appointments',
  { skipErrorRedirect: true },
)

const { data } = useFetch('api/Patients/search/patients')
console.log(data)

const form = ref({
  patientId: '',
  specialistId: '',
  startTime: '',
  generalPractitionerId: '',
  specialism: '',
  preferredSpecialistId: '',
  reason: '',
  questionnaire: '',
  urgency: 'Regulier',
  treatmentCode: '',
})

const message = ref(null)

async function submit() {
  message.value = null

  const request = {
    patientId: Number(form.value.patientId),
    specialistId: Number(form.value.specialistId),
    startTime: form.value.startTime,
    generalPractitionerId: Number(form.value.generalPractitionerId),
    specialism: Number(form.value.specialism),
    preferredSpecialistId: form.value.preferredSpecialistId
      ? Number(form.value.preferredSpecialistId)
      : null,
    reason: form.value.reason,
    questionnaire: form.value.questionnaire || null,
    urgency: form.value.urgency,
    treatmentCode: form.value.treatmentCode,
  }

  const result = await create(request)

  if (result.error) {
    const error = result.error

    message.value = {
      type: 'error',
      text: axios.isAxiosError(error) && typeof error.response?.data === 'string'
        ? error.response.data
        : 'Er is iets misgegaan bij het opslaan van de afspraak.',
    }

    return
  }

  message.value = {
    type: 'success',
    text: 'De afspraak is succesvol aangemaakt.',
  }

  form.value = {
    patientId: '',
    specialistId: '',
    startTime: '',
    generalPractitionerId: '',
    specialism: '',
    preferredSpecialistId: '',
    reason: '',
    questionnaire: '',
    urgency: 'Regulier',
    treatmentCode: '',
  }
}
</script>

<template>
  <div class="mx-auto max-w-3xl p-6">
    <h1 class="mb-6 text-2xl font-semibold">
      Afspraak aanmaken
    </h1>

    <form @submit.prevent="submit" class="space-y-5">

      <div>
        <label for="patientId" class="block text-sm font-medium">
          Patiënt ID
        </label>
        <input
          id="patientId"
          v-model="form.patientId"
          data-testid="patient-id"
          type="number"
          min="1"
          required
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <div>
        <label for="specialistId" class="block text-sm font-medium">
          Specialist ID
        </label>
        <input
          id="specialistId"
          v-model="form.specialistId"
          data-testid="specialist-id"
          type="number"
          min="1"
          required
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <div>
        <label for="startTime" class="block text-sm font-medium">
          Datum en tijd
        </label>
        <input
          id="startTime"
          v-model="form.startTime"
          data-testid="start-time"
          type="datetime-local"
          required
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <div>
        <label for="generalPractitionerId" class="block text-sm font-medium">
          Huisarts ID
        </label>
        <input
          id="generalPractitionerId"
          v-model="form.generalPractitionerId"
          data-testid="general-practitioner-id"
          type="number"
          min="1"
          required
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <div>
        <label for="specialism" class="block text-sm font-medium">
          Specialisme (enumwaarde)
        </label>
        <input
          id="specialism"
          v-model="form.specialism"
          data-testid="specialism"
          type="number"
          min="0"
          required
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <div>
        <label for="preferredSpecialistId" class="block text-sm font-medium">
          Voorkeurspecialist ID (optioneel)
        </label>
        <input
          id="preferredSpecialistId"
          v-model="form.preferredSpecialistId"
          data-testid="preferred-specialist-id"
          type="number"
          min="1"
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <div>
        <label for="reason" class="block text-sm font-medium">
          Reden
        </label>
        <textarea
          id="reason"
          v-model="form.reason"
          data-testid="reason"
          maxlength="500"
          required
          rows="3"
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <div>
        <label for="questionnaire" class="block text-sm font-medium">
          Vragenlijst (optioneel)
        </label>
        <textarea
          id="questionnaire"
          v-model="form.questionnaire"
          data-testid="questionnaire"
          rows="3"
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <div>
        <label for="urgency" class="block text-sm font-medium">
          Urgentie
        </label>
        <select
          id="urgency"
          v-model="form.urgency"
          data-testid="urgency"
          required
          class="mt-1 w-full rounded-md border px-3 py-2"
        >
          <option value="Regulier">Regulier</option>
          <option value="Urgent">Urgent</option>
          <option value="Spoed">Spoed</option>
        </select>
      </div>

      <div>
        <label for="treatmentCode" class="block text-sm font-medium">
          Behandelcode
        </label>
        <input
          id="treatmentCode"
          v-model="form.treatmentCode"
          data-testid="treatment-code"
          type="text"
          required
          class="mt-1 w-full rounded-md border px-3 py-2"
        />
      </div>

      <p
        v-if="message"
        data-testid="message"
        :role="message.type === 'error' ? 'alert' : 'status'"
        :class="message.type === 'error'
          ? 'rounded-md bg-red-50 p-3 text-red-800'
          : 'rounded-md bg-green-50 p-3 text-green-800'"
      >
        {{ message.text }}
      </p>

      <button
        type="submit"
        data-testid="submit"
        :disabled="loading"
        class="rounded-md bg-blue-600 px-5 py-2 font-medium text-white hover:bg-blue-700 disabled:opacity-50"
      >
        {{ loading ? 'Bezig...' : 'Afspraak aanmaken' }}
      </button>

    </form>
  </div>
</template>
