<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useRoute } from 'vue-router';
import AppHeader from '@/components/Layout/AppHeader.vue';
import AppMain from '@/components/Layout/AppMain.vue';
import AppNavigation from '@/components/Layout/AppNavigation.vue';

const route = useRoute()
</script>

<template>
  <header>
    <template v-if="route.meta.hideLayout">
      <AppMain>
        <RouterView />
      </AppMain>
    </template>

    <template v-else>
      <AppHeader />
      <AppNavigation />
      <AppMain>
        <RouterView />
      </AppMain>
    </template>
  </header>
</template>

