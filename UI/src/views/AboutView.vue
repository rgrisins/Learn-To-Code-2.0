<script setup lang="ts">
const audienceCards = [
  {
    title: 'Audzēkņiem',
    text: 'Strukturēta teorija, testi, praktiski uzdevumi un profils, kur redzams progress.',
    icon: 'student',
  },
  {
    title: 'Pedagogiem',
    text: 'Iespēja iesniegt jaunas tēmas, papildināt lapas, veidot testus un sekot audzēkņu iesniegumiem.',
    icon: 'teacher',
  },
]

const capabilities = [
  'Teorija glabājas Markdown formātā, lai kods un skaidrojumi būtu viegli uzturami.',
  'Uzdevumi tiek pārbaudīti ar testiem, tāpēc audzēknis uzreiz redz, vai risinājums strādā.',
  'Reitings un pārstāvniecības palīdz salīdzināt progresu individuāli un komandās.',
  'Mācību saturs ir veidots pa īsām, pārskatāmām tēmām, lai viegli turpināt no vietas, kur apstājies.',
]
</script>

<template>
  <section class="about-layout">
    <header class="about-hero content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <div class="theory-header exercises-header">
          <span class="page-title-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="10" />
              <path d="M12 16v-4" />
              <path d="M12 8h.01" />
            </svg>
          </span>
          <div class="theory-heading-copy">
            <h1 class="section-heading mb-1">Par LearnToCode</h1>
            <p class="about-lead mb-0">
              Platforma apvieno teoriju, testus, praktiskus uzdevumus, reitingu un pārstāvniecību darbu vienā mācību vidē.
            </p>
          </div>
        </div>
      </div>
    </header>

    <div class="about-content-grid">
      <article class="about-story content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <div class="representations-section-heading mb-3">
            <span class="representations-section-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M12 3v18" />
                <path d="M5 8h14" />
                <path d="M7 16h10" />
              </svg>
            </span>
            <h2 class="section-heading section-heading--caps mb-0">Kāpēc tas vajadzīgs</h2>
          </div>
          <div class="about-copy">
            <p>
              Programmēšanas sākumā svarīgākais ir skaidra secība: saprast pamatus, redzēt piemērus,
              pašam pamēģināt un saņemt atgriezenisko saiti. LearnToCode šo ceļu sadala mazākos soļos,
              lai audzēknis nezaudētu virzienu.
            </p>
            <p>
              Teorija ir sadalīta pa valodām un tēmām, uzdevumi palīdz pārbaudīt praktisko izpratni,
              bet progress profilā un reitingā dod redzamu motivāciju turpināt.
            </p>
          </div>

          <div class="about-pill-grid">
            <div v-for="card in audienceCards" :key="card.title" class="about-pill">
              <span class="about-pill__icon" aria-hidden="true">
                <svg v-if="card.icon === 'student'" viewBox="0 0 24 24" width="19" height="19" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M22 10 12 5 2 10l10 5 10-5z" />
                  <path d="M6 12v5c3 2 9 2 12 0v-5" />
                </svg>
                <svg v-else viewBox="0 0 24 24" width="19" height="19" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M4 19.5V5a2 2 0 0 1 2-2h11" />
                  <path d="M6 17h11a2 2 0 0 1 2 2v1H6a2 2 0 0 1 0-4z" />
                  <path d="M9 7h5" />
                  <path d="M9 11h7" />
                </svg>
              </span>
              <div>
                <strong>{{ card.title }}</strong>
                <span>{{ card.text }}</span>
              </div>
            </div>
          </div>
        </div>
      </article>

      <aside class="about-capabilities content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <div class="representations-section-heading mb-3">
            <span class="representations-section-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M20 6 9 17l-5-5" />
              </svg>
            </span>
            <h2 class="news-sidebar__title mb-0">Ko platforma nodrošina</h2>
          </div>
          <ul class="news-list">
            <li v-for="item in capabilities" :key="item">{{ item }}</li>
          </ul>
        </div>
      </aside>
    </div>
  </section>
</template>
