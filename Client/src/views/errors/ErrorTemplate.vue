<script setup lang="ts">
import { RouterLink } from 'vue-router'
</script>

<template>
  <main
    class="flex items-center justify-center min-h-screen w-full bg-[#F3F4F6] p-6 text-[#111827]"
    aria-label="Foutmelding"
    tabindex="0"
    aria-live="assertive"
    aria-atomic="true"
  >
    <div
      class="text-center max-w-md w-full p-8 bg-white rounded-2xl border border-[#E5E7EB] shadow-sm"
    >
      <h1 class="text-2xl font-bold text-[#111827] mb-4 leading-tight" name="ErrorTitle">
        <slot name="title">Er is iets misgegaan.</slot>
      </h1>

      <div class="text-base text-gray-600 mb-8 leading-relaxed">
        <slot>Er is een onverwachte fout opgetreden.</slot>
      </div>

      <div class="flex flex-row justify-center gap-4">
        <RouterLink
          :to="{ name: 'Home' }"
          class="inline-flex items-center justify-center px-5 py-3 bg-blue-600 hover:bg-blue-700 text-white font-medium text-sm rounded-xl transition-all focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 shadow-sm"
          name="ReturnToHomeBtn"
        >
          Terug naar Home
        </RouterLink>
        <!-- <RouterLink :to="{ name: 'Login' }" class="..." name="returnToLoginBtn">Terug naar Login</RouterLink> -->
      </div>
    </div>
  </main>
</template>
