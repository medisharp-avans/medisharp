<script setup lang="ts">
    import AppNavigationItem from '@/components/Layout/AppNavigationItem.vue';
    import IconHome from '@/components/icons/IconHome.vue';
    import IconCalendar from '@/components/icons/IconCalendar.vue';
    import IconFolder from '@/components/icons/IconFolder.vue';
    import IconDocumentArrow from '@/components/icons/IconDocumentArrow.vue';
    import IconUsers from '@/components/icons/IconUsers.vue';
    import IconClipboard from '@/components/icons/IconClipboard.vue';
    import { markRaw } from 'vue';

    const MyFolder = markRaw(IconFolder);
    const MyClipboard = markRaw(IconClipboard);
    const MyUsers = markRaw(IconUsers);
    const MyDocumentArrow = markRaw(IconDocumentArrow);
    const MyCalendar = markRaw(IconCalendar);
    const MyHome = markRaw(IconHome);
</script>

<template>
    <nav class="h-full py-4 text-[#111827]">
        <ul class="flex flex-col gap-1 px-2">
            <AppNavigationItem :define-route="'Home'" :icon="MyHome">Home</AppNavigationItem>
            <AppNavigationItem :define-route="'Appointments'" :icon="MyCalendar">Afspraken</AppNavigationItem>
            <AppNavigationItem :define-route="'MedicalFile'" :icon="MyFolder">Medisch Dossier</AppNavigationItem>
            <AppNavigationItem :define-route="'Referrals'" :icon="MyDocumentArrow">Verwijsbrieven</AppNavigationItem>
            <AppNavigationItem :define-route="'Patiënts'" :icon="MyUsers">Patiënten</AppNavigationItem>
            <AppNavigationItem :define-route="'MedicalResults'" :icon="MyClipboard">Onderzoeksuitslagen</AppNavigationItem>
        </ul>
    </nav>
</template>