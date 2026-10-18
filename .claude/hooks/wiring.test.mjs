// A hook that exists but is not registered constrains nothing. This checks that
// every guard is wired into .claude/settings.json, that each hook has its test,
// and that the deny rules keeping agents from passing a gate are still there.
// Run with: node --test .claude/hooks/*.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const settings = JSON.parse(readFileSync(new URL("../settings.json", import.meta.url), "utf-8"));

function registered(event, script) {
  return (settings.hooks?.[event] ?? []).find((entry) =>
    entry.hooks.some((h) => h.command.includes(script)));
}

test("spec-guard runs before every file write", () => {
  const entry = registered("PreToolUse", "spec-guard.mjs");
  assert.ok(entry, "spec-guard is not registered");
  assert.match(entry.matcher, /Write/);
  assert.match(entry.matcher, /Edit/);
});

test("harness-guard runs before every shell command", () => {
  const entry = registered("PreToolUse", "harness-guard.mjs");
  assert.ok(entry, "harness-guard is not registered");
  assert.match(entry.matcher, /Bash/);
  assert.match(entry.matcher, /PowerShell/);
});

test("stop-gate runs on every stop", () => {
  assert.ok(registered("Stop", "stop-gate.mjs"), "stop-gate is not registered");
});

test("every hook has its own test", () => {
  for (const hook of ["spec-guard", "harness-guard", "stop-gate"]) {
    assert.ok(existsSync(fileURLToPath(new URL(`./${hook}.mjs`, import.meta.url))), `${hook}.mjs is missing`);
    assert.ok(existsSync(fileURLToPath(new URL(`./${hook}.test.mjs`, import.meta.url))), `${hook}.test.mjs is missing`);
  }
});

test("agents are denied every command that passes a gate", () => {
  const deny = settings.permissions?.deny ?? [];
  for (const shell of ["Bash", "PowerShell"]) {
    for (const command of ["gh pr merge", "gh pr review", "gh issue close", "gh issue edit", "gh label", "gh release", "gh api"]) {
      assert.ok(deny.includes(`${shell}(${command}:*)`), `${shell}(${command}:*) is not denied`);
    }
  }
});
