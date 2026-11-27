<script setup lang="ts">
// Bovenbalk met logo/home-link, applicatienaam en profielmenu.
import IconLogo from '@/components/icons/IconLogo.vue'
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { HugeiconsIcon } from '@hugeicons/vue'
import { Logout01Icon, UserCircleIcon, UserIcon } from '@hugeicons/core-free-icons'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

const isMenuOpen = ref(false)
const menuRoot = ref<HTMLElement | null>(null)
const menuButton = ref<HTMLButtonElement | null>(null)

function toggleMenu() {
  isMenuOpen.value = !isMenuOpen.value
}

function closeMenu(returnFocus = false) {
  isMenuOpen.value = false
  if (returnFocus) menuButton.value?.focus()
}

function onDocumentClick(event: MouseEvent) {
  if (menuRoot.value && !menuRoot.value.contains(event.target as Node)) closeMenu()
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && isMenuOpen.value) closeMenu(true)
}

async function handleLogout() {
  closeMenu()
  await auth.logout()
  await router.push({ name: 'Login' })
}

onMounted(() => {
  document.addEventListener('click', onDocumentClick)
  document.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', onDocumentClick)
  document.removeEventListener('keydown', onKeydown)
})
</script>
<template>
  <header
    class="flex justify-between items-center px-6 py-4 bg-gray-100 border-b border-gray-200 text-gray-900"
  >
    <div class="flex items-center gap-3">
      <RouterLink
        :to="{ name: 'Home' }"
        class="flex items-center justify-center w-12 h-12 rounded-full cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
        aria-label="Home"
        id="home-button"
      >
        <IconLogo class="w-10 h-10 text-gray-900" />
      </RouterLink>
      <h1 class="text-lg font-semibold">De Zachte Heelmeesters</h1>
    </div>

    <div ref="menuRoot" class="relative">
      <button
        ref="menuButton"
        type="button"
        class="flex items-center justify-center w-12 h-12 rounded-full hover:bg-gray-200 transition-colors cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
        aria-label="Profielmenu"
        aria-haspopup="menu"
        :aria-expanded="isMenuOpen"
        aria-controls="profile-menu"
        id="profile-button"
        @click="toggleMenu"
      >
        <HugeiconsIcon :icon="UserCircleIcon" class="w-8 h-8 text-gray-900" aria-hidden="true" />
      </button>

      <div
        v-show="isMenuOpen"
        id="profile-menu"
        role="menu"
        aria-labelledby="profile-button"
        class="absolute right-0 mt-2 w-64 py-1 bg-white border border-gray-200 rounded-lg shadow-lg z-50"
      >
        <div v-if="auth.user" class="px-4 py-3 border-b border-gray-200" id="profile-menu-user">
          <p class="text-sm font-medium text-gray-900 truncate" :title="auth.user.fullName">
            {{ auth.user.fullName }}
          </p>
          <span
            v-if="auth.roleLabel"
            class="inline-flex items-center mt-1 px-2 py-0.5 rounded-full bg-blue-50 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-200"
            id="profile-menu-role"
          >
            {{ auth.roleLabel }}
          </span>
        </div>
        <RouterLink
          :to="{ name: 'Profile' }"
          role="menuitem"
          id="profile-menu-profile"
          class="flex items-center gap-2 px-4 py-2 text-sm text-gray-900 hover:bg-gray-100 focus:outline-none focus-visible:bg-gray-100"
          @click="closeMenu()"
        >
          <HugeiconsIcon :icon="UserIcon" class="w-5 h-5" aria-hidden="true" />
          Profiel
        </RouterLink>
        <button
          type="button"
          role="menuitem"
          id="profile-menu-logout"
          class="flex w-full items-center gap-2 px-4 py-2 text-sm text-red-700 hover:bg-gray-100 cursor-pointer focus:outline-none focus-visible:bg-gray-100"
          @click="handleLogout"
        >
          <HugeiconsIcon :icon="Logout01Icon" class="w-5 h-5" aria-hidden="true" />
          Uitloggen
        </button>
      </div>
    </div>
  </header>
</template>
