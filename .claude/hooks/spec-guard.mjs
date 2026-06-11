// Constraint: specs and decision records are durable context. They change deliberately,
// never as collateral damage of an implementation task. Any write to docs/specs or
// docs/decisions requires explicit human approval.

import { readFileSync } from "node:fs";

const payload = JSON.parse(readFileSync(0, "utf-8"));
const path = payload.tool_input?.file_path ?? "";

if (/[\\/]docs[\\/](specs|decisions)[\\/]/.test(path) && !/TEMPLATE\.md$/.test(path)) {
  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "ask",
      permissionDecisionReason: `This edits durable context (${path}). Spec and decision changes must be deliberate - confirm before proceeding.`
    }
  }));
}

process.exit(0);
