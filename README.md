<h1 align="center">
  GoboCat: The Flexible GML Formatter
</h1>

<h4 align="center">
  Try online version of the formatter here: https://ettykitty.github.io/GoboCat/
</h4>

> [!WARNING]
> GoboCat is in active development. Options and behaviors change weekly. For stability, use older versions or the original Gobo.

## What is GoboCat?

GoboCat is a **deterministic formatter** for GameMaker Language (GML). Unlike traditional formatters (Prettier, gofmt) that wrap lines based on length limits, GoboCat relies on user-controlled structural rules (aka "ESLint-light").

Formatting is semantic, logic-centered, and consistent. The same code formats identically regardless of identifier lengths or comments, ensuring your brain processes a consistent visual pattern for each syntactic structure.

### Philosophy

- **Refactor, don't hide**: GoboCat exposes complex code structures rather than masking them behind complex wrapping logic.
- **User controlled**: You control what expand into multiline forms, not character count.
- **Consistent**: No hidden heuristics or sudden layout shifts when character count changes.
- **Diff-minimal**: Trailing commas and exploded lists limit git diff noise.

### GoboCat vs Gobo

The original Gobo triggers line breaks based on a maximum line length. GoboCat shifts from line length as the primary formatting trigger, replacing it with explicit multiline toggles.

> [!WARNING]
> `LineWidth` is retained temporarily but is deprecated. If your workflow requires strict line-width wrapping, use Gobo instead of GoboCat.

## Examples

Check https://ettykitty.github.io/GoboCat/

## Usage

### Basic Commands

**Format a single file:**
```bash
gobo ./scripts/PlayerMovement.gml
```

**Format an entire directory (recursive):**
```bash
gobo ./src/scripts
```

**Check if files are formatted (CI/CD):**
Use this in GitHub Actions or build scripts. It will return a non-zero exit code if any files need formatting without actually changing them.
```bash
gobo --check ./src
```

### Options Reference

| Option | Description |
| :--- | :--- |
| `-h`, `--help` | Show all available commands. |
| `--check` | Validates formatting. Does **not** write changes to files. |
| `--fast` | Skips heavy validation. Use this if you have thousands of files and trust the output. |
| `--write-stdout` | Prints the formatted code to the terminal instead of saving to the file. |
| `--skip-write` | Dry run. Processes everything but doesn't touch your files. |

## Configuration

> [!WARNING]
> JSON is strict. Ensure that there are no missing commas and all property names are double-quoted.

GoboCat searches for a `.goborc.json` file starting from the directory of the file being formatted and searching up through parent directories. This allows you to have a global configuration at your project root and overrides in specific subdirectories. If no config was found, default options are used.

### Example `.goborc.json`

```json
{
  "useTabs": false,
  "tabWidth": 4
}
```

### Options

The following configuration options are available:

| Setting | Default | Supported | Description |
| :--- | :--- | :--- | :--- |
| `limitWidth` | `false` | `bool` | Toggle for `maxLineWidth` enforcement. |
| `maxLineWidth` | `90` | `int` | The line length that the formatter will wrap on. |
| `useTabs` | `false` | `bool` | Whether to indent with tabs instead of spaces. |
| `tabWidth` | `4` | `int` | Spaces per indentation level (used for line length calculation if `useTabs` is true). |
| `flatExpressions` | `false` | `bool` | Prevents expressions from wrapping, ignoring `maxLineWidth`. |
| `multilineStructs` | `true` | `bool` | Expands struct members onto new lines. |
| `multilineArrays` | `true` | `bool` | Expands array elements onto new lines when >1. |
| `multilineTernary` | `false` | `bool` | Expands conditional (ternary) expressions onto new lines (ESLint `multiline-ternary`). |
| `multilineArguments` | `0` | `0 (Never), 1 (Always), 2 (Smart)` | `Always` expands function arguments onto new lines when >1. `Smart` checks if one of the arguments is "complex" (not a `var`, `literal`, `accessor`). |
| `multilineConstructors` | `false` | `bool` | Expands all constructor function arguments onto new lines. |
| `multilineAccessors` | `false` | `bool` | Expands chained member accessors onto new lines when >1. |
| `blankLineAfterBlocks` | `true` | `bool` | Injects a blank line after `}` if followed by another statement (IDE2003 style). |
| `explicitUndefined` | `false` | `bool` | Replaces empty arguments in function calls with explicit `undefined` keyword. |

## Important notes

> [!WARNING]
> Don't complain if you've skipped this section.

### Ignored folders

GoboCat automatically ignores `node_modules`, `extensions`, `.git`, `.svn`, `prefabs`, `bin`, and `obj` folders.

### `#macro`

GoboCat cannot parse code that relies on macro expansion to be valid. Any standalone expression will be formatted with a semicolon, even if the expression is a macro.
```js
THESE_MACROS;
ARE.VALID;
BECAUSE_THEY_ARE_EXPRESSIONS()
```

### Ignored code

Write `// fmt-ignore` above a piece of code to prevent GoboCat from formatting it.
```js
// fmt-ignore
x := begin /*I like my structs this way*/ end
```
If your code ~~abuses macros~~ requires expanded macros to be valid, place the macros inside a block starting with `// fmt-ignore` to preserve them.

Note that the ignored code must still be valid GML.
```js
// fmt-ignore
{
    ABUSE_MACROS
    IN_THIS
    BLOCK
}

OTHERWISE_I_WILL;
ADD_SEMICOLONS;

// fmt-ignore
{
    // Won't work!
    invalid_syntax(
}
```

## How it works

GoboCat is written in C# and compiles to a self-contained binary using Native AOT in .NET 8.

It uses a custom GML parser to generate an Abstract Syntax Tree (AST). This tree is then converted into an intermediate "Doc" format (adapted from [CSharpier](https://github.com/belav/csharpier) and Prettier) to handle line-wrapping logic and comment placement.

The parser is designed to only accept valid GML (with a few exceptions). There is no officially-documented format for GML's syntax tree, so GoboCat uses a format similar to JavaScript.

GoboCat focuses on readability, consistent formatting, and reducing Git diffs. The code style is designed with these goals in mind.

### Semicolons

At the end of:
- Assignments
- Function calls
- Increment/decrement statements (i.e. `x++`)
- Control flow statements (i.e. `return`, `throw`, etc.)
  - Expression statements (any expression that is not part of a statement)

```js
// semicolon behavior
x = 123;
call();
x++;
--y;
foo;
```

### Control flow structures

Control flow structures like `if`, `with` and `repeat` are always formatted with parentheses and braces:
```js
// before
if true return

// after
if (true) {
    return;
}
```

### Empty lines

GoboCat attempts to preserve empty lines between statements, following these rules:
- Multiple empty lines are collapsed into a single empty line.
- Empty lines at the start and end of blocks (and whole files) are removed.
- Files always end with a single newline.
- Top-level functions and static functions are always surrounded by empty lines.

### Line endings

GoboCat enforces LF line endings (`\n`).

### Arguments

GoboCat enforces a space before each non-empty argument:
```js
// before
call(foo,bar);

// after
call(foo, bar);
```

### Empty arguments

In GML, empty arguments are implicitly passed to functions as `undefined`. GoboCat strips trailing empty arguments but preserves internal ones, trimming whitespace:
```js
// before
call(,,foo,);
call(, /*comment*/ ,)

// after
call(,, foo);
call(/*comment*/);

```

With `explicitUndefined` enabled, internal empty arguments are replaced with `undefined`:
```js
// before
call(,,foo,);

// after
call(undefined, undefined, foo);
```

### Trailing commas

Only if multiline (arrays, structs, arguments); stripped from inline.

```js
// multiline
s = {
    d: 5,
    f: 9,
};

// inline
s = {d: 5, f: 9};
```

### Array Accessors

Enforces modern (JS-style) chained accessors.

```js
// before
array[0, 2];

// after
array[0][2];
```

### Operators and Braces

Standardizes symbols according to the following table:

| Symbol| Preferred Form|
| ----------- | ----------- |
| `==`, `=`, `:=`| `==` for comparison and `=` for assignment|
| `!=`, `<>` | `!=`|
| `and`, `&&` | `&&`|
| `or`, `\|\|` | `\|\|`|
| `xor`, `^^` | `^^`|
|`not`, `!` | `!`|
| `begin`, `{` | `{`|
| `end`, `}` | `}`|
|`mod`, `%` | `%`|

### Redundant parentheses

Removes redundant parentheses around certain expressions:
```js
// before
var foo = ( -(((a + b))) + -(c) );

// after
var foo = -(a + b) + -c;
```

### Line wrapping (deprecated)

By default, GoboCat attempts to print expressions and statements in a single line if they fit. This goes for function calls, structs, arrays, and comma-separated `var`/`static`/`globalvar` declarations.  Blocks are never printed on a single line unless they are empty.

If a list of items is too long to fit in a single line, each item is printed on its own line. The exception to this rule is function calls --- if a struct or function exists at the end of an argument list, GoboCat tries to break on the final argument first:
```js
// default behavior
call(
    x______________,
    y________________,
    z__________________,
    w_____________
)

// break on last argument in method()
call(x____________, y___________, method({closure: self}, function() {
    return;
}));
```

### Comments

> [!WARNING]
> This behavior is subject to change! JSDoc comment formatting may be added in the future

Doesn't format the content of comments. May only move them around a little.
