<script setup lang="ts">
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
  <nav id="main-navigation" class="h-full py-4 text-[#111827]">
    <ul class="flex flex-col gap-1 px-2">
      <AppNavigationItem :define-route="'Home'" :icon="Home01Icon">Home</AppNavigationItem>
      <AppNavigationItem :define-route="'Appointments'" :icon="Calendar03Icon"
        >Afspraken</AppNavigationItem
      >
      <AppNavigationItem :define-route="'MedicalFile'" :icon="MedicalFileIcon"
        >Medisch Dossier</AppNavigationItem
      >
      <AppNavigationItem :define-route="'Referrals'" :icon="FileExportIcon"
        >Verwijsbrieven</AppNavigationItem
      >
      <AppNavigationItem :define-route="'Patiënts'" :icon="UserGroupIcon"
        >Patiënten</AppNavigationItem
      >
      <AppNavigationItem :define-route="'MedicalResults'" :icon="ClipboardIcon"
        >Onderzoeksuitslagen</AppNavigationItem
      >
    </ul>
  </nav>
</template>
