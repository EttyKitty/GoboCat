# GoboCat: Agent Guide

## Overview

Opinionated **GameMaker Language (GML)** formatter. Parse GML to AST, convert to Doc IR (Prettier-style), print formatted code.

---

## Project Structure

```
Gobo.slnx              — Solution file (VS solution workaround)
Gobo/                  — Core library (gobolib)
  Parser/              — GML lexer, parser, comment mapper
  Printer/             — Doc IR types + DocPrinter (Prettier-style layout engine)
    DocPrinter/        — DocPrinter, Indent, PropagateBreaks
    DocTypes/          — All Doc IR node types (Concat, Group, LineDoc, IndentDoc, etc.)
    Utilities/         — String helpers
  SyntaxNodes/         — AST node definitions + base classes
    Gml/               — Specific GML AST node types
      Literals/        — Literal node types (Integer, Decimal, String, etc.)
    GmlExtensions/     — Extended syntax handling (e.g. type annotations)
    Utilities/         — Helper utilities for nodes (DelimitedList, MemberChain, etc.)
  Text/                — SourceText abstraction + ObjectPool
  ConfigFileHandler.cs — Loads .goborc.json config files
  FormatOptions.cs     — All configurable formatting options
  GmlFormatter.cs      — Main entry point: Format(), Check(), FormatFileAsync()
  PrintContext.cs      — Context passed through the printing tree
  StringDiffer.cs      — Difference-finding utility for test assertions
  TextSpan.cs          — Span value type (start/end positions)
Gobo.Cli/              — CLI tool (gobo.exe)
Gobo.Tests/            — xUnit test suite
  FormattingTests.cs   — Regression tests: .test → .expected file pairs
  SampleTests.cs       — Idempotency tests on large sample files
  SourceTextTests.cs   — Tests for SourceText implementation
  Gml/                 — Test fixtures
    FormattingTests/   — .test / .expected / .actual file pairs
    Samples/           — Large sample .test files
Gobo.Benchmarks/       — BenchmarkDotNet benchmarks
Gobo.Playground/       — Blazor WebAssembly online demo
```

---

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Language  | C# 12 (.NET 9.0) |
| Platform  | Native AOT compatible (`IsAotCompatible=true`) |
| Assembly  | `gobolib` for core library |
| CLI       | Uses `DocoptNet` for argument parsing |
| Tests     | xUnit v2 |
| Serialization | System.Text.Json (source-generated) |
| Web demo  | Blazor WebAssembly (AOT compiled) |
| Benchmark | BenchmarkDotNet |

Key `Gobo/Gobo.csproj` settings:
- `TargetFramework`: net9.0
- `ImplicitUsings`: enabled
- `Nullable`: enabled
- `AllowUnsafeBlocks`: true
- JSON uses **source generators** (`FormatOptionsSerializer`). Reflection disabled.

---

## Pipeline: How Formatting Works

1. **Lexing** — `GmlLexer` tokenizes GML.
2. **Parsing** — `GmlParser` creates AST (`GmlSyntaxNode` tree).
3. **Comment Attachment** — `CommentMapper` attaches trivia to AST nodes via binary search.
4. **Printing** — `PrintNode(PrintContext)` returns **Doc IR** tree.
5. **Doc Printing** — `DocPrinter` processes Doc IR. Handles indentation, and line-breaking.
6. **Validation** — Re-parse output. Ensure:
   - Comments printed once.
   - AST hash identical (no semantic changes).
   - Valid GML output.

---

## Core Classes & Responsibilities

### Entry Point: `GmlFormatter` (Gobo/GmlFormatter.cs)

- `Format(string text, FormatOptions)` → `FormatResult`
- `Format(SourceText code, FormatOptions)` → `FormatResult`
- `Check(SourceText code, FormatOptions)` → `bool`
- `FormatFileAsync(string filePath, FormatOptions)` → `Task`

### Format Options: `FormatOptions` (Gobo/FormatOptions.cs)

| Property | Default | Description |
|----------|---------|-------------|
| `UseTabs` | `false` | Indent with tabs vs spaces |
| `TabWidth` | `4` | Spaces per indent level |
| `FlatExpressions` | `false` | Prevent expression wrapping |
| `MultilineStructs` | `true` | Force struct members to new lines |
| `MultilineArrays` | `true` | Force array elements to new lines |
| `MultilineTernary` | `false` | Force multiline ternaries |
| `MultilineArguments` | `false` | Force multiline args |
| `MultilineAccessors` | `false` | Force multiline chained accessors |
| `BlankLineAfterBlocks` | `false` | Insert blank line after `}` |
| `ExplicitUndefined` | `false` | Replace empty args with `undefined` |
| `BraceStyle` | `SameLine` | Brace placement (CLI only; `[JsonIgnore]`) |
| `ValidateOutput` | `true` | Re-parse & validate (`[JsonIgnore]`) |
| `RemoveSyntaxExtensions` | `false` | Strip GML extensions (`[JsonIgnore]`) |
| `GetDebugInfo` | `false` | Include timing/AST/Doc in result (`[JsonIgnore]`) |

### Configuration: `ConfigFileHandler` (Gobo/ConfigFileHandler.cs)

- Search `.goborc.json` up from target directory.
- Strict JSON parsing (source-generated).
- Fallback to `FormatOptions.Default`.

### Doc IR Types: `Gobo/Printer/DocTypes/`

- **`Doc`** — Base with factory methods: `Concat()`, `Group()`, `Indent()`, `HardLine`, `Line`, `SoftLine`.
- **`Group`** — Fits on one line (`Flat`) or breaks (`Break`).
- **`ConditionalGroup`** — Try layout alternatives in order.
- **`IndentDoc`** — Increase indent level.
- **`Align`** — Align to column.
- **`Fill`** — Greedy line filling.
- **`ForceFlat`** — No line breaks.
- **`IfBreak`** — Content depends on group break state.
- **`LineDoc`** — Soft/normal/hard lines.
- **`HardLine`** — Force newline.
- **`StringDoc`** — String literal.
- **`LiteralLine`** — Preserved newline.
- **`AlwaysFits`** — For comments.
- **`EndOfLineComment` / `InlineComment`** — Comment wrappers with ID tracking.
- **`Region`** — `#region` / `#endregion` markers.

### Doc Printer Options: `DocPrinterOptions` (Gobo/Printer/DocPrinterOptions.cs)

- Bridge `FormatOptions` → `DocPrinter`.
- `TrimInitialLines` (default `true`) strips leading newlines.

### Doc Printer: `DocPrinter` (Gobo/Printer/DocPrinter/DocPrinter.cs)

- Stack-based processing loop.
- Tracks printed comments for validation.

---

## AST Node Hierarchy

All nodes inherit `GmlSyntaxNode`:
```
GmlSyntaxNode (Gobo/SyntaxNodes/GmlSyntaxNode.cs)
├── PrintNode(PrintContext ctx) → Doc  // Each node produces its Doc IR here
├── Children: List<GmlSyntaxNode>      // Child node tree
├── Comments: List<CommentGroup>       // Attached comments
├── Span: TextSpan                     // Source position
└── Parent: GmlSyntaxNode?             // Parent reference
```

### AST Node Types (Gobo/SyntaxNodes/Gml/)

**Statements:**
- `Block`, `IfStatement`, `WhileStatement`, `DoStatement`, `ForStatement`, `RepeatStatement`, `WithStatement`, `SwitchStatement`, `SwitchBlock`, `SwitchCase`
- `ReturnStatement`, `BreakStatement`, `ContinueStatement`, `ExitStatement`, `ThrowStatement`, `DeleteStatement`
- `TryStatement`, `CatchProduction`, `FinallyProduction`
- `FunctionDeclaration`, `EnumDeclaration`, `EnumBlock`, `EnumMember`
- `GlobalVariableStatement`, `VariableDeclarationList`, `VariableDeclarator`
- `MacroDeclaration`, `DefineStatement`, `RegionStatement`, `Document`

**Expressions:**
- `BinaryExpression`, `UnaryExpression`, `AssignmentExpression`, `ConditionalExpression`
- `CallExpression`, `ArgumentList`, `ArrayExpression`, `ArrayIndexExpression`
- `StructExpression`, `StructProperty`, `MemberDotExpression`, `MemberIndexExpression`
- `NewExpression`, `ConstructorClause`, `ParenthesizedExpression`, `Identifier`
- `Parameter`, `ParameterList`, `IncDecStatement`, `UndefinedArgument`
- `TemplateExpression`, `TemplateLiteral`, `TemplateText`

**Literals:**
- `Literal` — Base literal node.
- `IntegerLiteral`, `DecimalLiteral`, `StringLiteral`, `VerbatimStringLiteral`, `UndefinedLiteral`

**Extensions:**
- `TypeAnnotation`

**Utilities:**
- `DelimitedList` — Comma-separated formatting.
- `MemberChain` — Chained access grouping.
- `RightHandSide` — Assignment formatting.
- `Statement` — Statement-level helpers.

---

## Comment System

Comments parsed as **trivia tokens**:
- `CommentMapper` uses **binary search** to find `EnclosingNode`, `PrecedingNode`, `FollowingNode`.
- Placement:
  - **OwnLine** — Attached as Leading to following node.
  - **EndOfLine** — Attached as Trailing to preceding node.
  - **Remaining** — Inline; checked as leading to following node.
- Types: `Leading`, `Trailing`, `Dangling`.
- `// fmt-ignore` prevents formatting for statement below.

---

## Testing

### Test Framework: xUnit

**FormattingTests** (`Gobo.Tests/FormattingTests.cs`):
- Compare `.test` input to `.expected` in `Gml/FormattingTests/`.
- Verify idempotency (second pass).
- Write `.actual` on failure.

**SampleTests** (`Gobo.Tests/SampleTests.cs`):
- Test stability on large files in `Gml/Samples/`. No `.expected` comparison.

**SourceTextTests** (`Gobo.Tests/SourceTextTests.cs`):
- Test `SourceText` abstraction.

**Convention:**
- `*.test` — Input GML.
- `*.expected` — Expected output.
- `*.actual` — Failure output (gitignored).

---

## Key Design

1. **AST-based** — Full AST before printing. Enables structural transforms.
2. **Prettier-style Doc IR** — Layout decisions made during printing via group fitting.
3. **Idempotency** — `format(format(input)) == format(input)`.
4. **Comment preservation** — Placement relative to AST nodes.
5. **Validated output** — Re-parse to verify AST equivalence.
6. **SourceText abstraction** — Unified text operations.
7. **AOT compatible** — No reflection; source-generated JSON.

---

## Common Patterns for Modifications

### Adding a new AST node

1. Create class in `Gobo/SyntaxNodes/Gml/` inheriting `GmlSyntaxNode`.
2. Implement `PrintNode(PrintContext ctx) → Doc`.
3. Register in `GmlParser`.
4. Add tests in `Gobo.Tests/Gml/FormattingTests/`.
5. Update `AGENTS.md` if needed.

### Adding a new format option

1. Add property to `FormatOptions.cs`.
2. Use in `PrintNode()` via `PrintContext`.
3. Update `DocPrinterOptions` only if affecting engine (width, indent).
4. Add tests with `.goborc.json`.
5. Update `README.md` if needed.
6. Update `AGENTS.md` if needed.

### Modifying formatting behavior

1. Locate `PrintNode()` in `Gobo/SyntaxNodes/Gml/`.
2. Modify Doc IR construction.
3. Run `dotnet test Gobo.Tests`.
4. Update `README.md` if needed.
5. Update `AGENTS.md` if needed.

---

## Useful Commands

```bash
# Ensure the lib compiles
dotnet build Gobo

# Run all tests
dotnet test Gobo.Tests
```
