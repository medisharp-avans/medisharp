<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useRoute } from 'vue-router';
import AppHeader from '@/components/Layout/AppHeader.vue';
import AppMain from '@/components/Layout/AppMain.vue';
import AppNavigation from '@/components/Layout/AppNavigation.vue';

const route = useRoute()
</script>

<template>
  <div v-if="route.meta.hideLayout" class="min-h-screen bg-white">
    <AppMain>
      <RouterView />
    </AppMain>
  </div>

  <div v-else class="min-h-screen flex flex-col bg-white">
    <AppHeader />
    
    <div class="flex flex-1">
      <aside class="w-1/4 min-w-62.5 max-w-[25%] bg-[#F3F4F6] border-r border-[#E5E7EB]">
        <AppNavigation />
      </aside>

      
        <AppMain>
          <RouterView />
        </AppMain>
      
    </div>
  </div>
</template>

