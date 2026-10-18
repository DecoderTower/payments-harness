// Code approval moves a service change to Release (docs/Factory.md, Gates; ADR-007).
// When a human merges an implementation PR, each issue it references with "Refs #n" —
// a service change, which stays open until released — has its card moved to Release.
// Document- and harness-only PRs use "Closes #n": GitHub closes the issue on merge and
// the project's built-in workflow moves it to Done. Spec and design PRs move nothing.

import { execFileSync } from "node:child_process";
import { pathToFileURL } from "node:url";

/** True when the PR description ticks one of the Implementation kinds. */
export function isImplementation(body) {
  return /^- \[[xX]\] Implementation/m.test(body ?? "");
}

/** Issue numbers referenced with "Refs #n", deduplicated, in order of first mention. */
export function referencedIssues(body) {
  const numbers = [...(body ?? "").matchAll(/\brefs\s+#(\d+)/gi)].map((match) => Number(match[1]));
  return [...new Set(numbers)];
}

function gh(args) {
  return execFileSync("gh", args, { encoding: "utf-8" });
}

function moveToRelease({ owner, projectNumber, repository, body }) {
  if (!isImplementation(body)) {
    console.log("Not an implementation PR; no card moves.");
    return;
  }
  const issues = referencedIssues(body);
  if (issues.length === 0) {
    console.log("No issue referenced with Refs #n; no card moves.");
    return;
  }

  const project = JSON.parse(gh(["project", "view", projectNumber, "--owner", owner, "--format", "json"]));
  const { fields } = JSON.parse(gh(["project", "field-list", projectNumber, "--owner", owner, "--format", "json"]));
  const status = fields.find((field) => field.name === "Status");
  const release = status?.options?.find((option) => option.name === "Release");
  if (!release) {
    throw new Error('The project\'s Status field has no "Release" column (ADR-007).');
  }

  for (const issue of issues) {
    const url = `https://github.com/${repository}/issues/${issue}`;
    // item-add returns the existing card when the issue is already on the board.
    const item = JSON.parse(gh(["project", "item-add", projectNumber, "--owner", owner, "--url", url, "--format", "json"]));
    gh(["project", "item-edit", "--id", item.id, "--project-id", project.id, "--field-id", status.id, "--single-select-option-id", release.id]);
    console.log(`#${issue} -> Release`);
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  moveToRelease({
    owner: process.env.PROJECT_OWNER,
    projectNumber: process.env.PROJECT_NUMBER,
    repository: process.env.GITHUB_REPOSITORY,
    body: process.env.PR_BODY,
  });
}
