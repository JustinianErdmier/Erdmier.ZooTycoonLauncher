# Milestone 13 — Game Config Tab Rename: Ledger

**Date:** 2026-10-23

**Plan:** [`milestone-13-game-config-tab-rename.md`](../../Plans/Epic-1-MVP/milestone-13-game-config-tab-rename.md) ·
**Branch:** `feat/milestone-13-game-config-tab-rename`

The main window's **INI Config** tab is renamed **Game Config**, so the launcher stops showing users a file-format name. The Options dialogue's *INI editor* group box
becomes *Config editor*. Code identifiers, folders and namespaces containing `IniConfig` keep their names; the tab is still a view of `zoo.ini`.

## Rulings taken without the maintainer

- **R1 — Alt-C in the SDD.** SDD §9.1 lists the tab as Alt-I, but no access key is wired up in the code. With General already on G, the SDD changes to Alt-C (the `C` of
  *Config*). Nothing in the code changes. Cost if wrong: one access-key letter in the SDD, to be changed when access keys are actually wired up.

## Defects found in the plan

None so far.

## Incidents

- **Task 1 had no running-app check.** The computer-use `request_access` call could not resolve the launcher as an application, so the implementer could not drive it
  and took no screenshots.

## Outstanding items

### Before the PR

- Verify in the running app that the Game Config tab header fits on one line and the Options *Config editor* group box reads correctly.
