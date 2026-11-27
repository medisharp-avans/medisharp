<script setup lang="ts">
// Gedeelde opmaak voor de authenticatiepagina's (login, registreren).
// Slots: "title" = kop, "subtitle" = korte uitleg, default = formuliervelden,
// "actions" = knoppen onderaan het formulier, "footer" = link naar de andere pagina.
// Het formulier zit in de template; de view luistert naar @submit.
import IconLogo from '@/components/icons/IconLogo.vue'

const props = withDefaults(
  defineProps<{
    /** Algemene foutmelding boven het formulier (bv. ongeldige inloggegevens). */
    error?: string | null
  }>(),
  { error: null },
)

const emit = defineEmits<{ submit: [] }>()
</script>

<template>
  <section
    class="flex items-center justify-center min-h-screen w-full bg-gray-100 p-6 text-gray-900"
    aria-labelledby="auth-title"
  >
    <div class="max-w-md w-full p-8 bg-white rounded-2xl border border-gray-200 shadow-sm">
      <div class="flex flex-col items-center text-center mb-8">
        <IconLogo class="size-10 text-blue-600 mb-4" />
        <h1 id="auth-title" class="text-2xl font-bold text-gray-900 leading-tight">
          <slot name="title"></slot>
        </h1>
        <p v-if="$slots.subtitle" class="mt-2 text-base text-gray-600 leading-relaxed">
          <slot name="subtitle"></slot>
        </p>
      </div>

      <div
        v-if="props.error"
        role="alert"
        class="mb-6 px-4 py-3 text-sm text-red-700 bg-red-50 border border-red-200 rounded-xl"
        data-testid="auth-error"
      >
        {{ props.error }}
      </div>

      <form class="flex flex-col gap-5" novalidate @submit.prevent="emit('submit')">
        <slot></slot>

        <div class="flex flex-col gap-3 mt-2">
          <slot name="actions"></slot>
        </div>
      </form>

      <div v-if="$slots.footer" class="mt-8 text-center text-sm text-gray-600">
        <slot name="footer"></slot>
      </div>
    </div>
  </section>
</template>
