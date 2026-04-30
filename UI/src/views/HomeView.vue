<script setup lang="ts">
const featuredNews = [
  {
    category: 'Teorija',
    date: '28.04.2026',
    title: 'Programmēšanas valodu katalogs ir atvērts',
    summary:
      'Teorijas sadaļā tagad var izvēlēties valodu, tēmu un lasīt materiālu pa lapām. Sākumā pieejamas Python, Java un C# valodas.',
  },
  {
    category: 'Pedagogiem',
    date: '28.04.2026',
    title: 'Pieprasījumi teorijas papildināšanai',
    summary:
      'Pedagogi un administratori var iesniegt jaunas tēmas, pievienot teorijas lapas vai piedāvāt labojumus esošam saturam.',
  },
  {
    category: 'Administrēšana',
    date: '27.04.2026',
    title: 'Admin panelī pieejama lietotāju un pieprasījumu pārvaldība',
    summary:
      'Administratoriem ir vienots skats lietotāju sarakstam, lomu pieprasījumiem un teorijas rediģēšanas pieprasījumiem.',
  },
]

const sideNews = [
  'Audzēkņi var lasīt teoriju bez pieteikšanās.',
  'Pedagoga lomas pieprasījumi tiek pārbaudīti pirms apstiprināšanas.',
  'Teorijas saturs tiek glabāts Markdown formātā, lai ērti rādītu koda piemērus.',
]
</script>

<template>
  <section class="news-layout">
    <header class="news-hero content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 p-xl-5">
        <h1 class="section-heading mb-3">Aktuālais platformā</h1>
        <p class="news-lead mb-0">
          Jaunumi par mācību materiāliem, lomu pieprasījumiem un platformas iespējām vienā pārskatāmā plūsmā.
        </p>
      </div>
    </header>

    <div class="news-main">
      <article
        v-for="item in featuredNews"
        :key="item.title"
        class="news-card content-panel card border-primary-subtle"
      >
        <div class="card-body p-3 p-lg-4">
          <div class="news-meta">
            <span>{{ item.category }}</span>
            <time>{{ item.date }}</time>
          </div>
          <h2>{{ item.title }}</h2>
          <p>{{ item.summary }}</p>
        </div>
      </article>
    </div>

    <aside class="news-sidebar content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <h2 class="news-sidebar__title">Kas jāzina</h2>
        <ul class="news-list">
          <li v-for="item in sideNews" :key="item">{{ item }}</li>
        </ul>
      </div>
    </aside>
  </section>
</template>
