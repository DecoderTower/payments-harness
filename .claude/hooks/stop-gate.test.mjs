// The stop gate is a feedback loop; this checks it actually blocks a red build.
// Run with: node --test .claude/hooks/   (needs the dotnet SDK on PATH)

import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const hook = fileURLToPath(new URL("./stop-gate.mjs", import.meta.url));

function stop(cwd, payload = {}) {
  return spawnSync("node", [hook], { cwd, input: JSON.stringify(payload), encoding: "utf-8" });
}

function withTempRepo(setup, check) {
  const dir = mkdtempSync(join(tmpdir(), "stop-gate-"));
  try {
    setup(dir);
    check(dir);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
}

test("lets the agent stop once it has already been blocked", () => {
  withTempRepo(
    (dir) => {
      mkdirSync(join(dir, "src"));
      writeFileSync(join(dir, "src", "Broken.csproj"), "not a project");
    },
    (dir) => assert.equal(stop(dir, { stop_hook_active: true }).status, 0),
  );
});

test("has nothing to gate before any project exists", () => {
  withTempRepo(() => {}, (dir) => assert.equal(stop(dir).status, 0));
});

test("blocks the stop and feeds the failure back when the build is red", () => {
  withTempRepo(
    (dir) => {
      mkdirSync(join(dir, "src"));
      writeFileSync(join(dir, "src", "Broken.csproj"), "not a project");
    },
    (dir) => {
      const result = stop(dir);
      assert.equal(result.status, 2);
      assert.match(result.stderr, /Stop gate: dotnet build failed/);
    },
  );
});
