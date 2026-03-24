# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**OKR Driven Execution** is an enterprise OKR (Objectives & Key Results) platform focused on enforcing real execution — mandatory check-ins, accountability timelines, and cycle reporting. It is a semester project (PUC-SC) currently in its specification phase.

The project addresses the gap between OKR adoption and actual execution by combining structured data capture, visibility dashboards, and alert-driven accountability.

## Planned Architecture

The system is designed around **Event Sourcing + CQRS** with an **Event-Driven Architecture**:

- **Write path:** API receives commands → publishes domain events → PostgreSQL stores events/snapshots
- **Read path:** Event handlers consume events → update MongoDB read projections → frontend queries MongoDB via API
- **Async processing:** RabbitMQ decouples check-in submission from alert/report generation

### Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Next.js + TypeScript |
| Backend | .NET (C#) |
| Write DB | PostgreSQL (Event Sourcing) |
| Read DB | MongoDB (CQRS projections) |
| Message Queue | RabbitMQ |
| Infrastructure | Docker + Terraform |

### Key Domain Concepts

- **OKR:** Objective + Key Results (each with metric, period, owner, department)
- **Key Result statuses:** On-track / At-risk / Off-track
- **Check-in:** Periodic mandatory progress update; comment required if progress regresses
- **Cycle:** A time-bounded OKR period; locked for edits after start, closed with final % and auto-determined status
- **Roles:** Admin, Manager, Colaborador (role-based access control)

## Functional Requirements Summary

| Code | Feature |
|------|---------|
| RF-01–03 | OKR CRUD (creation locked after cycle starts) |
| RF-04–06 | Check-ins, visual timeline, alert system (KR at risk 2+ weeks or 0% monthly progress) |
| RF-07–08 | Executive dashboard and per-owner view |
| RF-09–10 | Cycle closure with auto-status and cycle reports |
| RF-11–12 | RBAC and immutable audit log |

## Non-Functional Requirements

- Dashboard loads < 2s; check-in saves < 1s
- 5000+ concurrent users, multi-tenant ready
- 99% uptime, daily backups
- JWT/OAuth2 auth, bcrypt, HTTPS, rate limiting
- 70%+ backend test coverage
- LGPD (Brazilian data privacy law) compliance

## Architecture Decisions to Preserve

1. **Event Sourcing:** PostgreSQL is the write source of truth — never read directly from it for queries; use MongoDB projections.
2. **CQRS split:** Commands go to the .NET API; queries are served from MongoDB. Do not mix these paths.
3. **Async via RabbitMQ:** Alert generation and report creation must be event-driven, not synchronous API responses. No e-mail service in scope — alerts are internal to the platform only.
4. **OKR edit lock:** OKRs must be uneditable once a cycle is active (RF-02). This is a business rule, not a permission issue.
5. **Immutable audit log:** Changes must be logged immutably (RF-12) — no soft deletes that bypass audit events.