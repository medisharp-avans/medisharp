<script setup lang="ts">
import { ref } from 'vue'
import axios from 'axios'
import { useRoute, useRouter } from 'vue-router'
import AuthLayout from '@/components/Layout/AuthLayout.vue'
import BaseInput from '@/components/elements/BaseInput.vue'
import BaseButton from '@/components/elements/BaseButton.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const fieldErrors = ref<{ email: string | null; password: string | null }>({
  email: null,
  password: null,
})
const error = ref<string | null>(null)
const loading = ref(false)

function validate(): boolean {
  fieldErrors.value = {
    email: email.value.trim() ? null : 'E-mailadres is verplicht.',
    password: password.value ? null : 'Wachtwoord is verplicht.',
  }
  return !fieldErrors.value.email && !fieldErrors.value.password
}

async function onSubmit() {
  error.value = null
  if (!validate()) return

  loading.value = true
  try {
    await auth.login(email.value.trim(), password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    // Alleen interne paden toestaan.
    await router.push(redirect.startsWith('/') && !redirect.startsWith('//') ? redirect : '/')
  } catch (err) {
    const status = axios.isAxiosError(err) ? err.response?.status : undefined
    error.value =
      status === 401
        ? 'Ongeldig e-mailadres of wachtwoord.'
        : 'Inloggen is mislukt. Probeer het later opnieuw.'
    password.value = ''
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout :error="error" @submit="onSubmit">
    <template #title>Inloggen</template>
    <template #subtitle>Log in met uw MediSharp-account.</template>

    <BaseInput
      v-model="email"
      label="E-mailadres"
      type="email"
      autocomplete="email"
      required
      :disabled="loading"
      :error="fieldErrors.email"
      data-testid="login-email"
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
  </AuthLayout>
</template>
