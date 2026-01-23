# 1.0.0 (2026-01-23)


### Bug Fixes

* correct solution file format, test setup, and startup configuration ([aed97f7](https://github.com/bsteinemann/mkat/commit/aed97f728118c440b525bdfe0bdaf3f9b3c28609))
* only require auth for /api/ paths, allow SPA access ([2291674](https://github.com/bsteinemann/mkat/commit/2291674e1b4c775dcdb58e73f66d06c786434576))
* remove background workers from integration test hosts ([605b1e5](https://github.com/bsteinemann/mkat/commit/605b1e54eb3eebe0b61a02807f22315acc6393cb))
* resolve Monitor ambiguity and add missing Xunit usings ([99589fb](https://github.com/bsteinemann/mkat/commit/99589fbaf7df4fea39186882865f2f26b6e22fcd))
* update Vite proxy target to port 8080 ([dd302d5](https://github.com/bsteinemann/mkat/commit/dd302d54a9d6b5a9469816b711acbd0e1c75c2c6))


### Features

* add AlertDispatchWorker background service ([7cea836](https://github.com/bsteinemann/mkat/commit/7cea8364346c7a899fdb3fbb9c84040f2aafb374))
* add AlertsController with CRUD and acknowledge endpoints ([787f954](https://github.com/bsteinemann/mkat/commit/787f95412a8eef676db4429bac7a97b04a37d16f))
* add API client layer and layout/common components ([d083e4f](https://github.com/bsteinemann/mkat/commit/d083e4f8b521e3f7bb3b661add3c273987c82610))
* add BasicAuthMiddleware with tests ([241c6a1](https://github.com/bsteinemann/mkat/commit/241c6a17d349b68a6aabae04b7c6bd9de95cf1f6))
* add DTOs and FluentValidation validators ([367e2df](https://github.com/bsteinemann/mkat/commit/367e2dffdaa90aea780e07a159ee92eaf76178ff))
* add GetPausedServicesAsync to IServiceRepository ([616f501](https://github.com/bsteinemann/mkat/commit/616f5015273465274b36381523a0d6bf02530464))
* add HeartbeatController with heartbeat endpoint ([ce7da6c](https://github.com/bsteinemann/mkat/commit/ce7da6c81d2db927fa869463b4f12d28bb3abf72))
* add HeartbeatMonitorWorker background service ([3f63ef1](https://github.com/bsteinemann/mkat/commit/3f63ef17b818ca406dc315aaf134a745066bdacd))
* add IMuteWindowRepository and MuteWindowRepository ([20a5a13](https://github.com/bsteinemann/mkat/commit/20a5a1380dc6e13505e440b45f370d132419aea1))
* add MaintenanceResumeWorker background service ([ae3f808](https://github.com/bsteinemann/mkat/commit/ae3f808991beebbd4d4a7263980c103f4d92ce41))
* add monitor CRUD endpoints and UI ([6910113](https://github.com/bsteinemann/mkat/commit/6910113b9144bd04fcb77ff1e328ac29227fdeac))
* add Mute endpoint to ServicesController ([57e7fcd](https://github.com/bsteinemann/mkat/commit/57e7fcdb4364bfd857430b7ba45172159fc1f612))
* add notification interfaces and DTOs ([d4fa0b5](https://github.com/bsteinemann/mkat/commit/d4fa0b56255fd3950d84bb3066b7d024e7d42910))
* add NotificationDispatcher service ([a9f9833](https://github.com/bsteinemann/mkat/commit/a9f9833744ac2825f0ac3aefd58901c1d8bb1db5))
* add pages, components, router, and TanStack Query setup ([2949fef](https://github.com/bsteinemann/mkat/commit/2949fefe4a35d8c28885924e56f5f2f0f2b32dce))
* add Pause/Resume endpoints to ServicesController ([6bb8474](https://github.com/bsteinemann/mkat/commit/6bb84748207e5283ea8188a3263f82c41e303730))
* add production docker-compose.yml for deployment ([fe0794b](https://github.com/bsteinemann/mkat/commit/fe0794bbb38726f0e5c9e540bf2814c34399531c))
* add ServicesController with CRUD endpoints and integration tests ([2a2b3fb](https://github.com/bsteinemann/mkat/commit/2a2b3fbb06201f6cffb98e580539071815ae8a8e))
* add StateService with state machine logic ([0098133](https://github.com/bsteinemann/mkat/commit/0098133b11af8b92bb16b21a8435d67d968805e8))
* add structured logging, exception handling, and security headers middleware ([b5ab056](https://github.com/bsteinemann/mkat/commit/b5ab0563d4978336ba3aef32b60d9450cd8625bb))
* add TelegramBotService and wire M4 notification DI ([eea220f](https://github.com/bsteinemann/mkat/commit/eea220f8fde586be40d9c9feafa2a28b2b9bc177))
* add TelegramChannel with retry logic and message formatting ([5a93dc6](https://github.com/bsteinemann/mkat/commit/5a93dc68184a637b4ebc0837aa0b4b83618aced3))
* add webhook test buttons and HTTP POST labels to service detail ([251522b](https://github.com/bsteinemann/mkat/commit/251522bf69e13e83285acceadbac5b8748268125))
* add WebhookController with fail/recover endpoints ([26f1676](https://github.com/bsteinemann/mkat/commit/26f1676b8430252cb8b35bebc474d5ce032a4c57))
* scaffold React frontend with Vite, Tailwind v4, TanStack ([5c1dc76](https://github.com/bsteinemann/mkat/commit/5c1dc76980ad50b3432b0406923a88e772b9a61b))
* serve SPA from API with static files and fallback routing ([02b21a9](https://github.com/bsteinemann/mkat/commit/02b21a968ab5db86a4b4f751d9c26307c03158a4))
* update Dockerfile with multi-stage frontend build and security hardening ([f7415c2](https://github.com/bsteinemann/mkat/commit/f7415c2b1551bac06355b2c36344bbe7697bc0b7))
