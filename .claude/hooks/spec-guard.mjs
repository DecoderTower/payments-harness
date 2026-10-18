// Constraint: specs, decision records, and the rest of the durable context — and the
// harness itself — change deliberately, never as collateral damage of an
// implementation task. Any write to them requires explicit human approval.

import { readFileSync } from "node:fs";
import { isProtectedFile } from "./protected-paths.mjs";

const payload = JSON.parse(readFileSync(0, "utf-8"));
const path = payload.tool_input?.file_path ?? "";

if (isProtectedFile(path)) {
  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "ask",
      permissionDecisionReason: `This edits durable context or the harness (${path}). Those changes must be deliberate - confirm before proceeding.`
    }
  }));
}

process.exit(0);
