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

test("allows the templates", () => {
  assert.equal(guard("/repo/docs/specs/TEMPLATE.md"), null);
  assert.equal(guard("/repo/docs/decisions/TEMPLATE.md"), null);
});

test("allows code and other documents", () => {
  assert.equal(guard("/repo/src/Payments.Api/Program.cs"), null);
  assert.equal(guard("/repo/docs/Architecture.md"), null);
  assert.equal(guard("/repo/notdocs/specs/x.md"), null);
});

test("allows a payload without a file path", () => {
  assert.equal(guard(undefined), null);
});
