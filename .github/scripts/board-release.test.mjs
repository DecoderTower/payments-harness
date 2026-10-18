// Which merged PRs move which cards to Release (ADR-007).
// Run with: node --test .github/scripts/*.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import { isImplementation, referencedIssues } from "./board-release.mjs";

const implementation = "- [x] Implementation, changes behaviour — against a merged spec";
const technical = "- [x] Implementation, no behaviour change — migrations, build";
const design = "- [x] Design — decision records";

test("a service change references its issue with Refs", () => {
  assert.deepEqual(referencedIssues(`Refs #24\n\n${implementation}`), [24]);
});

test("Closes is not a service change reference", () => {
  assert.deepEqual(referencedIssues("Closes #27"), []);
});

test("several references, deduplicated, in order", () => {
  assert.deepEqual(referencedIssues("Refs #2 and refs #21, then Refs #2 again"), [2, 21]);
});

test("no references and no body", () => {
  assert.deepEqual(referencedIssues("Nothing here"), []);
  assert.deepEqual(referencedIssues(undefined), []);
});

test("the template placeholder is not a reference", () => {
  assert.deepEqual(referencedIssues("Refs #<!-- service changes -->"), []);
});

test("only implementation PRs move cards", () => {
  assert.equal(isImplementation(implementation), true);
  assert.equal(isImplementation(technical), true);
  assert.equal(isImplementation(design), false);
  assert.equal(isImplementation("- [ ] Implementation, changes behaviour"), false);
});
