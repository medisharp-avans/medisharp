<script setup lang="ts">
import DatalistField from '@/components/forms/DataListField.vue'
import SelectField from '@/components/forms/SelectField.vue'
import TextAreaField from '@/components/forms/TextAreaField.vue'
import FormMessage from '@/components/forms/FormMessage.vue'
import StepForm from '@/components/forms/StepForm.vue'
import SummaryList from '@/components/forms/SummaryList.vue'
import { computed, ref, watch } from 'vue'
import { useCreate } from '@/composables/api/useCreate'
import { useFetch } from '@/composables/api/useFetch'
import { endpoints } from '@/services/endpoints'
import { useLookup } from '@/composables/forms/useLookup'
import { useFormMessage } from '@/composables/forms/useFormMessage'
import { enumOptions } from '@/utils/enumOptions'
import { Specialism, type Patient, type Specialist, type TreatmentCode } from '@/types/appointment'

const steps = [
  {
    id: 'patient',
    title: 'Patiënt',
    description: 'Voor wie is de afspraak en in welk specialisme?',
  },
  {
    id: 'care',
    title: 'Behandeling',
    description: 'Kies de behandelcode en eventuele voorkeurspecialist.',
  },
  { id: 'details', title: 'Details', description: 'Geef aan waarom de afspraak nodig is.' },
  {
    id: 'review',
    title: 'Controleren',
    description: 'Controleer de gegevens en maak de afspraak aan.',
  },
]

const step = ref(0)

const emptyForm = () => ({
  specialism: '' as number | '',
  reason: '',
  questionnaire: '',
  urgency: 'Regulier',
})

const form = ref(emptyForm())
const { message, clear: clearMessage, success, error: showError, fail } = useFormMessage()

const specialismOptions = enumOptions(Specialism)
const urgencyOptions = ['Regulier', 'Urgent', 'Spoed'].map((value) => ({ value, label: value }))

const specialismName = computed(() =>
  form.value.specialism === '' ? undefined : Specialism[form.value.specialism],
)

const params = () => ({ specialism: specialismName.value })
const hasSpecialism = () => specialismName.value !== undefined

const { data: specialists } = useFetch<Specialist[]>(
  endpoints.planning.specialists,
  params,
  hasSpecialism,
)
const { data: treatmentCodes } = useFetch<TreatmentCode[]>(
  endpoints.planning.treatmentCodes,
  params,
  hasSpecialism,
)
const { data: patients } = useFetch<Patient[]>(endpoints.planning.patients)
const { create, loading } = useCreate(endpoints.appointments, { skipErrorRedirect: true })

// Zoekvelden: getypte tekst, opties voor de lijst en het gekozen item (voor het ID)
const {
  text: patientText,
  options: patientOptions,
  selected: patient,
} = useLookup(
  patients,
  (p) => p.patientId,
  (p) => p.patientFullName,
)

const {
  text: specialistText,
  options: specialistOptions,
  selected: specialist,
} = useLookup(
  specialists,
  (s) => s.specialistId,
  (s) => s.specialistFullName,
)

const {
  text: codeText,
  options: codeOptions,
  selected: treatmentCode,
} = useLookup(
  treatmentCodes,
  (c) => c.code,
  (c) => c.code,
  (c) => c.description,
)

watch(
  () => form.value.specialism,
  () => {
    specialistText.value = ''
    codeText.value = ''
  },
)

// Overzicht voor de laatste stap
const summaryItems = computed(() => [
  { testid: 'patient', label: 'Patiënt', value: patientText.value, step: 0 },
  { testid: 'specialism', label: 'Specialisme', value: specialismName.value ?? '', step: 0 },
  {
    testid: 'specialist',
    label: 'Voorkeursspecialist',
    value: specialistText.value || 'Geen voorkeur',
    step: 1,
  },
  {
    testid: 'treatment-code',
    label: 'Behandelcode',
    value: treatmentCode.value
      ? `${treatmentCode.value.code} – ${treatmentCode.value.description}`
      : codeText.value,
    step: 1,
  },
  { testid: 'reason', label: 'Reden', value: form.value.reason, step: 2 },
  {
    testid: 'questionnaire',
    label: 'Vragenlijst',
    value: form.value.questionnaire || 'Geen',
    step: 2,
  },
  { testid: 'urgency', label: 'Urgentie', value: form.value.urgency, step: 2 },
])

async function submit() {
  clearMessage()

  if (!patient.value || !treatmentCode.value || form.value.specialism === '') {
    showError('Kies een patiënt, specialisme en behandelcode uit de lijst.')
    return
  }

  const result = await create({
    patientId: patient.value.patientId,
    specialism: Number(form.value.specialism),
    preferredSpecialistId: specialist.value?.specialistId ?? null,
    reason: form.value.reason,
    questionnaire: form.value.questionnaire || null,
    urgency: form.value.urgency,
    treatmentCode: treatmentCode.value.code,
  })

  if (result.error) {
    fail(result.error, 'Er is iets misgegaan bij het opslaan van de afspraak.')
    return
  }

  success('De afspraak is succesvol aangemaakt.')

  form.value = emptyForm()
  patientText.value = ''
  specialistText.value = ''
  codeText.value = ''
  step.value = 0
}
</script>

<template>
  <div class="mx-auto max-w-3xl p-6">
    <h1 class="mb-2 text-2xl font-semibold">Afspraak aanmaken</h1>
    <p class="mb-6 text-sm text-gray-600">Velden zonder "optioneel" zijn verplicht.</p>

    <FormMessage :message="message" class="mb-6" />

    <StepForm
      v-model="step"
      :steps="steps"
      :loading="loading"
      submit-label="Afspraak aanmaken"
      @submit="submit"
    >
      <!-- Stap 1: patiënt & specialisme -->
      <template #step-patient>
        <DatalistField
          id="patientId"
          v-model="patientText"
          label="Patiënt"
          hint="Typ een naam en kies een patiënt uit de lijst."
          :options="patientOptions"
          placeholder="Zoek op naam…"
          data-testid="patient-id"
          required
        />

        <SelectField
          id="specialism"
          v-model="form.specialism"
          label="Specialisme"
          :options="specialismOptions"
          placeholder="Kies een specialisme"
          data-testid="specialism"
          required
        />
      </template>

      <!-- Stap 2: behandeling -->
      <template #step-care>
        <DatalistField
          id="specialistId"
          v-model="specialistText"
          label="Voorkeursspecialist"
          hint="Laat leeg als er geen voorkeur is."
          :options="specialistOptions"
          :disabled="form.specialism === ''"
          :placeholder="form.specialism === '' ? 'Kies eerst een specialisme' : 'Zoek op naam…'"
          data-testid="specialist-id"
          optional
        />

        <DatalistField
          id="treatmentCode"
          v-model="codeText"
          label="Behandelcode"
          :options="codeOptions"
          :disabled="form.specialism === ''"
          :placeholder="form.specialism === '' ? 'Kies eerst een specialisme' : 'Zoek op code…'"
          data-testid="treatment-code"
          required
        />
      </template>

      <!-- Stap 3: details -->
      <template #step-details>
        <TextAreaField
          id="reason"
          v-model="form.reason"
          label="Reden"
          hint="Maximaal 500 tekens."
          :maxlength="500"
          data-testid="reason"
          required
        />

        <TextAreaField
          id="questionnaire"
          v-model="form.questionnaire"
          label="Vragenlijst"
          data-testid="questionnaire"
          optional
        />

        <SelectField
          id="urgency"
          v-model="form.urgency"
          label="Urgentie"
          :options="urgencyOptions"
          data-testid="urgency"
          required
        />
      </template>

      <!-- Stap 4: controleren -->
      <template #step-review>
        <SummaryList :items="summaryItems" @edit="step = $event" />
      </template>
    </StepForm>
  </div>
</template>
