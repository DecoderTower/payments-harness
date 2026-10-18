// Durable context and the harness itself (docs/Factory.md, rule 3). They change
// deliberately, through human review, never as collateral damage of a task.
// One list, shared by spec-guard (file writes) and harness-guard (shell commands).

const PATHS = String.raw`docs[\\/](?:specs|decisions|runbooks)(?![\w-])|docs[\\/](?:Architecture|Factory)\.md|AGENTS\.md|\.claude[\\/](?:hooks|skills)(?![\w-])|\.claude[\\/]settings\.json|\.github(?![\w-])`;

// Anywhere in a command line.
export const PROTECTED_IN_COMMAND = new RegExp(PATHS, "i");

const PROTECTED_FILE = new RegExp(String.raw`(?:^|[\\/])(?:${PATHS})`, "i");

// Templates are scaffolding, not context, and stay writable.
export function isProtectedFile(path) {
  return PROTECTED_FILE.test(path) && !/TEMPLATE\.md$/.test(path);
}
