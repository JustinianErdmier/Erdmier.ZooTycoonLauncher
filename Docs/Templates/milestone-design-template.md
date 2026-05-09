# Milestone Name — Design

[//]: # (Notes for the following header section: Status can either be Draft or Approved. Only add the 'open questions' paranthetical if there are open questions.)

- **Date:** YYYY-MM-DD
- **Status:** [Draft | Approved] <(open questions [pending | resolved] YYYY-MM-DD)>
- **Companion plan:** [`YYYY-MM-DD—milestone-name.md`](./YYYY-MM-DD—milestone-name.md)
- **SDD reference:** [`SoftwareDesignDocument.md`](../SoftwareDesignDocument.md) — §X.Y SectionTitle, §X.Z SectionTitle

---

## 1. Goal

[//]: # (Replace with one or two sentences describing what this milestone achieves and how it fits the broader project arc.)

---

## 2. Scope

### In scope

This milestone delivers:

[//]: # (Replace with a list of the following: 1. component inventory: all concrete artifacts — interfaces, services, ViewModels, files — exist after this milestone that didn't before; and then 2. feature coverage: what those delivered artifacts actually do and don't handle — behavior and requirement boundaries, not component existence.)

### Out of scope

It does **not** deliver:

[//]: # (Briefly note why each item is deferred e.g. dependency, separate plan, future milestone, etc. )

[//]: # (Only add the 'deferred to...' paranthetical if there is a plan for the deferred item)

- **Item** — reason for deferral. *(deferred to `YYYY-MM-DD-plan-name.md`)*

---

## 3. Key design decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | …        | …         |
| 2 | …        | …         |

---

## 4. Architecture

[//]: # (Describe service/component contracts and how they interact. Break into subsections per service or layer.)

### 4.1 `IFooService` / `FooService`

[//]: # (Interface signature, constructor dependencies, key methods, and any notable invariants.)

### 4.2 DI wiring

[//]: # (List registrations added to the composition root e.g. App.axaml.cs.)

---

## 5. Data models

[//]: # (Describe new or modified models, their fields, types, and validation rules. Reference SDD §7 where applicable.)

---

## 6. Error handling

[//]: # (Map anticipated failure conditions to recovery strategies or user-facing messages. Reference SDD §10.)

| Condition | Handling strategy |
|-----------|-------------------|
| …         | …                 |

---

## 7. Open questions

[//]: # (If there are no open questions, don't include this section.)

[//]: # (If there are any open questions, list them here. Always try to ask either yes/no questions or provide options. Then suggest which answer you would choose and explain why.)

[//]: # (Update this section once all questions are resolved and update the Status line above.)

| # | Question | Recommendation | Decision |
|---|----------|----------------|----------|
| 1 | …        | …              | …        |