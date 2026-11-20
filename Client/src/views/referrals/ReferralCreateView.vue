<script setup lang="ts">
// Formulierbasis voor AFS-01a. Invoer bestaat alleen zolang deze pagina geopend is.
// Opslaan en de afhandeling van veldfouten volgen afzonderlijk.
import { reactive, ref, watch, onMounted, onUnmounted } from 'vue'
import { getSpecialties, getTreatments, type TreatmentChoice } from '@/services/treatmentCatalog'
import { RouterLink } from 'vue-router'
import BaseButton from '@/components/elements/BaseButton.vue'
import ReferralFormField from '@/components/referrals/ReferralFormField.vue'

const draft = reactive({
  specialty: '',
  treatmentCode: '',
  bsn: '',
  patientName: '',
  patientAddress: '',
  patientCity: '',
  patientPhoneNumber: '',
  patientEmailAddress: '',
  reasonForReferral: '',
  clinicalQuestion: '',
  medicalInformation: '',
  medication: '',
  allergies: '',
  urgency: 'Regular',
})

const specialties = ref<string[]>([])
const loadingSpecialties = ref(false)
const specialtyError = ref('')
const controller = new AbortController()

async function loadSpecialties() {
  loadingSpecialties.value = true
  specialtyError.value = ''
  try {
    specialties.value = await getSpecialties(controller.signal)
  } catch {
    if (!controller.signal.aborted) {
      specialtyError.value = 'De specialismen konden niet worden geladen. Probeer het opnieuw.'
    }
  } finally {
    loadingSpecialties.value = false
  }
}
onMounted(loadSpecialties)
const treatments = ref<TreatmentChoice[]>([])
const loadingTreatments = ref(false)
const treatmentError = ref('')
let treatmentController: AbortController | undefined

// Annuleer oudere aanvragen zodat een trage respons nooit een nieuwere keuze overschrijft.
async function loadTreatments() {
  treatmentController?.abort()
  draft.treatmentCode = ''
  treatments.value = []
  treatmentError.value = ''
  loadingTreatments.value = false
  if (!draft.specialty) return
  const request = new AbortController()
  treatmentController = request
  loadingTreatments.value = true
  try {
    const result = await getTreatments(draft.specialty, request.signal)
    if (!request.signal.aborted) treatments.value = result
  } catch {
    if (!request.signal.aborted) {
      treatmentError.value = 'De behandelingen konden niet worden geladen. Probeer het opnieuw.'
    }
  } finally {
    if (!request.signal.aborted) loadingTreatments.value = false
  }
}
watch(() => draft.specialty, loadTreatments, { flush: 'sync' })
onUnmounted(() => {
  controller.abort()
  treatmentController?.abort()
})

const fieldsetClasses = 'min-w-0 rounded-2xl border border-gray-200 bg-white p-4 sm:p-6'
const selectClasses =
  'block w-full min-w-0 rounded-xl border border-gray-500 bg-white px-3 py-2.5 text-base leading-6 text-gray-900 shadow-sm focus:border-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-600 disabled:cursor-not-allowed disabled:border-gray-200 disabled:bg-gray-100 disabled:text-gray-600'
</script>

<template>
  <section class="mx-auto max-w-5xl space-y-6 pb-8" aria-labelledby="referral-create-title">
    <div class="space-y-4">
      <RouterLink
        :to="{ name: 'Referrals' }"
        class="inline-flex min-h-11 items-center gap-2 rounded-lg text-sm font-medium text-blue-700 hover:underline focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2"
      >
        <span aria-hidden="true">←</span>
        Terug naar verwijsbrieven
      </RouterLink>
      <div class="flex flex-wrap items-center gap-3">
        <h2 id="referral-create-title" class="text-2xl leading-tight font-semibold sm:text-3xl">
          Nieuwe verwijzing
        </h2>
        <span class="rounded-full bg-blue-50 px-3 py-1 text-sm font-medium text-blue-800">
          In voorbereiding
        </span>
      </div>
      <p class="max-w-2xl text-base leading-6 text-gray-600">
        Vul de gegevens in die de specialist nodig heeft om de verwijzing te beoordelen. Het
        ziekenhuis plant de afspraak.
      </p>
      <div class="rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-900">
        Dit formulier slaat nog geen gegevens op. Gebruik alleen fictieve patiëntgegevens.
      </div>
    </div>

    <!-- Voorkom ook bij Enter dat de browser gegevens via een formulier-URL verstuurt. -->
    <form class="space-y-6" autocomplete="off" @submit.prevent>
      <p class="text-sm text-gray-600">Alle invulvelden zijn verplicht.</p>

      <fieldset :class="fieldsetClasses">
        <legend class="px-2 text-lg font-semibold">Patiëntgegevens</legend>
        <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <ReferralFormField
            id="patient-name"
            v-model="draft.patientName"
            label="Naam patiënt"
            :maxlength="200"
          />
          <ReferralFormField
            id="patient-bsn"
            v-model="draft.bsn"
            label="BSN"
            inputmode="numeric"
            :maxlength="9"
            hint="Neem alle cijfers over, inclusief eventuele nullen aan het begin."
          />
          <ReferralFormField
            id="patient-address"
            v-model="draft.patientAddress"
            label="Adres en huisnummer"
            :maxlength="300"
          />
          <ReferralFormField
            id="patient-city"
            v-model="draft.patientCity"
            label="Woonplaats"
            :maxlength="100"
          />
          <ReferralFormField
            id="patient-phone"
            v-model="draft.patientPhoneNumber"
            label="Telefoonnummer"
            type="tel"
            :maxlength="32"
          />
          <ReferralFormField
            id="patient-email"
            v-model="draft.patientEmailAddress"
            label="E-mailadres"
            type="email"
            :maxlength="254"
          />
        </div>
      </fieldset>

      <fieldset :class="fieldsetClasses">
        <legend class="px-2 text-lg font-semibold">Verwijzing</legend>
        <div class="space-y-5">
          <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <div class="min-w-0">
              <label for="referral-specialty" class="mb-2 block text-sm font-medium"
                >Specialisme</label
              >
              <select
                id="referral-specialty"
                v-model="draft.specialty"
                :class="selectClasses"
                aria-describedby="specialty-status"
                :disabled="loadingSpecialties || specialties.length === 0"
                required
              >
                <option value="" disabled>Specialisme kiezen</option>
                <option v-for="specialty in specialties" :key="specialty" :value="specialty">
                  {{ specialty }}
                </option>
              </select>
            </div>
            <div class="min-w-0">
              <label for="referral-treatment" class="mb-2 block text-sm font-medium">
                Behandeling en behandelcode
              </label>
              <select
                id="referral-treatment"
                v-model="draft.treatmentCode"
                :class="selectClasses"
                aria-describedby="treatment-choice-hint"
                :disabled="loadingTreatments || treatments.length === 0"
                required
              >
                <option value="" disabled>Behandeling kiezen</option>
                <option
                  v-for="treatment in treatments"
                  :key="treatment.code"
                  :value="treatment.code"
                >
                  {{ treatment.description }} — {{ treatment.code }}
                </option>
              </select>
            </div>
          </div>
          <div id="specialty-status" role="status" class="text-sm text-gray-700">
            <p v-if="loadingSpecialties">Specialismen laden…</p>
            <template v-else-if="specialtyError">
              <p>{{ specialtyError }}</p>
              <BaseButton class="mt-2" variant="secondary" @click="loadSpecialties"
                >Opnieuw proberen</BaseButton
              >
            </template>
            <p v-else-if="specialties.length === 0">
              Er zijn momenteel geen specialismen beschikbaar.
            </p>
          </div>
          <div id="treatment-choice-hint" role="status" class="text-sm text-gray-600">
            <p v-if="!draft.specialty">Kies eerst een specialisme.</p>
            <p v-else-if="loadingTreatments">Behandelingen laden…</p>
            <template v-else-if="treatmentError">
              <p>{{ treatmentError }}</p>
              <BaseButton variant="secondary" class="mt-2" @click="loadTreatments"
                >Opnieuw proberen</BaseButton
              >
            </template>
            <p v-else-if="treatments.length === 0">
              Er zijn geen behandelingen beschikbaar voor dit specialisme.
            </p>
          </div>
          <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <ReferralFormField
              id="referral-reason"
              v-model="draft.reasonForReferral"
              label="Reden van verwijzing"
              multiline
            />
            <ReferralFormField
              id="referral-question"
              v-model="draft.clinicalQuestion"
              label="Vraagstelling aan de specialist"
              multiline
            />
            <div>
              <label for="referral-urgency" class="mb-2 block text-sm font-medium">Urgentie</label>
              <select
                id="referral-urgency"
                v-model="draft.urgency"
                name="urgency"
                :class="selectClasses"
                required
              >
                <option value="Regular">Regulier</option>
              </select>
            </div>
          </div>
        </div>
      </fieldset>

      <fieldset :class="fieldsetClasses">
        <legend class="px-2 text-lg font-semibold">Medische informatie</legend>
        <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <ReferralFormField
            id="referral-medical-information"
            v-model="draft.medicalInformation"
            class="sm:col-span-2"
            label="Relevante medische gegevens"
            hint="Neem alleen gegevens op die voor deze verwijzing relevant zijn."
            multiline
            :rows="4"
          />
          <ReferralFormField
            id="referral-medication"
            v-model="draft.medication"
            label="Medicatie"
            hint="Vul ‘Geen’ in als de patiënt geen medicatie gebruikt."
            multiline
          />
          <ReferralFormField
            id="referral-allergies"
            v-model="draft.allergies"
            label="Allergieën"
            hint="Vul ‘Geen bekend’ in als er geen allergieën bekend zijn."
            multiline
          />
        </div>
      </fieldset>

      <div
        class="flex flex-col gap-4 border-t border-gray-200 pt-5 sm:flex-row sm:items-center sm:justify-end"
      >
        <RouterLink
          :to="{ name: 'Referrals' }"
          class="inline-flex min-h-11 items-center justify-center rounded-xl border border-gray-200 bg-white px-5 py-3 text-sm font-medium hover:bg-gray-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-gray-500 focus-visible:ring-offset-2"
        >
          Annuleren
        </RouterLink>
        <BaseButton type="submit" disabled>Verwijzing opslaan</BaseButton>
      </div>
    </form>
  </section>
</template>
