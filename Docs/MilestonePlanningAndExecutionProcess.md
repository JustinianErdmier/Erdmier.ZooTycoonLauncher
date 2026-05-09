1. Start by planning a milestone with referencing the SDD (D:\Source\Personal\Erdmier.ZooTycoonLauncher\Docs\SoftwareDesignDocument.md).
2. Use the superpowers:writing-plans skill to create a design document for the milestone.
    1. Path: D:\Source\Personal\Erdmier.ZooTycoonLauncher\Docs\Plans
    2. Naming Convention: {YYYY}-{MM}-{DD}—{milestone name}—design.md
        1. If the `milestone name` has spaces, replace them with hyphens
    3. Read the milestone template for guidance on how to structure the design document
        1. Path to template: D:\Source\Personal\Erdmier.ZooTycoonLauncher\Docs\Templates\milestone-design-template.md
3. After the design document is finished, have the user review it, answer any open questions, and tweak it as needed.
4. Once the design document is approved, use the superpowers:writing-plans skill to create a plan document for the milestone.
    1. Path: D:\Source\Personal\Erdmier.ZooTycoonLauncher\Docs\Plans
    2. Naming Convention: {YYYY}-{MM}-{DD}—{milestone name}.md
        1. If the `milestone name` has spaces, replace them with hyphens
5. After the plan document is finished, have the user review and tweak it as needed.
6. Always execute plan documents inline via the superpowers:executing-plans skill.
7. When validating the task completion, clean the solution before building it.
    1. Always use PowerShell to run commands — NEVER use Bash.
8. After a task is completed, commit the changes to VCS. Use the conventional commit format and conventional commit gitmojis for commit messages.