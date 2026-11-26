<script setup lang="ts">
import { computed, ref } from 'vue'

const model = defineModel<string>({ required: true })
const props = defineProps<{
  id: string
  label: string
  options: readonly string[]
  noneLabel: string
}>()

const items = ref<string[]>([])
const search = ref('')
const custom = ref('')
const status = ref('')
const filteredOptions = computed(() =>
  props.options.filter((option) =>
    option.toLocaleLowerCase('nl').includes(search.value.trim().toLocaleLowerCase('nl')),
  ),
)

function updateValue() {
  model.value = status.value || items.value.join('\n')
}

function add(value: string) {
  const entry = value.trim()
  if (!entry || items.value.some((item) => item.toLowerCase() === entry.toLowerCase())) return
  status.value = ''
  items.value.push(entry)
  updateValue()
}

function choose(event: Event) {
  const select = event.target as HTMLSelectElement
  if (select.value === props.noneLabel || select.value === 'Onbekend') {
    status.value = select.value
    updateValue()
  } else {
    add(select.value)
  }
  select.value = ''
}

function addCustom() {
  add(custom.value)
  custom.value = ''
}

function remove(index: number) {
  items.value.splice(index, 1)
  updateValue()
}

const controlClasses =
  'block w-full min-w-0 rounded-xl border border-gray-500 bg-white px-3 py-2 text-gray-900 focus-visible:outline-2 focus-visible:outline-blue-600'
</script>

<template>
  <fieldset class="min-w-0 space-y-2">
    <legend class="mb-2 text-sm font-semibold">{{ label }}</legend>
    <label :for="`${id}-search`" class="block text-sm">Zoeken in {{ label.toLowerCase() }}</label>
    <input
      :id="`${id}-search`"
      v-model="search"
      type="search"
      :class="controlClasses"
      autocomplete="off"
      @keydown.enter.prevent
    />
    <div v-if="search.trim()">
      <p role="status" class="text-sm">
        {{
          filteredOptions.length
            ? `${filteredOptions.length} gevonden`
            : 'Geen resultaten. Je kunt het item zelf toevoegen.'
        }}
      </p>
      <ul
        v-if="filteredOptions.length"
        class="mt-1 max-h-48 overflow-y-auto rounded-xl border border-gray-300"
      >
        <li v-for="option in filteredOptions" :key="option">
          <button
            type="button"
            class="block w-full px-3 py-2 text-left hover:bg-blue-50 focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-blue-600 disabled:text-gray-500"
            :disabled="items.includes(option)"
            @click="add(option)"
          >
            {{ option }}{{ items.includes(option) ? ' (toegevoegd)' : '' }}
          </button>
        </li>
      </ul>
    </div>
    <label :for="id" class="block text-sm">Kies een optie om toe te voegen</label>
    <select :id="id" :class="controlClasses" @change="choose">
      <option value="">Kies uit de lijst</option>
      <option :disabled="items.length > 0">{{ noneLabel }}</option>
      <option :disabled="items.length > 0">Onbekend</option>
      <option v-for="option in options" :key="option" :disabled="items.includes(option)">
        {{ option }}
      </option>
    </select>
    <label :for="`${id}-custom`" class="block text-sm"
      >Staat het er niet bij? Voeg het zelf toe.</label
    >
    <input
      :id="`${id}-custom`"
      v-model="custom"
      data-pending-entry
      :class="controlClasses"
      autocomplete="off"
      pattern="^$"
      title="Klik op Toevoegen of maak dit veld leeg voordat je opslaat."
      @keydown.enter.prevent="addCustom"
    />
    <button
      type="button"
      class="text-blue-700 underline disabled:text-gray-500"
      :disabled="!custom.trim()"
      @click="addCustom"
    >
      Toevoegen
    </button>
    <div aria-live="polite">
      <p v-if="status">{{ status }}</p>
      <ul v-else-if="items.length" class="space-y-1">
        <li v-for="(item, index) in items" :key="item" class="flex flex-wrap items-center gap-2">
          <span class="break-all">{{ item }}</span>
          <button
            type="button"
            class="text-blue-700 underline"
            :aria-label="`Verwijder ${item}`"
            @click="remove(index)"
          >
            Verwijder
          </button>
        </li>
      </ul>
    </div>
  </fieldset>
</template>
