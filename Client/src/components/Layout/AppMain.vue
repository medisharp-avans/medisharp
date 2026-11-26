<!-- Hoofdinhoud van de pagina. tabindex="-1" zodat de skip link en Alt + M hier focus kunnen zetten.
     min-w-0 zorgt dat brede inhoud (zoals tabellen) binnen de pagina scrolt in plaats van de hele pagina te verbreden. -->
<template>
  <main id="main-content" class="flex-1 min-w-0 m-2 sm:m-4 bg-white text-gray-900" tabindex="-1">
    <slot></slot>
  </main>
</template>
