<script setup lang="ts">
import IconProfile from '@/components/icons/IconProfile.vue'
import IconLogo from '@/components/icons/IconLogo.vue'
import { RouterLink } from 'vue-router'
</script>
<template>
  <header
    class="flex justify-between items-center px-6 py-4 bg-[#F3F4F6] border-b border-[#E5E7EB] text-[#111827]"
  >
    <div class="flex items-center gap-3">
      <IconLogo class="w-10 h-10 text-[#111827]" />
      <h1 class="text-lg font-semibold">De Zachte Heelmeesters</h1>
    </div>

    <div>
      <RouterLink
        :to="{ name: 'Profile' }"
        class="flex items-center justify-center w-12 h-12 rounded-full hover:bg-[#E5E7EB] transition-colors cursor-pointer"
        aria-label="Profiel"
        id="profile-button"
      >
        <IconProfile class="w-8 h-8 text-[#111827]" />
      </RouterLink>
    </div>
  </header>
</template>
