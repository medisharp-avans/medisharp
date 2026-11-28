<script setup lang="ts">
import { ref } from 'vue'
import axios from 'axios'
import { useRoute, useRouter } from 'vue-router'
import AuthLayout from '@/components/Layout/AuthLayout.vue'
import BaseInput from '@/components/elements/BaseInput.vue'
import BaseButton from '@/components/elements/BaseButton.vue'
import { useAuthStore, type LoginChallenge } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const code = ref('')
// Gezet na stap 1 (juist wachtwoord): dan wordt de codestap getoond.
const challenge = ref<LoginChallenge | null>(null)
const fieldErrors = ref<{ email: string | null; password: string | null; code: string | null }>({
  email: null,
  password: null,
  code: null,
})
const error = ref<string | null>(null)
const loading = ref(false)

function validate(): boolean {
  fieldErrors.value = {
    email: email.value.trim() ? null : 'E-mailadres is verplicht.',
    password: password.value ? null : 'Wachtwoord is verplicht.',
    code: null,
  }
  return !fieldErrors.value.email && !fieldErrors.value.password
}

function statusOf(err: unknown): number | undefined {
  return axios.isAxiosError(err) ? err.response?.status : undefined
}

async function submitCredentials() {
  if (!validate()) return
  try {
    challenge.value = await auth.login(email.value.trim(), password.value)
    code.value = ''
  } catch (err) {
    error.value =
      statusOf(err) === 401
        ? 'Ongeldig e-mailadres of wachtwoord.'
        : 'Inloggen is mislukt. Probeer het later opnieuw.'
  } finally {
    password.value = ''
  }
}

async function submitCode() {
  const value = code.value.trim()
  fieldErrors.value.code = /^\d{6}$/.test(value) ? null : 'Voer de 6-cijferige code in.'
  if (fieldErrors.value.code || !challenge.value) return

  try {
    await auth.verifyCode(challenge.value.challengeId, value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    // Alleen interne paden toestaan.
    await router.push(redirect.startsWith('/') && !redirect.startsWith('//') ? redirect : '/')
  } catch (err) {
    error.value =
      statusOf(err) === 401
        ? 'Onjuiste of verlopen verificatiecode.'
        : 'Verifiëren is mislukt. Probeer het later opnieuw.'
    code.value = ''
  }
}

async function onSubmit() {
  error.value = null
  loading.value = true
  try {
    await (challenge.value ? submitCode() : submitCredentials())
  } finally {
    loading.value = false
  }
}

function restart() {
  challenge.value = null
  code.value = ''
  error.value = null
}
</script>

<template>
  <AuthLayout :error="error" @submit="onSubmit">
    <template #title>{{ challenge ? 'Verificatiecode' : 'Inloggen' }}</template>
    <template #subtitle>
      <template v-if="challenge">
        We hebben een code gestuurd naar {{ challenge.maskedEmail }}. De code is 10 minuten geldig.
      </template>
      <template v-else>Log in met uw MediSharp-account.</template>
    </template>

    <template v-if="challenge">
      <BaseInput
        v-model="code"
        label="Verificatiecode"
        autocomplete="one-time-code"
        required
        :disabled="loading"
        :error="fieldErrors.code"
        data-testid="login-code"
      />
    </template>
    <template v-else>
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
    </template>

    <template #actions>
      <BaseButton type="submit" :disabled="loading" data-testid="login-submit">
        <template v-if="challenge">{{
          loading ? 'Bezig met controleren…' : 'Bevestigen'
        }}</template>
        <template v-else>{{ loading ? 'Bezig met inloggen…' : 'Inloggen' }}</template>
      </BaseButton>
      <button
        v-if="challenge"
        type="button"
        class="text-sm font-medium text-blue-600 hover:underline"
        :disabled="loading"
        data-testid="login-restart"
        @click="restart"
      >
        Opnieuw inloggen / nieuwe code
      </button>
    </template>
  </AuthLayout>
</template>
