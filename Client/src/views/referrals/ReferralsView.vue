<script setup lang="ts">
// De hoofdpagina toont de ingang naar het formulier; een onderliggende route toont haar eigen pagina.
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <RouterView v-slot="{ Component }">
    <component :is="Component" v-if="Component" />
    <section v-else class="mx-auto max-w-5xl space-y-5 py-4" aria-labelledby="referrals-title">
      <h2 id="referrals-title" class="text-2xl font-semibold sm:text-3xl">Verwijsbrieven</h2>
      <p class="max-w-2xl leading-6 text-gray-600">
        Maak een verwijzing met de patiëntgegevens en de relevante medische informatie voor de
        specialist.
      </p>
      <RouterLink
        :to="{ name: 'ReferralCreate' }"
        class="inline-flex min-h-11 items-center justify-center rounded-xl bg-blue-600 px-5 py-3 text-sm font-medium text-white shadow-sm hover:bg-blue-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2"
      >
        Nieuwe verwijzing
      </RouterLink>
    </section>
  </RouterView>
</template>
