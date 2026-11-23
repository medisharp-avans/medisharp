<script setup lang="ts">
import { ref } from 'vue'
import axios from 'axios'
import { useRoute, useRouter } from 'vue-router'
import AuthTemplate from './AuthTemplate.vue'
import BaseInput from '@/components/elements/BaseInput.vue'
import BaseButton from '@/components/elements/BaseButton.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const username = ref('')
const password = ref('')
const fieldErrors = ref<{ username: string | null; password: string | null }>({
  username: null,
  password: null,
})
const error = ref<string | null>(null)
const loading = ref(false)

function validate(): boolean {
  fieldErrors.value = {
    username: username.value.trim() ? null : 'Gebruikersnaam is verplicht.',
    password: password.value ? null : 'Wachtwoord is verplicht.',
  }
  return !fieldErrors.value.username && !fieldErrors.value.password
}

async function onSubmit() {
  error.value = null
  if (!validate()) return

  loading.value = true
  try {
    await auth.login(username.value.trim(), password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    // Alleen interne paden toestaan.
    await router.push(redirect.startsWith('/') && !redirect.startsWith('//') ? redirect : '/')
  } catch (err) {
    const status = axios.isAxiosError(err) ? err.response?.status : undefined
    error.value =
      status === 401
        ? 'Ongeldige gebruikersnaam of wachtwoord.'
        : 'Inloggen is mislukt. Probeer het later opnieuw.'
    password.value = ''
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthTemplate :error="error" @submit="onSubmit">
    <template #title>Inloggen</template>
    <template #subtitle>Log in met uw MediSharp-account.</template>

    <BaseInput
      v-model="username"
      label="Gebruikersnaam"
      autocomplete="username"
      required
      :disabled="loading"
      :error="fieldErrors.username"
      data-testid="login-username"
    />
    <BaseInput
      v-model="password"
      label="Wachtwoord"
      type="password"
      autocomplete="current-password"
      required
      :disabled="loading"
      :error="fieldErrors.password"
      data-testid="login-password"
    />

    <template #actions>
      <BaseButton type="submit" :disabled="loading" data-testid="login-submit">
        {{ loading ? 'Bezig met inloggen…' : 'Inloggen' }}
      </BaseButton>
    </template>

    <!--
    <template #footer>
      Nog geen account?
      <RouterLink :to="{ name: 'Register' }" class="font-medium text-blue-600 hover:underline">
        Registreren
      </RouterLink>
    </template>
    -->
  </AuthTemplate>
</template>
