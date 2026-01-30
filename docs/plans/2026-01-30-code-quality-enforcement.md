# Code Quality Enforcement Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add EditorConfig, Prettier, .NET warnings-as-errors, and enforce all of them in CI.

**Architecture:** Three independent concerns — .NET build strictness (Directory.Build.props), frontend formatting (Prettier), and editor consistency (.editorconfig). CI pipeline updated to enforce Prettier.

**Tech Stack:** EditorConfig, Directory.Build.props, Prettier, eslint-config-prettier, GitHub Actions

---

### Task 1: Add .editorconfig

**Files:**
- Create: `.editorconfig`

**Step 1: Create the .editorconfig file**

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 4

[*.{cs,csx}]
indent_size = 4

[*.{ts,tsx,js,jsx,json,css,html,yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false

[*.{sln,csproj,props,targets}]
indent_size = 2

[Makefile]
indent_style = tab
```

**Step 2: Commit**

```bash
git add .editorconfig
git commit -m "chore: add .editorconfig for consistent formatting"
```

---

### Task 2: Add Directory.Build.props for .NET warnings-as-errors

**Files:**
- Create: `Directory.Build.props`

**Step 1: Create Directory.Build.props in repo root**

```xml
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

**Step 2: Run `dotnet build` and fix any warnings-turned-errors**

```bash
dotnet build
```

Expected: May produce errors from existing warnings. Fix each one before proceeding.

Common fixes:
- Unused variables/parameters → remove or prefix with `_`
- Missing XML doc comments → suppress `CS1591` if not wanted, or add docs
- Nullable reference warnings → fix or annotate

**Step 3: Run tests to confirm nothing broke**

```bash
dotnet test
```

Expected: All tests pass.

**Step 4: Commit**

```bash
git add Directory.Build.props
# also add any files that were fixed
git commit -m "chore: treat warnings as errors across all .NET projects"
```

---

### Task 3: Add Prettier to frontend

**Files:**
- Create: `src/mkat-ui/.prettierrc`
- Modify: `src/mkat-ui/package.json` (add deps + scripts)
- Modify: `src/mkat-ui/eslint.config.js` (add eslint-config-prettier)

**Step 1: Install Prettier and eslint-config-prettier**

```bash
cd src/mkat-ui && npm install --save-dev prettier eslint-config-prettier
```

**Step 2: Create `.prettierrc`**

```json
{
  "singleQuote": true,
  "semi": true,
  "trailingComma": "all",
  "printWidth": 100,
  "tabWidth": 2
}
```

**Step 3: Add npm scripts to package.json**

Add to `"scripts"`:
```json
"format": "prettier --write \"src/**/*.{ts,tsx,css}\"",
"format:check": "prettier --check \"src/**/*.{ts,tsx,css}\""
```

**Step 4: Add eslint-config-prettier to eslint.config.js**

Add import:
```js
import prettierConfig from 'eslint-config-prettier';
```

Add to the config array (after existing extends):
```js
prettierConfig,
```

**Step 5: Run Prettier to format existing code**

```bash
cd src/mkat-ui && npm run format
```

**Step 6: Verify ESLint still passes**

```bash
cd src/mkat-ui && npm run lint
```

**Step 7: Verify format check passes**

```bash
cd src/mkat-ui && npm run format:check
```

Expected: All files formatted, no issues.

**Step 8: Commit**

```bash
git add src/mkat-ui/.prettierrc src/mkat-ui/package.json src/mkat-ui/package-lock.json src/mkat-ui/eslint.config.js
# also add any reformatted source files
git commit -m "chore: add Prettier with eslint-config-prettier"
```

---

### Task 4: Update CI to enforce Prettier

**Files:**
- Modify: `.github/workflows/ci.yml`

**Step 1: Add format:check step to CI frontend job**

In the `frontend` job, add after the Lint step:

```yaml
      - name: Format check
        run: npm run format:check
        working-directory: src/mkat-ui
```

**Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: enforce Prettier format check in CI"
```

---

### Task 5: Update docs

**Files:**
- Modify: `docs/learnings.md`

**Step 1: Add learnings entry**

Add entry about the code quality enforcement setup.

**Step 2: Commit**

```bash
git add docs/learnings.md
git commit -m "docs: add learnings entry for code quality enforcement"
```
