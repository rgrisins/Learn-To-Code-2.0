<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'

const route = useRoute()
const required = typeof route.query.required === 'string' ? route.query.required : ''
</script>

<template>
  <section class="content-panel card border-primary-subtle">
    <div class="card-body p-3 p-lg-4">
      <h1 class="section-heading mb-3">Pieeja liegta</h1>
      <p class="mb-3 text-secondary-emphasis">
        Tev nav nepieciešamo tiesību, lai atvērtu šo sadaļu.
        <span v-if="required"> Nepieciešamās lomas: {{ required }}.</span>
      </p>

      <RouterLink class="btn btn-primary" to="/profile">
        Uz profilu
      </RouterLink>
    </div>
  </section>
</template>