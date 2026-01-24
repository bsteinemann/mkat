# [1.3.0](https://github.com/bsteinemann/mkat/compare/v1.2.1...v1.3.0) (2026-01-24)


### Features

* add EventBroadcaster with Channel-based pub/sub ([9541270](https://github.com/bsteinemann/mkat/commit/954127050486e4b48334e2148cb04add38e67e8e))
* add frontend push subscription and SSE connection ([45715fa](https://github.com/bsteinemann/mkat/commit/45715fada9de80f3fac5836ca41d800f142e88a4))
* add IEventBroadcaster interface and ServerEvent DTO ([b82f7ed](https://github.com/bsteinemann/mkat/commit/b82f7ed08716e863b3ed2517eb3fe301ece6ed57))
* add push subscription API endpoints ([3607bcd](https://github.com/bsteinemann/mkat/commit/3607bcdb88da99ab375a98e0d60df49888554e84))
* add PushSubscription domain entity ([c70478f](https://github.com/bsteinemann/mkat/commit/c70478fbc7ee86e7e75f6146c1555da1a27c207b))
* add PushSubscription repository and EF Core mapping ([d049460](https://github.com/bsteinemann/mkat/commit/d049460ba04a66a619d5c968b503ca9c8800c42b))
* add PWA manifest and app icons ([b5b7549](https://github.com/bsteinemann/mkat/commit/b5b7549e73014daf989a9fc7e6dd276e2359a813))
* add service worker for push notifications ([dbb1150](https://github.com/bsteinemann/mkat/commit/dbb11509854ee5ccded06d46c9f1cd5915c6313e))
* add SSE events stream endpoint ([c623fd6](https://github.com/bsteinemann/mkat/commit/c623fd64bd24849d98bfdcf2dca044657d0ea23a))
* add WebPushChannel with VAPID support ([19b1ec2](https://github.com/bsteinemann/mkat/commit/19b1ec26fe0ff958a4227575d8f494089ad88414))
* broadcast SSE event after successful alert dispatch ([a9beaae](https://github.com/bsteinemann/mkat/commit/a9beaae0164bd30752d14ca05c6464069d5cd5c0))

## [1.2.1](https://github.com/bsteinemann/mkat/compare/v1.2.0...v1.2.1) (2026-01-24)


### Bug Fixes

* **ci:** write semantic-release outputs to GITHUB_OUTPUT ([a8da3a1](https://github.com/bsteinemann/mkat/commit/a8da3a160dd4862acb4343ccf78e5dbb03b12b96))

# [1.2.0](https://github.com/bsteinemann/mkat/compare/v1.1.0...v1.2.0) (2026-01-23)


### Bug Fixes

* sanitize base path injection and cache fallback HTML ([7429bf3](https://github.com/bsteinemann/mkat/commit/7429bf3a9cda4eab2dfd3c39443a44478df5cb74))


### Features

* add getBasePath runtime config utility ([481d393](https://github.com/bsteinemann/mkat/commit/481d393f9458b733afcd430012ea41debcebcd64))
* add UsePathBase and runtime config injection for base path support ([f8e42c9](https://github.com/bsteinemann/mkat/commit/f8e42c9d6d8678a5b5467b3fdbecccc7d2bd9eed))
* configure TanStack Router with runtime basepath ([7cc544c](https://github.com/bsteinemann/mkat/commit/7cc544c25b68865bde0ab5656e6b4da606e2257f))
* configure Vite for relative asset paths ([9372fee](https://github.com/bsteinemann/mkat/commit/9372feeb39fbf726280fcb9f3cc7889d0fe6ed73))
* update API client to use runtime base path ([d768590](https://github.com/bsteinemann/mkat/commit/d76859044092315542956b2a2d841549d91162ac))
* update login page to use base path for auth check ([cd5b27c](https://github.com/bsteinemann/mkat/commit/cd5b27c7eea37d7f0391a9eb9e12001be26861cd))

# [1.1.0](https://github.com/bsteinemann/mkat/compare/v1.0.0...v1.1.0) (2026-01-23)


### Bug Fixes

* pass Telegram env vars to container and escape MarkdownV2 time string ([8f6c2f0](https://github.com/bsteinemann/mkat/commit/8f6c2f00d2e8debf1c5168d02801a7fe0e25daed))
* remove useEffect setState in ContactsSection to satisfy lint rule ([8208547](https://github.com/bsteinemann/mkat/commit/8208547814f4292c6927dcd34a8f2c0de4b3f555))
* serialize HealthEndpointTests to prevent Serilog race condition ([e25a5c0](https://github.com/bsteinemann/mkat/commit/e25a5c0ad98943a898a0202dbc8e8e9f6e6dc674))


### Features

* add channel management endpoints for contacts ([19b305d](https://github.com/bsteinemann/mkat/commit/19b305dc1ecb0cd78b871e7a04cf0e2c63fe63ca))
* add Contact CRUD endpoints with validation ([db553a9](https://github.com/bsteinemann/mkat/commit/db553a9a2d3ff848874ffb45158c2b31ce8f0d2c))
* add contact picker to service edit page ([dbc8ae0](https://github.com/bsteinemann/mkat/commit/dbc8ae048c47d12711f6f2ad37faf1c0532c8e2d))
* add Contact, ContactChannel, ServiceContact entities and EF migration ([7468535](https://github.com/bsteinemann/mkat/commit/74685356a9c0b3c7dd9ffcaee23ae6be6c9301c9))
* add FluentValidation rules for metric monitor config ([1f56ab4](https://github.com/bsteinemann/mkat/commit/1f56ab447fbdabd86f39ac91dc9bf1e925ad8be4))
* add frontend contacts page and service contact picker ([962c7f3](https://github.com/bsteinemann/mkat/commit/962c7f310ccc55f22a988c305056b4c72ea7ef9b))
* add frontend metric monitor support ([7ed4af2](https://github.com/bsteinemann/mkat/commit/7ed4af234c6b445c935c2177201954460ce22016))
* add frontend peers page with pairing dialog ([d463247](https://github.com/bsteinemann/mkat/commit/d463247a6b4b05bcb2d3f5b510679ddf3a03c606))
* add IMetricReadingRepository, implementation, and EF migration ([e09b5f3](https://github.com/bsteinemann/mkat/commit/e09b5f3cc799c61ebc2f34b76575480cbbea8e55))
* add metric history and latest endpoints ([e2a08dd](https://github.com/bsteinemann/mkat/commit/e2a08dd7a52ded3b08a732793d59273ee91c30d3))
* add metric push endpoint (POST /metric/{token}) ([7ad1206](https://github.com/bsteinemann/mkat/commit/7ad1206a0b5243dc8345864c045b2b27e4148e90))
* add MetricEvaluator with all 4 threshold strategies ([dc0dbe0](https://github.com/bsteinemann/mkat/commit/dc0dbe082708d8bde53e76eaedfb3e63ba7bc407))
* add MetricReading entity and metric fields to Monitor ([55a5b80](https://github.com/bsteinemann/mkat/commit/55a5b80c6e5c8bdc2a35df2e39c525235a2ea4b5))
* add MetricRetentionWorker for metric history cleanup ([cb2c548](https://github.com/bsteinemann/mkat/commit/cb2c548610a1e3aae5a44fad99015f5082b30773))
* add notification failure hook to AlertDispatchWorker ([a7eba2c](https://github.com/bsteinemann/mkat/commit/a7eba2c5854c484583f567802ffe9cc049fde54b))
* add pairing token generation and validation service ([09b4358](https://github.com/bsteinemann/mkat/commit/09b43589746aa27d85cb2285b0f4442befd85b1d))
* add Peer entity, repository, and EF migration ([7320a95](https://github.com/bsteinemann/mkat/commit/7320a95993cdb2c8ec709f298f8f1dd657ec6df8))
* add peer pairing API endpoints (initiate, complete, accept) ([f86b46c](https://github.com/bsteinemann/mkat/commit/f86b46c265916881abfabd76c62d2de42d744dcb))
* add PeerHeartbeatWorker for sending heartbeats to peers ([979a911](https://github.com/bsteinemann/mkat/commit/979a911c5a12643e8624f412e2e8e1ed8ad28635))
* add service-contact assignment endpoints ([d48e52b](https://github.com/bsteinemann/mkat/commit/d48e52b204d88f7613384fb8945a2688c84bd6a3))
* add ThresholdStrategy enum and MonitorType.Metric ([ccd6550](https://github.com/bsteinemann/mkat/commit/ccd65500db0fd3d3b0e6f634b149a4493fa1e513))
* update alert dispatch to route via contacts with fallback ([0b46975](https://github.com/bsteinemann/mkat/commit/0b46975947c37c283b7deaf8b1a5626f044f90d6))
* update monitor CRUD to support Metric type ([cc5eb71](https://github.com/bsteinemann/mkat/commit/cc5eb7157ed673b2f9be0d5e74aa76e2bf5fbd84))

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
