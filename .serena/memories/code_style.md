# Code Style & Conventions

## Formatting (from .editorconfig)
- **Indentation**: 4 spaces (soft tabs)
- **Line endings**: CRLF
- **Braces**: Allman style for methods and types (new line); K&R for control flow (else/catch/finally on same line as closing brace)
- **Usings**: System.* first, sorted alphabetically

## Naming
- Types, properties, methods: PascalCase
- Interfaces: prefix with `I`
- Private fields: `_camelCase` prefix
- Prefer `var` everywhere

## Preferences
- Expression-bodied members preferred for constructors, methods, properties
- Prefer object initializers
- Prefer pattern matching over `as` with null check
- Prefer `default` over `default(T)`
- Prefer inline `out` variable declarations
- Prefer curly braces even for single-line blocks
- No `this.` qualification

## Modifiers Order
public, private, internal, protected, static, readonly, virtual, override, new, async, abstract, sealed
