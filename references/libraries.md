# Library and license catalog

**Overriding rule:** package licenses change (the OSS commercialization wave in the .NET ecosystem, 2023–2025). Treat the state below as a starting point — when recommending, **verify the current license** on NuGet/GitHub (search the web when in doubt, if the agent has network access) and warn the user explicitly if a library is paid or dual-license.

## ⚠ Known commercialization cases

| Library | Status | What to do |
|---|---|---|
| **MediatR** | commercial license since v13 (free only for small companies/OSS) | Warn; alternatives: Wolverine, own implementation |
| **AutoMapper** | commercial license since v15 (same as MediatR — same author) | Warn; alternatives: Mapperly, Mapster, manual mapping |
| **FluentAssertions** | commercial license since v8 (Xceed); **v7.x remains Apache 2.0** | Always pin `[7.*` in Directory.Packages.props |
| **MassTransit** | commercial since v9; v8 supported until the end of the transition period | Warn; alternatives: Wolverine, RabbitMQ.Client directly |
| **Hangfire** | open-core: the core is free (LGPL), Pro/Ace are paid | The core is fine to use; tell the user which features are paid |
| **QuestPDF** | Community license free only below a company revenue threshold | Warn and ask about the company's size |
| **Moq** | SponsorLink controversy (2023) | Do not use — the template standard is NSubstitute (BSD) |
| **ImageSharp** | Six Labors Split License — paid above a revenue threshold | Warn; alternative: SkiaSharp (MIT) |
| **Duende IdentityServer** | commercial (free only for dev/test/low revenue) | Warn; alternatives: Keycloak, ASP.NET Core Identity + OpenIddict |

## Fixed template elements (verified, free)

| Library | License | Role |
|---|---|---|
| FluentValidation | Apache 2.0 | validation (always) |
| Serilog + Console/File/MSSqlServer/Seq/GrafanaLoki sinks | Apache 2.0 | logging (always; Seq as a *server* has a free Individual license) |
| xUnit, NSubstitute, Testcontainers | Apache 2.0 / BSD / MIT | tests (always) |
| Orval | MIT | frontend API client + TanStack Query hooks generated from the backend OpenAPI document (always) |
| EF Core, Dapper | MIT / Apache 2.0 | data access (user's choice) |
| DbUp / grate | MIT | SQL migrations in the script-based variant |
| Wolverine | MIT (core; paid JasperFx support/add-ons — verify when recommending) | mediator/messaging |
| Mapperly, Mapster | Apache 2.0 / MIT | mapping |
| Scrutor | MIT | assembly scanning for the custom mediator |

## Dynamic proposal catalog — backend

| Need | Proposals (prefer free) |
|---|---|
| background jobs / scheduling | Hangfire (open-core), Quartz.NET (Apache 2.0), BackgroundService (built-in) |
| realtime | SignalR (built into ASP.NET Core) |
| HTTP resilience | Polly / Microsoft.Extensions.Resilience (MIT) |
| external API clients | Refit (MIT), Flurl (MIT) |
| distributed cache | StackExchange.Redis (MIT), HybridCache (MIT) |
| messaging | Wolverine, RabbitMQ.Client (Apache 2.0/MPL), ⚠ MassTransit v9 |
| auth | ASP.NET Core Identity (MIT), Keycloak (Apache 2.0), OpenIddict (Apache 2.0), ⚠ Duende |
| Excel / PDF | ClosedXML (MIT), ⚠ QuestPDF, ⚠ IronPDF (commercial) |
| e-mail | MailKit (MIT) |
| feature flags | Microsoft.FeatureManagement (MIT) |

## Dynamic proposal catalog — frontend

| Need | Proposals |
|---|---|
| forms | react-hook-form + zod (MIT) |
| routing | React Router (MIT), TanStack Router (MIT) |
| tables / data grid | TanStack Table (MIT); ⚠ MUI X DataGrid — Pro/Premium features paid; ⚠ AG Grid Enterprise paid |
| charts | Recharts (MIT), Chart.js (MIT) |
| client state (beyond server state) | Zustand (MIT) — propose only when TanStack Query is not enough |
| dates | date-fns (MIT), Day.js (MIT) |
| i18n | i18next + react-i18next (MIT) |
| drag & drop | dnd-kit (MIT) |
| icons | lucide-react (ISC), @mui/icons-material (MIT, with MUI) |
| components with Tailwind | shadcn/ui (MIT, copied into the repo), Radix UI (MIT) |
