<script setup lang="ts">
// Gedeelde opmaak voor de foutpagina's (400, 401, 403, 404, 500).
// Slot "title" = kop, default slot = uitleg. Wordt binnen <AppMain> gerenderd, dus geen eigen <main>.
import { RouterLink } from 'vue-router'
</script>

<template>
  <section
    class="flex items-center justify-center min-h-screen w-full bg-gray-100 p-6 text-gray-900"
    aria-labelledby="error-title"
  >
    <div
      class="text-center max-w-md w-full p-8 bg-white rounded-2xl border border-gray-200 shadow-sm"
    >
      <h1 id="error-title" class="text-2xl font-bold text-gray-900 mb-4 leading-tight">
        <slot name="title">Er is iets misgegaan.</slot>
      </h1>

      <div class="text-base text-gray-600 mb-8 leading-relaxed">
        <slot>Er is een onverwachte fout opgetreden.</slot>
      </div>

      <div class="flex flex-row justify-center gap-4">
        <RouterLink
          :to="{ name: 'Home' }"
          class="inline-flex items-center justify-center px-5 py-3 bg-blue-600 hover:bg-blue-700 text-white font-medium text-sm rounded-xl transition-all focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2 shadow-sm"
          data-testid="return-to-home"
        >
          Terug naar Home
        </RouterLink>
        <!-- <RouterLink :to="{ name: 'Login' }" class="..." data-testid="return-to-login">Terug naar Login</RouterLink> -->
      </div>
    </div>
  </section>
</template>
