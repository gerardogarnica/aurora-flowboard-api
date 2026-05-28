---
name: spec-creator
description: This skill should be used when the user provides a user story and asks to define a specification, create a spec, fill in the blanks of a story, or wants to refine feature requirements through guided Q&A. Trigger phrases include "define a spec", "create specification", "I have a user story", "spec out this feature", or "help me define requirements".
---

# Spec Creator

Transform a rough user story into a complete, agreed-upon feature specification through collaborative assumption refinement.

## Purpose

Given a user story, produce a full draft specification by filling in all unstated details. Surface every non-technical and functional assumption made, then iteratively refine any the user disagrees with — one question at a time, with guided options — until the spec reflects the user's intent exactly.

---

## Phase 1 — Draft Specification

When the user provides a user story:

1. **Expand the story** into a complete feature specification. Include:
   - Feature goal and success criteria
   - User flows and edge cases
   - UI/UX behavior (labels, states, validation, empty states, errors)
   - Data requirements (fields, constraints, defaults)
   - Permission and role assumptions
   - Integration points with other features
   - Out-of-scope boundaries

2. **List every assumption** you made that is non-technical or functional — things not explicitly stated in the user story. Number them sequentially.

**Output format:**

```
## Draft Specification
[Full specification text]

---

## Assumptions Made
1. [Assumption one]
2. [Assumption two]
3. [Assumption three]
...

---

Which of these assumptions would you like to change? Reply with the numbers (e.g. "2, 5") or "none" if everything looks good.
```

---

## Phase 2 — Refinement Loop

When the user replies with assumption numbers to revisit:

- Collect the list of rejected assumption numbers.
- Ask about them **one at a time**, in the order given.
- For each question, show:

```
**Progress:** [████████░░] Question X of Y

**Assumption #N:** [restate the original assumption]

**How should this work?**

1. [Option A — concrete alternative]
2. [Option B — concrete alternative]
3. [Option C — concrete alternative]
4. [Option D — concrete alternative]
5. Other — tell me what you have in mind
```

- After each answer, acknowledge it briefly, update your internal spec, and move to the next question.
- When all questions are answered, present the **updated assumption list** and ask: "Are there any remaining assumptions you'd like to revisit, or does everything look good now?"

### Progress Bar Rules

- Use block characters: `█` for completed, `░` for remaining.
- Total width: 10 characters.
- Example for question 3 of 7: `[████░░░░░░] Question 3 of 7`

### Option Generation Rules

- Each option must be **concrete and specific** — not generic placeholders.
- Options should represent meaningfully different approaches, not variations of the same idea.
- At least one option should reflect the most common industry default for this type of decision.
- Option 5 is always **"Other — tell me what you have in mind"**.
- If the user picks "Other", accept their free-text answer without further clarification unless it's genuinely ambiguous.

---

## Phase 3 — Completion

When the user confirms all assumptions are acceptable:

```
✓ Specification locked. I'm ready to generate the full feature specification for [feature name].

Type /spec-creator:generate to produce the final document, or ask me to implement it directly.
```

Then wait for the user's next instruction.

---

## Commands

- `/spec-creator:generate` — Output the final agreed specification as a structured document
- `/spec-creator:reset` — Start over with a new user story

---

## Tone and Style

- Be direct and specific in assumption statements — avoid vague phrases like "standard behavior" or "as expected."
- Keep options short (one sentence each). The user should be able to scan them in under 5 seconds.
- Never ask two questions at once during Phase 2.
- Do not add new assumptions mid-refinement. All assumptions surface in Phase 1.
- After each Phase 2 answer, do NOT re-show the entire spec — just confirm and continue.
