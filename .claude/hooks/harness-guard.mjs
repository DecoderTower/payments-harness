// Constraint: the spec guard sees file writes, but a shell command can delete or
// overwrite the same files. Any shell command that would modify, move, or delete
// durable context or the harness requires explicit human approval.

import { readFileSync } from "node:fs";
import { PROTECTED_IN_COMMAND } from "./protected-paths.mjs";

const payload = JSON.parse(readFileSync(0, "utf-8"));
const command = payload.tool_input?.command ?? "";

const MODIFIES = new RegExp(
  [
    String.raw`(?:^|[\s;&|(])(?:rm|rmdir|del|erase|unlink|truncate|mv|move|cp|copy|ren|rename|tee|ri|mi|cpi|sc|ac)(?=\s)`,
    String.raw`\b(?:Remove-Item|Move-Item|Copy-Item|Rename-Item|Set-Content|Add-Content|Clear-Content|Out-File|New-Item)\b`,
    String.raw`\bsed\s+-[a-zA-Z]*i`,
    String.raw`\bgit\s+(?:rm|mv|checkout|restore|reset|clean|apply)\b`,
    String.raw`>`,
  ].join("|"),
  "i",
);

if (PROTECTED_IN_COMMAND.test(command) && MODIFIES.test(command)) {
  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "ask",
      permissionDecisionReason: "This command may modify, move, or delete durable context or the harness itself. Those change deliberately, through human review - confirm before proceeding."
    }
  }));
}

process.exit(0);
