// The harness guard is a constraint; this checks it is not a vacuous one.
// Run with: node --test .claude/hooks/*.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const hook = fileURLToPath(new URL("./harness-guard.mjs", import.meta.url));

function guard(command) {
  const payload = command === undefined ? {} : { tool_input: { command } };
  const result = spawnSync("node", [hook], { input: JSON.stringify(payload), encoding: "utf-8" });
  assert.equal(result.status, 0, result.stderr);
  return result.stdout ? JSON.parse(result.stdout).hookSpecificOutput.permissionDecision : null;
}

test("asks before deleting a hook", () => {
  assert.equal(guard("rm .claude/hooks/stop-gate.mjs"), "ask");
  assert.equal(guard("git rm -r .claude/hooks"), "ask");
  assert.equal(guard("Remove-Item .claude\\hooks\\spec-guard.mjs -Force"), "ask");
});

test("asks before overwriting the settings or durable context", () => {
  assert.equal(guard("echo '{}' > .claude/settings.json"), "ask");
  assert.equal(guard("sed -i 's/ask/allow/' .claude/hooks/spec-guard.mjs"), "ask");
  assert.equal(guard("mv docs/specs/stripe-deposit-processing.md /tmp/"), "ask");
  assert.equal(guard("git checkout main -- AGENTS.md"), "ask");
  assert.equal(guard("cat new.yml > .github/workflows/ci.yml"), "ask");
});

test("allows reading and running the harness", () => {
  assert.equal(guard("node --test .claude/hooks/*.test.mjs"), null);
  assert.equal(guard("cat .claude/settings.json"), null);
  assert.equal(guard("git diff -- docs/specs"), null);
  assert.equal(guard("git add .claude/hooks AGENTS.md"), null);
});

test("allows changes elsewhere", () => {
  assert.equal(guard("rm -rf src/Payments.Api/bin"), null);
  assert.equal(guard("dotnet build > build.log"), null);
  assert.equal(guard("git status"), null);
});

test("allows a payload without a command", () => {
  assert.equal(guard(undefined), null);
});
