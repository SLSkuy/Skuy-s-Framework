# Phase 4: Module Layer

## Goal

Convert entity capabilities into attachable modules.

## Tasks

- movement module
- animation module
- interaction module
- health module
- camera target module
- physics proxy module

## Special Rules

- animation must be detachable
- animation reads entity state only
- modules must be enable/disable friendly by role

## Success Criteria

- abilities are assembled by modules
- entity core stays thin
- animation and additional gameplay modules can be plugged in later without core rewrites
