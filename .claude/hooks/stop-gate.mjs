// Feedback loop: an agent cannot declare work done while the build or tests are red.
// Runs on the Stop event. Exit 2 blocks the stop and feeds the failure back to the agent.

import { existsSync, readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { execFileSync } from "node:child_process";

const payload = JSON.parse(readFileSync(0, "utf-8"));

// A previous stop was already blocked by this hook; let the agent stop to avoid a loop.
if (payload.stop_hook_active) process.exit(0);

function hasCsproj(dir) {
  if (!existsSync(dir)) return false;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) {
      if (hasCsproj(full)) return true;
    } else if (entry.name.endsWith(".csproj")) {
      return true;
    }
  }
  return false;
}

// Nothing to gate until the implementation exists.
if (!hasCsproj("src") && !hasCsproj("tests")) process.exit(0);

function run(args) {
  try {
    execFileSync("dotnet", args, { encoding: "utf-8", stdio: ["ignore", "pipe", "pipe"] });
    return null;
  } catch (err) {
    return `${err.stdout ?? ""}${err.stderr ?? ""}`;
  }
}

const buildOutput = run(["build", "--nologo", "-v", "quiet"]);
if (buildOutput !== null) {
  process.stderr.write(`Stop gate: dotnet build failed.\n${buildOutput}`);
  process.exit(2);
}

const testOutput = run(["test", "--nologo", "-v", "quiet"]);
if (testOutput !== null) {
  process.stderr.write(`Stop gate: dotnet test failed.\n${testOutput}`);
  process.exit(2);
}

process.exit(0);
