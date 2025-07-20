# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

[... existing content remains unchanged ...]

## Key Development Patterns

### Essential Development Practices
- **Always use strongly-typed DTOs instead of dynamic objects**
  - Provides compile-time type safety
  - Enables better tooling support and refactoring
  - Prevents runtime type-related errors
  - Improves API contract clarity and documentation
- **Always use secure way to identify admin, and not depend on queries (from frontend)**
  - Prevents potential security vulnerabilities
  - Ensures admin access is verified through robust authentication mechanisms

[... rest of existing content remains unchanged ...]