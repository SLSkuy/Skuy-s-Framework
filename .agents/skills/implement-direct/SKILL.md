---
name: implement-direct
description: "Implement a spec or ticket list directly by writing production code only. Never write, modify, or run tests, even when the spec or acceptance criteria require tests. Use whenever the user asks for implement-direct, direct implementation, skip TDD, skip tests, or to code from the task list without test work."
disable-model-invocation: true
------------------------------

# Implement Direct

Implement the work described by the spec or tickets by writing **production code only**.

**Never write, modify, or run tests.** This rule applies even when the spec, ticket, acceptance criteria, or existing workflow explicitly requires tests.

Do not write a failing test, do not perform a red → green loop, and do not add or update tests after implementing the production behavior.

The spec and acceptance criteria are the specification.

## Process

1. **Gather.** Read the spec or tickets the user pointed at. If there is a ticket list, that list is the work. Do not invent extra tickets.

2. **Frontier.** A ticket is ready when every ticket that blocks it is done. Work ready tickets in order.

3. **One ticket.** Restate the end-to-end behavior. Implement only enough **production code** to satisfy that ticket's acceptance criteria. Do not build features that belong to later tickets. If a criterion is ambiguous, ask.

4. **Check.** Inspect the implementation and relevant production-code call sites as needed. You may run production-code checks such as compilation, typechecking, linting, or other non-test validation when available and useful.

   **Do not write, modify, or run tests under any circumstances.**

   If existing tests would need to change because of the production change, leave them untouched. Do not make test changes part of the implementation.

5. **Close.** Check off criteria that can be honestly verified from the production implementation and available non-test checks. Mark the ticket done when the production work is complete. If a criterion cannot be honestly checked without tests, do not create or run tests; report that the criterion could not be verified through this implementation-only workflow.

   Then take the next unblocked ticket.
