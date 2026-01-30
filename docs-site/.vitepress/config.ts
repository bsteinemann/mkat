import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'mkat',
  description: 'Self-hosted monitoring for homelabs and small web projects',
  base: '/mkat/',
  cleanUrls: true,
  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/mkat/favicon.svg' }],
  ],
  themeConfig: {
    nav: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'Concepts', link: '/concepts/services' },
      { text: 'Recipes', link: '/recipes/web-api' },
      { text: 'API Reference', link: '/api-reference' },
    ],
    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'What is mkat?', link: '/' },
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Deployment', link: '/deployment' },
        ],
      },
      {
        text: 'Concepts',
        items: [
          { text: 'Services', link: '/concepts/services' },
          { text: 'Monitors', link: '/concepts/monitors' },
          { text: 'Alerts & Notifications', link: '/concepts/alerts' },
          { text: 'Peer Monitoring', link: '/concepts/peers' },
        ],
      },
      {
        text: 'Recipes',
        items: [
          { text: 'Monitor a Web API', link: '/recipes/web-api' },
          { text: 'Monitor a Cron Job', link: '/recipes/cron-job' },
          { text: 'Track a Metric', link: '/recipes/metric' },
          { text: 'Set Up Telegram', link: '/recipes/telegram' },
        ],
      },
      {
        text: 'Reference',
        items: [
          { text: 'API Reference', link: '/api-reference' },
          { text: 'Environment Variables', link: '/environment-variables' },
        ],
      },
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/bsteinemann/mkat' },
    ],
    editLink: {
      pattern: 'https://github.com/bsteinemann/mkat/edit/main/docs-site/:path',
    },
    search: {
      provider: 'local',
    },
    footer: {
      message: 'Released under the MIT License.',
    },
  },
})
