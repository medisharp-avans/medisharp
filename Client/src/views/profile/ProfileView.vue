<script lang="ts" setup>
import ContentCard from '@/components/Layout/ContentCard.vue'
import CalendarSyncView from './CalendarSyncView.vue'
</script>

<template>
  <div class="mx-auto w-full max-w-2xl px-6 py-8">
    <ContentCard>
      <template #title>Profiel</template>
      <template #description
        >Beheer hier je persoonlijke instellingen en profiel informatie</template
      >
    </ContentCard>

    <div class="mt-6">
      <ContentCard>
        <template #title>Agenda koppeling</template>
        <template #description>Koppel je persoonlijke agenda aan ons systeem</template>
        <CalendarSyncView :specialist-id="1" />
      </ContentCard>
    </div>
  </div>
</template>
