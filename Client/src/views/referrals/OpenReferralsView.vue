<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getOpenReferrals, type OpenReferral } from '@/services/openReferrals'

const rows = ref<OpenReferral[]>([])
const page = ref(1)
const loading = ref(true)
const message = ref('')

async function load(target: number) {
  loading.value = true
  message.value = ''
  try {
    rows.value = await getOpenReferrals(target)
    page.value = target
  } catch (error) {
    rows.value = []
    message.value = error instanceof Error ? error.message : 'Ophalen is mislukt.'
  } finally {
    loading.value = false
  }
}
onMounted(() => load(1))
</script>

<template>
  <section class="mx-auto max-w-5xl space-y-4">
    <RouterLink :to="{ name: 'Referrals' }" class="text-blue-700 underline">
      Terug naar verwijsbrieven
    </RouterLink>
    <h2 class="text-2xl font-semibold">Open verwijzingen</h2>
    <p v-if="loading" role="status">Verwijzingen laden…</p>
    <p v-else-if="message" role="alert">{{ message }}</p>
    <template v-else>
      <p v-if="!rows.length">Geen open verwijzingen op deze pagina.</p>
      <div v-else class="overflow-x-auto">
        <table class="w-full text-left">
          <caption class="sr-only">
            Open verwijzingen voor afspraakplanning
          </caption>
          <thead>
            <tr class="border-b">
              <th scope="col" class="p-3">Specialisme</th>
              <th scope="col" class="p-3">Behandelcode</th>
              <th scope="col" class="p-3">Naam patiënt</th>
              <th scope="col" class="p-3">BSN</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, index) in rows" :key="index" class="border-b">
              <td class="p-3">{{ row.specialty }}</td>
              <td class="p-3">{{ row.treatmentCode }}</td>
              <td class="p-3">{{ row.patientName }}</td>
              <td class="p-3">{{ row.bsn }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="flex flex-wrap items-center gap-4">
        <button v-if="page > 1" class="text-blue-700 underline" @click="load(page - 1)">
          Vorige pagina
        </button>
        <span>Pagina {{ page }}</span>
        <button v-if="rows.length === 25" class="text-blue-700 underline" @click="load(page + 1)">
          Volgende pagina
        </button>
      </div>
    </template>
  </section>
</template>
