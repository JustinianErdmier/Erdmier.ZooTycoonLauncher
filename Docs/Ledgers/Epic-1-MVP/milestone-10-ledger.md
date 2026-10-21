# Milestone 10 — INI Recovery and Read-only Detection: Ledger

**Date:** 2026-10-21

**Plan:** [`milestone-10-ini-recovery-and-read-only-detection.md`](../../Plans/Epic-1-MVP/milestone-10-ini-recovery-and-read-only-detection.md) ·
**Spec:** [`milestone-10-ini-recovery-and-read-only-detection-design.md`](../../Specs/Epic-1-MVP/milestone-10-ini-recovery-and-read-only-detection-design.md) ·
**Branch:** `feat/milestone-10-ini-recovery-and-read-only-detection`, cut from `main` at `db73f62`

The spec was written from the SDD at the maintainer's request, without brainstorm questions; the maintainer accepted its calls C1–C10 before the plan was written.
Executed with subagent-driven development: Sonnet implementers, Opus task reviewers, one Opus whole-branch review and one fix wave with its re-review. All nine
tasks completed; Tasks 6 and 8 each took one fix round. Final position: clean build with 0 warnings; every test project green (Domain 139, Application 197,
Infrastructure 58, Desktop 182, Architecture 13).

---

## Rulings taken without the maintainer

1. **Unsigned commits**, continuing the earlier instruction while the GPG agent stays locked. *Cost:* unsigned history; re-sign by rebase if wanted.
2. **R1 — The No INI placeholder takes `ILogger<IniPlaceholderViewModel>`** where the plan wrote a non-generic `ILogger`: the project idiom and its test helper.
   *Cost:* none.
3. **R2 — `Ini.NotAnIni` names the file, not the full path** (`Path.GetFileName`); the plan used `{fileName}` without defining it. *Cost:* a longer message if
   the full path was wanted.
4. **Commit `967233b` keeps its "Claude Sonnet 5.5" co-author line.** A Sonnet implementer wrote it, so it is accurate; rewriting a mid-branch commit was not
   worth it. *Cost:* one commit's trailer differs from the rest.
5. **`IniErrors.AlreadyExistsCode`** was added in Task 6 and both the INI tab and the Fix dialogue compare against it, rather than a third copy of the literal.
   *Cost:* one small cross-task edit.
6. **The INI tab's Create / Locate are disabled when `zoo.exe` is missing**, as the Fix dialogue already does (SDD §7.2.5). *Cost:* a user with neither file
   fixes the EXE first.
7. **A vanished installation (`NotFound`) from the No INI box re-boots like `Ini.AlreadyExists`**; the pointed boot falls back to normal resolution.
   *Cost:* none.
8. **A row-update failure after `zoo.ini` was written is logged and swallowed**, like the snapshot step; the next boot re-verifies `HasIni` from disk.
   *Cost:* the row's flag lags by one boot.
9. **C10's sentence became "Use the INI Config tab to create or locate zoo.ini."** — "…locate one." read as dangling after two other sentences. The spec and SDD
   quotes follow. *Cost:* one sentence of copy.
10. **The in-app check used a placeholder `zoo.exe`.** Copying from `SampleInstallations/` was blocked by the session sandbox, so the scratch installation held an
    empty `zoo.exe` (the launcher only checks it exists; the game was never launched) and a hand-written source `.ini` for Locate. *Cost:* none for this
    milestone's checks.
11. **The controller corrected the spec's §5.3 itself** after the fix wave's re-review, so it records the `NotFound` re-boot and the `zoo.exe` gate (rulings 6
    and 7). The executed plan's task text keeps its original wording as a record. *Cost:* an unreviewed two-line docs edit.

## Defects found in the plan

- **R1 / R2:** Task 5 named a non-generic logger and Task 3 used an undefined `{fileName}` (rulings 2 and 3).
- **Task 8's amendment list missed SDD §7.7 and §8.2**, which still put Create Default under the transactional overwrite ordering and so contradicted the new §7.4.
  Caught in Task 8's review; fixed in its fix round.
- **Task 5 asked for two new trailing constructor parameters** after an optional one, which C# does not allow; the required one went before the optional ones.

## Incidents

- **A Task 8 implementer left a background shell running** (a malformed heredoc). The controller stopped it; later dispatches repeated the foreground-only rule.
- **`open_application` started a second launcher instance** during the in-app check; the controller closed it.
- **Typing long paths in the in-app check used the clipboard**, overwriting the maintainer's clipboard contents.
- **Two implementers captured no failing run** (Tasks 5 and, partly, 6); their tests were written against the finished code and reviewed as sound.

## Verification

- Clean build, 0 warnings; all five test projects pass.
- **Checked in the running app** (21 Oct 2026, 14:32–14:35) on a scratch installation registered for the purpose and deleted through the launcher afterwards:
  - Fix dialogue: *Fix INI* shows the new copy and `Create` / `Locate…`; `Create` wrote a 917-byte default `zoo.ini` and flipped the group to *INI present*.
  - General tab, Cannot Play with only the INI missing, points at the INI Config tab.
  - No INI box: the copy and both buttons; the picker is titled *Locate zoo.ini*, starts in the installation folder and filters to INI files. Locate copied the
    source, and the window re-booted onto the INI tab showing its values, with the earlier Create's settings in history.
  - With `zoo.ini` marked Read-only, the INI tab shows the file strip, and Save reports the read-only explanation instead of "access denied".
  - No temp or probe file was left in the folder.
- **Not driven:** a folder-level read-only installation (needs an ACL change) and the `Ini.AlreadyExists` race — both covered by unit and integration tests.

---

## Outstanding items

### Polish, whenever the area is next touched

- The access-denied → probe → `Ini.ReadOnly` / `Ini.WriteFailed` mapping is written twice (`IniWritePipeline`, `IniRecoverySteps`).
- The probe file can be left if its write creates the file and then throws (the orphan cleanup's prefix match removes it later); a cancelled probe reports
  `Unknown`.
- `InstallationWriteProbe` is the only Infrastructure class on Microsoft's `ILogger<T>`; the rest use Serilog's.
- Locate's section check misses a header followed by a comment (`[user] ; note`) and a UTF-8 BOM before the first header.
- The No INI body copy is written both in the XAML (for the monospace `zoo.ini`) and in the view model's constant.
- Untested: `TryCreateAsync`'s race-at-move branch; a failed Locate in the Fix dialogue. The Fix dialogue's re-probe passes `CancellationToken.None`.
- `IniTestData.Probe` lives in the INI test folder but the boot tests use it.

### Not planned

- **The main window shrinks to the No INI box's height** (`MainWindow` sizes to content; the placeholder was already top-aligned). Pre-existing.
- **Deleting the open installation from the Installation Manager leaves its tabs on screen**, against SDD §7.2.4's re-resolve rule. Pre-existing; the No INI box
  now copes (ruling 7).
- A 32-bit launcher process would be subject to UAC file virtualisation and the probe would report Writable; the shipped x64 build is not. Belongs with DW-36.
- Copy From Another Installation — DW-40. Clearing a read-only attribute automatically — out of scope (spec §2).
