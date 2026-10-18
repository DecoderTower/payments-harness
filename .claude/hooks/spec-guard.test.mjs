// The spec guard is a constraint; this checks it is not a vacuous one.
// Run with: node --test .claude/hooks/

import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const hook = fileURLToPath(new URL("./spec-guard.mjs", import.meta.url));

function guard(filePath) {
  const payload = filePath === undefined ? {} : { tool_input: { file_path: filePath } };
  const result = spawnSync("node", [hook], { input: JSON.stringify(payload), encoding: "utf-8" });
  assert.equal(result.status, 0, result.stderr);
  return result.stdout ? JSON.parse(result.stdout).hookSpecificOutput : null;
}

test("asks before writing a spec", () => {
  assert.equal(guard("/repo/docs/specs/stripe-deposit-processing.md")?.permissionDecision, "ask");
});

test("asks before writing a decision record, on Windows paths too", () => {
  assert.equal(guard("D:\\repo\\docs\\decisions\\006-new.md")?.permissionDecision, "ask");
});

test("asks for a repository-relative path", () => {
  assert.equal(guard("docs/specs/new-flow-processing.md")?.permissionDecision, "ask");
});

test("asks before writing the rest of the durable context", () => {
  for (const path of ["/repo/docs/Architecture.md", "/repo/docs/Factory.md", "/repo/AGENTS.md"]) {
    assert.equal(guard(path)?.permissionDecision, "ask", path);
  }
});

test("asks before writing the harness itself", () => {
  for (const path of [
    "/repo/.claude/hooks/stop-gate.mjs",
    "/repo/.claude/skills/implement-spec/SKILL.md",
    "/repo/.claude/settings.json",
    "/repo/.github/workflows/ci.yml",
    "D:\\repo\\.claude\\worktrees\\factory\\.claude\\hooks\\spec-guard.mjs",
  ]) {
    assert.equal(guard(path)?.permissionDecision, "ask", path);
  }
});

test("allows the templates", () => {
  assert.equal(guard("/repo/docs/specs/TEMPLATE.md"), null);
  assert.equal(guard("/repo/docs/decisions/TEMPLATE.md"), null);
});

test("allows code, local settings, and other documents", () => {
  for (const path of [
    "/repo/src/Payments.Api/Program.cs",
    "/repo/README.md",
    "/repo/notdocs/specs/x.md",
    "/repo/.claude/settings.local.json",
    "D:\\repo\\.claude\\worktrees\\factory\\src\\Payments.Api\\Program.cs",
  ]) {
    assert.equal(guard(path), null, path);
  }
});

test("allows a payload without a file path", () => {
  assert.equal(guard(undefined), null);
});
