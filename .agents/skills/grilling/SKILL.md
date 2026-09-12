---
name: grilling
description: Grill the user relentlessly about a plan, decision, or idea. Use when the user wants to stress-test their thinking, or uses any 'grill' trigger phrases.
---

Interview the user relentlessly until you reach a shared understanding. Map this as a **design tree**: every decision branches into the decisions that hang off it.

Work the tree in **rounds**. The **frontier** is every decision whose prerequisites are already settled: the questions you can ask _now_ without guessing at answers you haven't heard yet. Ask the whole frontier in one round. Then wait for the answers before the next round.

Collect each round through the agent's **structured question tool** (in Cursor: `AskQuestion`): one tool call per round, one item per frontier question. Chat carries context; the form carries the answers. Do not list numbered questions in the reply for the user to type back.

For each item:

- The prompt is the question (title plus any body they need to choose)
- At least two genuine options; your recommended answer first, marked recommended
- Open-ended decisions still go through the form: recommendation plus distinct alternatives. Custom wording is the form's Other path, not a chat essay

If that tool is unavailable, fall back to this chat shape and wait:

```
❓ **Q1** - **<question title>**: <question body, including choices>

➡️ <your recommended answer>
```

Each round of answers reshapes the tree: settled decisions push the frontier outward and unblock questions that depended on them. Recompute the frontier and ask the next round. A question whose answer depends on another question still open in this round belongs to a _later_ round, not this one.

Finding _facts_ is your job, never the user's. When a frontier question needs a fact from the environment (filesystem, tools, etc.), dispatch a sub-agent to find it; don't ask the user for anything you could look up yourself. Don't block on it: a running exploration is an unsettled prerequisite, so only the questions downstream of it wait for the sub-agent to report; ask the rest of the frontier now. The _decisions_ are the user's: put each to them through the form and wait.

The session is done when the frontier is empty: every branch of the design tree visited, nothing left silently assumed. Do not act on it until the user confirms you have reached a shared understanding.
