---
name: implement-direct
description: "Implement a spec or ticket list by writing production code first, without test-driven development. Use whenever the user asks for implement-direct, direct implementation, skip TDD, skip red-green, or to code from the task list rather than writing failing tests first."
disable-model-invocation: true
---

# Implement Direct

Implement the work described by the spec or tickets by writing the production change first. Do not write a failing test in order to know what to build. Do not run a red → green loop.

The spec and acceptance criteria are the specification.

## Process

1. **Gather.** Read the spec or tickets the user pointed at. If there is a ticket list, that list is the work. Do not invent extra tickets.

2. **Frontier.** A ticket is ready when every ticket that blocks it is done. Work ready tickets in order.

3. **One ticket.** Restate the end-to-end behavior. Implement only enough production code to satisfy that ticket's acceptance criteria. Do not build features that belong to later tickets. If a criterion is ambiguous, ask.

4. **Check.** Typecheck as you go. Run a focused check if the project has one. If existing tests fail because of the change, make them pass or update them to match the specified behavior. Add new tests only when the ticket's criteria require them, and only after the production behavior exists.

5. **Close.** Check off criteria that now hold. Mark the ticket done. If a criterion cannot be honestly checked, leave it open and stop. Then take the next unblocked ticket.
