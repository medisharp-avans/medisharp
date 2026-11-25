<script setup lang="ts">
// Zijnavigatie met de hoofdpagina's van de applicatie.
import AppNavigationItem from '@/components/Layout/AppNavigationItem.vue'
import {
  Home01Icon,
  Calendar03Icon,
  MedicalFileIcon,
  FileExportIcon,
  UserGroupIcon,
  ClipboardIcon,
} from '@hugeicons/core-free-icons'
</script>

<template>
  <nav id="main-navigation" aria-label="Hoofdnavigatie" class="h-full py-4 text-gray-900">
    <ul class="flex flex-col gap-1 px-2">
      <AppNavigationItem route-name="Home" :icon="Home01Icon">Home</AppNavigationItem>
      <AppNavigationItem route-name="Appointments" :icon="Calendar03Icon">
        Afspraken
      </AppNavigationItem>
      <AppNavigationItem route-name="NewAppointment" :icon="Calendar03Icon">
        Afspraak aanmaken
      </AppNavigationItem>
      <AppNavigationItem route-name="MedicalFile" :icon="MedicalFileIcon">
        Medisch Dossier
      </AppNavigationItem>
      <AppNavigationItem route-name="Referrals" :icon="FileExportIcon">
        Verwijsbrieven
      </AppNavigationItem>
      <AppNavigationItem route-name="Patients" :icon="UserGroupIcon">Patiënten</AppNavigationItem>
      <AppNavigationItem route-name="MedicalResults" :icon="ClipboardIcon">
        Onderzoeksuitslagen
      </AppNavigationItem>
    </ul>
  </nav>
</template>
