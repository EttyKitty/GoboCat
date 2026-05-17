<h1 align="center">
  GoboCat: The Flexible GML Formatter
</h1>

<h4 align="center">
  Try online version of the formatter here: https://ettykitty.github.io/Gobo/
</h4>

> [!WARNING]
> GoboCat is still being actively developed. I'm adding options and changing behavior weekly. If you want stability, use older versions or return to Gobo.

## What is GoboCat?

GoboCat is a **deterministic formatter** for GameMaker Language (GML). It parses your code and reprints it according to rules you control, without relying on line width limits or visual complexity heuristics.

Unlike traditional formatters (Prettier, gofmt, etc.), GoboCat does **not** decide when to break lines based on how long a line is. Instead, you choose which syntactic structures should always "explode" into multiline form (arrays, structs, function arguments, ternaries, binary expressions, etc.). When a structure explodes, it propagates, forcing all related children to explode as well. The result is **predictable, consistent, and diff‑friendly**.

GoboCat is **opinionated about the format of some things**, but it gives you **binary‑choice toggles** for high‑level structural preferences. You enable an option, and GoboCat applies it **everywhere**, no hidden "smart" heuristics, no sudden changes because a variable name grew longer. This means the same code will format identically regardless of the length of identifiers or comments.

**My personal belief**: Formatting should be semantic, logic‑centered, and **ruthlessly consistent**. An array should look the same everywhere in the codebase, not suddenly change because a line happened to exceed 90 characters. Your brain shouldn't have to maintain multiple visual patterns for the same syntactic structure. When a logical group is complex, multiple binaries, ternaries, nested calls, it should be explicitly delimited, without heuristic *ifs* or *buts*.

## What is different in GoboCat vs Gobo?

The original Gobo was fully opinionated: it decided when to break lines based on a single user input - line length. GoboCat is **slightly less opinionated**, it shifts from line length being the main formatting trigger and replaces it with user‑controlled "explosion" toggles.

You, the developer, decide how exploded you want your code to be. GoboCat simply follows those decisions, consistently and without surprises.

> [!WARNING]
> `LineWidth` is still retained as an option for now, but the goal is to abandon it fully. It may be deleted in the future. If you want it, use Gobo, not GoboCat.

## Philosophy

- **Predictability over magic**: No sudden explosions because you've added a few characters to the line.
- **Explicit over implicit**: Formatting rules are toggles you set, not heuristics you guess.
- **Refactor, don’t hide**: If code looks messy after formatting, it *is* messy. GoboCat reveals complexity; it’s up to you to simplify the structure, live with it, or disable options.
- **Diff‑minimal**: Trailing commas, exploded argument lists, and propagation rules make adding, removing, or reordering elements change only the lines they appear on.

By using GoboCat, you agree to let it control the *low‑level* details in exchange for speed, determinism, and smaller git diffs. But *you* retain control over the *high‑level* structural explosion of your code, not an arbitrary visual character limit.

End style debates with your team and save mental energy for what’s important!

## Example

Only some of the options are shown. See the online demo for more.

```js
// Input
x = a and b or c  a=0xFG=1 var var var i := 0 s={d: 5, f: 9}
do begin
;;;;show_debug_message(i)
;;;;i++
end until not i < 10 return
call()

// Output
x = a && b || c;
a = 0xF;
G = 1;
var i = 0;
s = {
    d: 5,
    f: 9,
};
do {
    show_debug_message(i);
    i++;
} until (!i < 10)

return call();
```

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

### Tips

- **Ignore Folders:** Gobo automatically ignores `node_modules`, `extensions`, `.git`, `.svn`, `prefabs`, `bin`, and `obj` folders to keep your scans fast.
- **Automation:** Run `gobo --check .` in your Pull Request pipeline to ensure no unformatted code ever hits your main branch.

## Configuration

> [!WARNING]
> GoboCat uses strict JSON parsing. Ensure that there are no missing commas and all property names are double-quoted.

GoboCat searches for a `.goborc.json` file starting from the directory of the file being formatted and searching up through parent directories. This allows you to have a global configuration at your project root and overrides in specific subdirectories. If no config was found, default options are used.

### Example `.goborc.json`

```json
{
  "width": 90,
  "useTabs": true
}
```

### Options

The following configuration options are available:

| Setting | Default | Description |
| :--- | :--- | :--- |
| `limitWidth` | `false` | Toggle for `maxLineWidth` enforcement. |
| `maxLineWidth` | `90` | The line length that the formatter will wrap on. |
| `useTabs` | `false` | Whether to indent with tabs instead of spaces. |
| `tabWidth` | `4` | Spaces per indentation level (used for line length calculation if `useTabs` is true). |
| `flatExpressions` | `false` | Prevents expressions from wrapping, regardless of `maxLineWidth`. |
| `multilineStructs` | `true` | Forces struct members onto new lines. |
| `multilineArrays` | `true` | Forces array elements onto new lines (ignores 1-length). |
| `multilineTernary` | `false` | Forces conditional (ternary) expressions onto multiple lines (ESLint `multiline-ternary`). |
| `multilineArguments` | `false` | Forces all function arguments onto new lines, regardless of line width. |
| `multilineConstructors` | `false` | Forces all constructor function arguments onto new lines, regardless of line width. |
| `multilineAccessors` | `false` | Forces chained member accessors onto multiple lines when 2+ accessors present. |
| `blankLineAfterBlocks` | `true` | Injects a blank line after `}` if followed by another statement (IDE2003 style). |
| `explicitUndefined` | `false` | Replaces empty arguments in function calls with explicit `undefined` keyword. |

## Important notes

> [!WARNING]
> Don't complain if you've skipped this section.

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

### Line wrapping

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

Only if multiline for any reason (arrays, structs, arguments); stripped from inline.

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
| `==`, `=`, `:=`| `==` or `=` depending on context|
| `!=`, `<>` | `!=`|
| `and`, `&&` | `&&`|
| `or`, `\|\|` | `\|\|`|
| `xor`, `^^` | `^^`|
|`not`, `!` | `!`|
| `!=`, `<>` | `!=`|
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

### Comments

> [!WARNING]
> This behavior is subject to change! JSDoc comment formatting may be added in the future

Doesn't format the content of comments. May only move them a little.
