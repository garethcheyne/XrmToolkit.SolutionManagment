/**
 * Combined Test Report Generator — Solution Management (err403)
 *
 * Runs both Vitest (React/TS) and xUnit (.NET/C#) test suites and produces:
 *   - reports/test-report.md       — human-readable summary
 *   - reports/test-report.pdf      — branded A4 PDF report
 *   - Console output               — summary with pass/fail counts
 *
 * Usage:
 *   npx tsx scripts/generate-test-report.ts
 *   npm run report:test
 */

import { execSync } from "node:child_process";
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import path from "node:path";
import { escapeHtml, renderBrandedPdf, getTimestamp } from "./pdf-engine";

// ─── Directories ─────────────────────────────────────────────────────────────

const webUIDir = process.cwd();                                    // WebUI/
const rootDir = path.resolve(webUIDir, "..");                      // err403.SolutionManagment/
const solutionRoot = path.resolve(rootDir, "..");                  // repo root
const reportsDir = path.join(solutionRoot, "reports");
const vitestOutputPath = path.join(reportsDir, "vitest-results.json");
const trxOutputPath = path.join(reportsDir, "dotnet-results.trx");
const testProjectPath = path.join(
  solutionRoot,
  "err403.SolutionManagment.Tests",
  "err403.SolutionManagment.Tests.csproj"
);

// VS MSBuild — builds the old-style .NET 4.8 main project correctly.
// The dotnet SDK MSBuild cannot handle the resx resources in the main project.
const VS_MSBUILD = [
  "C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe",
  "C:\\Program Files (x86)\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe",
  "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe",
  "C:\\Program Files (x86)\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe",
].find(existsSync) ?? "MSBuild.exe";

// ─── Types ───────────────────────────────────────────────────────────────────

interface TestResult {
  name: string;
  status: "passed" | "failed" | "skipped";
  duration: number;
  failureMessage?: string;
}

interface SuiteResult {
  name: string;
  file: string;
  tests: TestResult[];
  passed: number;
  failed: number;
  skipped: number;
  duration: number;
}

interface VitestJsonOutput {
  success: boolean;
  testResults: Array<{
    name: string;
    assertionResults: Array<{
      ancestorTitles: string[];
      title: string;
      status: "passed" | "failed" | "pending";
      duration: number | null;
      failureMessages: string[];
    }>;
  }>;
}

// ─── Run Vitest ───────────────────────────────────────────────────────────────

function runVitestTests(): VitestJsonOutput {
  try {
    execSync(`npx vitest run --reporter=json --outputFile="${vitestOutputPath}"`, {
      cwd: webUIDir,
      encoding: "utf-8",
      stdio: ["pipe", "pipe", "pipe"],
    });
  } catch {
    // Vitest exits non-zero on failures — results are still written
  }

  if (!existsSync(vitestOutputPath)) {
    console.error("  ERROR: Vitest JSON output not found. Is Vitest installed?");
    process.exit(1);
  }

  return JSON.parse(readFileSync(vitestOutputPath, "utf-8")) as VitestJsonOutput;
}

function parseVitestResults(json: VitestJsonOutput): SuiteResult[] {
  return json.testResults.map((fileResult) => {
    const relativePath = path.relative(webUIDir, fileResult.name).replace(/\\/g, "/");
    const tests: TestResult[] = (fileResult.assertionResults || []).map((t) => ({
      name: [...t.ancestorTitles, t.title].join(" › "),
      status: t.status === "pending" ? "skipped" : t.status,
      duration: t.duration || 0,
      failureMessage: t.failureMessages.length > 0 ? t.failureMessages.join("\n") : undefined,
    }));

    return {
      name: relativePath.replace(/^src\/__tests__\//, "").replace(/\.test\.(ts|tsx)$/, ""),
      file: relativePath,
      tests,
      passed: tests.filter((t) => t.status === "passed").length,
      failed: tests.filter((t) => t.status === "failed").length,
      skipped: tests.filter((t) => t.status === "skipped").length,
      duration: tests.reduce((s, t) => s + t.duration, 0),
    };
  });
}

// ─── Run dotnet test ──────────────────────────────────────────────────────────

function runDotnetTests(): string | null {
  if (!existsSync(testProjectPath)) {
    console.warn("  WARN: C# test project not found — skipping dotnet tests.");
    console.warn(`        Expected: ${testProjectPath}`);
    return null;
  }

  const mainSln = path.join(solutionRoot, "err403.SolutionManagment.sln");

  // Step 1: Build main project with VS MSBuild (handles old-style .resx correctly)
  try {
    console.log(`        Building main project (MSBuild)...`);
    execSync(`"${VS_MSBUILD}" "${mainSln}" -t:Build -p:Configuration=Debug -verbosity:minimal`, {
      encoding: "utf-8",
      stdio: ["pipe", "pipe", "pipe"],
    });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    console.warn(`  WARN: MSBuild failed — ${msg}`);
  }

  // Step 2: Build + run the SDK-style test project with dotnet
  try {
    execSync(
      `dotnet test "${testProjectPath}" --logger "trx;LogFileName=dotnet-results.trx" --results-directory "${reportsDir}"`,
      { encoding: "utf-8", stdio: ["pipe", "pipe", "pipe"] }
    );
  } catch {
    // dotnet test exits non-zero on failures — TRX is still written
  }

  if (!existsSync(trxOutputPath)) {
    console.warn("  WARN: TRX file was not produced. dotnet test may have failed to build.");
    return null;
  }

  return readFileSync(trxOutputPath, "utf-8");
}

/** Parse a TRX XML string produced by dotnet test into SuiteResult[]. */
function parseTrxResults(trx: string): SuiteResult[] {
  // ── Build a map: testId → className (from <TestDefinitions>) ──
  const classMap = new Map<string, string>();
  const unitTestRx = /<UnitTest[^>]*\bid="([^"]+)"[^>]*>[\s\S]*?<TestMethod[^>]*className="([^"]+)"[^>]*/g;
  let m: RegExpExecArray | null;
  while ((m = unitTestRx.exec(trx)) !== null) {
    classMap.set(m[1]!, m[2]!);
  }

  // ── Parse each UnitTestResult ──
  const resultRx =
    /<UnitTestResult[^>]*\btestId="([^"]+)"[^>]*\btestName="([^"]+)"[^>]*\boutcome="([^"]+)"[^>]*\bduration="([^"]+)"[^>]*(?:\/>([\s\S]*?<\/UnitTestResult>)?|>([\s\S]*?)<\/UnitTestResult>)/g;

  // Simpler, more robust approach: extract attributes with individual regex
  interface RawResult {
    testId: string;
    testName: string;
    outcome: string;
    duration: string;
    errorMessage?: string;
  }

  const rawResults: RawResult[] = [];
  const resultBlockRx = /<UnitTestResult([^>]+)(?:\/>|(>[\s\S]*?<\/UnitTestResult>))/g;
  let rb: RegExpExecArray | null;
  while ((rb = resultBlockRx.exec(trx)) !== null) {
    const attrs = rb[1]!;
    const body = rb[2] ?? "";

    const get = (attr: string) => {
      const rx = new RegExp(`\\b${attr}="([^"]+)"`);
      return rx.exec(attrs)?.[1] ?? "";
    };

    let errorMessage: string | undefined;
    const msgMatch = /<Message>([\s\S]*?)<\/Message>/.exec(body);
    if (msgMatch) {
      // Decode XML entities
      errorMessage = msgMatch[1]!
        .replace(/&lt;/g, "<")
        .replace(/&gt;/g, ">")
        .replace(/&amp;/g, "&")
        .replace(/&quot;/g, '"')
        .trim();
    }

    rawResults.push({
      testId: get("testId"),
      testName: get("testName"),
      outcome: get("outcome"),
      duration: get("duration"),
      errorMessage,
    });
  }

  // ── Group by class name ──
  const suiteMap = new Map<string, TestResult[]>();
  for (const r of rawResults) {
    const className = classMap.get(r.testId) ?? "Tests";
    // Shorten: "err403.SolutionManagment.Tests.BumpVersionTests" → "BumpVersionTests"
    const shortClass = className.split(".").pop() ?? className;

    const status: TestResult["status"] =
      r.outcome === "Passed" ? "passed" : r.outcome === "Failed" ? "failed" : "skipped";

    const durationMs = parseTrxDuration(r.duration);

    const list = suiteMap.get(shortClass) ?? [];
    list.push({ name: r.testName, status, duration: durationMs, failureMessage: r.errorMessage });
    suiteMap.set(shortClass, list);
  }

  // ── Build SuiteResult[] ──
  return Array.from(suiteMap.entries()).map(([className, tests]) => ({
    name: className,
    file: `err403.SolutionManagment.Tests/${className}.cs`,
    tests,
    passed: tests.filter((t) => t.status === "passed").length,
    failed: tests.filter((t) => t.status === "failed").length,
    skipped: tests.filter((t) => t.status === "skipped").length,
    duration: tests.reduce((s, t) => s + t.duration, 0),
  }));
}

/** Convert TRX duration "00:00:00.1234567" to milliseconds. */
function parseTrxDuration(d: string): number {
  if (!d) return 0;
  const parts = d.split(":");
  if (parts.length !== 3) return 0;
  const [h, min, sec] = parts.map(parseFloat);
  return ((h! * 3600) + (min! * 60) + sec!) * 1000;
}

// ─── Markdown generation ──────────────────────────────────────────────────────

function generateMarkdown(reactSuites: SuiteResult[], csharpSuites: SuiteResult[]): string {
  const allSuites = [...reactSuites, ...csharpSuites];
  const totalTests = allSuites.reduce((s, r) => s + r.tests.length, 0);
  const totalPassed = allSuites.reduce((s, r) => s + r.passed, 0);
  const totalFailed = allSuites.reduce((s, r) => s + r.failed, 0);
  const totalDuration = allSuites.reduce((s, r) => s + r.duration, 0);
  const allGreen = totalFailed === 0;
  const ts = getTimestamp();

  const lines: string[] = [];
  lines.push("# Solution Management — Test Report");
  lines.push("");
  lines.push(`**Date:** ${ts}  `);
  lines.push(`**Status:** ${allGreen ? "✅ PASS" : "❌ FAIL"}  `);
  lines.push(`**Total:** ${totalTests} tests | ${totalPassed} passed | ${totalFailed} failed | ${totalDuration.toFixed(0)}ms`);
  lines.push("");
  lines.push("---");
  lines.push("");

  const renderSection = (title: string, suites: SuiteResult[]) => {
    if (suites.length === 0) return;
    lines.push(`## ${title}`);
    lines.push("");
    lines.push("| Suite | Tests | Passed | Failed | Duration | Status |");
    lines.push("|-------|------:|-------:|-------:|---------:|--------|");
    for (const r of suites) {
      const status = r.failed === 0 ? "Pass ✅" : "Fail ❌";
      lines.push(`| ${r.name} | ${r.tests.length} | ${r.passed} | ${r.failed} | ${r.duration.toFixed(0)}ms | ${status} |`);
    }
    lines.push("");

    for (const r of suites) {
      lines.push(`### ${r.name}`);
      lines.push("");
      lines.push(`> **File:** \`${r.file}\``);
      lines.push("");
      lines.push("| # | Test | Time | Result |");
      lines.push("|--:|------|-----:|--------|");
      r.tests.forEach((t, i) => {
        const icon = t.status === "passed" ? "✅ Pass" : t.status === "failed" ? "❌ Fail" : "⏭ Skip";
        lines.push(`| ${i + 1} | ${t.name} | ${t.duration.toFixed(0)}ms | ${icon} |`);
      });
      lines.push("");

      const failures = r.tests.filter((t) => t.status === "failed");
      if (failures.length > 0) {
        lines.push("#### Failures");
        for (const f of failures) {
          lines.push(`**${f.name}**`);
          lines.push("```");
          lines.push(f.failureMessage ?? "No details");
          lines.push("```");
          lines.push("");
        }
      }
    }
    lines.push("---");
    lines.push("");
  };

  renderSection("React / TypeScript", reactSuites);
  renderSection("C# / .NET", csharpSuites);

  lines.push("*Generated by `scripts/generate-test-report.ts`*");
  return lines.join("\n");
}

// ─── PDF body ────────────────────────────────────────────────────────────────

function buildPdfBody(reactSuites: SuiteResult[], csharpSuites: SuiteResult[]): string {
  const allSuites = [...reactSuites, ...csharpSuites];
  const totalTests = allSuites.reduce((s, r) => s + r.tests.length, 0);
  const totalPassed = allSuites.reduce((s, r) => s + r.passed, 0);
  const totalFailed = allSuites.reduce((s, r) => s + r.failed, 0);
  const totalDuration = allSuites.reduce((s, r) => s + r.duration, 0);
  const allGreen = totalFailed === 0;
  const passRate = totalTests > 0 ? ((totalPassed / totalTests) * 100).toFixed(1) : "0";

  const renderSuiteRows = (suites: SuiteResult[]) =>
    suites
      .map(
        (r) =>
          `<tr>
        <td>${escapeHtml(r.name)}</td>
        <td class="center">${r.tests.length}</td>
        <td class="center">${r.passed}</td>
        <td class="center">${r.failed}</td>
        <td class="center">${r.duration.toFixed(0)}ms</td>
        <td class="center">${r.failed === 0 ? '<span class="badge pass">PASS</span>' : '<span class="badge fail">FAIL</span>'}</td>
      </tr>`
      )
      .join("\n");

  const renderSuiteDetails = (suites: SuiteResult[], colorClass = "") => {
    let html = "";
    for (const r of suites) {
      let testsHtml = "";
      r.tests.forEach((t, i) => {
        const icon = t.status === "passed" ? "✓" : t.status === "failed" ? "✗" : "—";
        const cls = t.status === "passed" ? "pass-icon" : t.status === "failed" ? "fail-icon" : "skip-icon";
        testsHtml += `<tr>
          <td class="center">${i + 1}</td>
          <td>${escapeHtml(t.name)}</td>
          <td class="center">${t.duration.toFixed(0)}ms</td>
          <td class="center"><span class="${cls}">${icon}</span></td>
        </tr>`;
      });

      const failures = r.tests.filter((t) => t.status === "failed");
      let failureHtml = "";
      if (failures.length > 0) {
        failureHtml = `<div class="failures">` +
          failures
            .map(
              (f) =>
                `<div class="failure-item">
            <div class="failure-name">${escapeHtml(f.name)}</div>
            <pre class="failure-detail">${escapeHtml(f.failureMessage ?? "No details")}</pre>
          </div>`
            )
            .join("") +
          `</div>`;
      }

      html += `<div class="suite-block">
        <div class="suite-header">
          <span class="suite-name">${escapeHtml(r.name)}</span>
          <span class="suite-meta">${r.passed}/${r.tests.length} passed &middot; ${r.duration.toFixed(0)}ms</span>
        </div>
        <table class="test-table">
          <thead><tr><th>#</th><th>Test</th><th>Time</th><th>Result</th></tr></thead>
          <tbody>${testsHtml}</tbody>
        </table>
        ${failureHtml}
      </div>`;
    }
    return html;
  };

  return `
  <div class="status-banner ${allGreen ? "pass" : "fail"}">
    <div class="status-icon">${allGreen ? "✓" : "✗"}</div>
    <div>
      <div class="status-label">${allGreen ? "All Tests Passed" : "Some Tests Failed"}</div>
      <div style="font-size:10px;color:#64748b;">${totalTests} tests across ${allSuites.length} suites</div>
    </div>
  </div>

  <div class="kpi-row">
    <div class="kpi"><div class="kpi-value">${totalTests}</div><div class="kpi-label">Total Tests</div></div>
    <div class="kpi"><div class="kpi-value" style="color:#16a34a">${totalPassed}</div><div class="kpi-label">Passed</div></div>
    <div class="kpi"><div class="kpi-value" style="color:${totalFailed > 0 ? "#dc2626" : "#16a34a"}">${totalFailed}</div><div class="kpi-label">Failed</div></div>
    <div class="kpi"><div class="kpi-value">${passRate}%</div><div class="kpi-label">Pass Rate</div></div>
    <div class="kpi"><div class="kpi-value">${totalDuration.toFixed(0)}ms</div><div class="kpi-label">Duration</div></div>
  </div>

  ${reactSuites.length > 0 ? `
  <h2 class="section-title">React / TypeScript — Suite Summary</h2>
  <table>
    <thead><tr><th>Suite</th><th class="center">Tests</th><th class="center">Passed</th><th class="center">Failed</th><th class="center">Duration</th><th class="center">Status</th></tr></thead>
    <tbody>${renderSuiteRows(reactSuites)}</tbody>
  </table>
  ${renderSuiteDetails(reactSuites)}
  ` : ""}

  ${csharpSuites.length > 0 ? `
  <h2 class="section-title purple">C# / .NET — Suite Summary</h2>
  <table>
    <thead><tr><th>Suite</th><th class="center">Tests</th><th class="center">Passed</th><th class="center">Failed</th><th class="center">Duration</th><th class="center">Status</th></tr></thead>
    <tbody>${renderSuiteRows(csharpSuites)}</tbody>
  </table>
  ${renderSuiteDetails(csharpSuites, "purple")}
  ` : ""}`;
}

// ─── Main ─────────────────────────────────────────────────────────────────────

async function main() {
  if (!existsSync(reportsDir)) mkdirSync(reportsDir, { recursive: true });

  console.log("");
  console.log("  Solution Management — Test Report Generator");
  console.log("  ============================================");
  console.log("");

  // ── React / TypeScript ──
  console.log("  [1/2] Running Vitest (React/TypeScript)...");
  const vitestJson = runVitestTests();
  const reactSuites = parseVitestResults(vitestJson);
  const reactPassed = reactSuites.reduce((s, r) => s + r.passed, 0);
  const reactTotal = reactSuites.reduce((s, r) => s + r.tests.length, 0);
  const reactFailed = reactSuites.reduce((s, r) => s + r.failed, 0);
  for (const suite of reactSuites) {
    const icon = suite.failed === 0 ? "  PASS" : "  FAIL";
    console.log(`        ${icon}  ${suite.name} (${suite.passed}/${suite.tests.length})`);
  }
  console.log(`        ─────  ${reactPassed}/${reactTotal} passed, ${reactFailed} failed`);
  console.log("");

  // ── C# / .NET ──
  console.log("  [2/2] Running xUnit (C#/.NET)...");
  const trxContent = runDotnetTests();
  const csharpSuites = trxContent ? parseTrxResults(trxContent) : [];
  const csharpPassed = csharpSuites.reduce((s, r) => s + r.passed, 0);
  const csharpTotal = csharpSuites.reduce((s, r) => s + r.tests.length, 0);
  const csharpFailed = csharpSuites.reduce((s, r) => s + r.failed, 0);
  if (csharpSuites.length > 0) {
    for (const suite of csharpSuites) {
      const icon = suite.failed === 0 ? "  PASS" : "  FAIL";
      console.log(`        ${icon}  ${suite.name} (${suite.passed}/${suite.tests.length})`);
    }
    console.log(`        ─────  ${csharpPassed}/${csharpTotal} passed, ${csharpFailed} failed`);
  } else {
    console.log("        (skipped — no results)");
  }
  console.log("");

  // ── Write Markdown ──
  const mdPath = path.join(reportsDir, "test-report.md");
  writeFileSync(mdPath, generateMarkdown(reactSuites, csharpSuites), "utf-8");
  console.log(`  Markdown:  ${path.relative(webUIDir, mdPath)}`);

  // ── Write PDF ──
  const pdfPath = path.join(reportsDir, "test-report.pdf");
  const allFailed = [...reactSuites, ...csharpSuites].reduce((s, r) => s + r.failed, 0);
  const allTotal = [...reactSuites, ...csharpSuites].reduce((s, r) => s + r.tests.length, 0);
  const allPassed = allTotal - allFailed;

  try {
    await renderBrandedPdf({
      title: "Test Report",
      subtitle: `${allPassed}/${allTotal} passed`,
      bodyHtml: buildPdfBody(reactSuites, csharpSuites),
      outputPath: pdfPath,
    });
    console.log(`  PDF:       ${path.relative(webUIDir, pdfPath)}`);
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    console.log(`  PDF skipped: ${msg}`);
    console.log("  (Install Playwright with: npx playwright install chromium)");
  }

  // ── Summary ──
  console.log("");
  console.log("  ============================================");
  const allGreen = allFailed === 0;
  console.log(`  ${allGreen ? "✅ PASS" : "❌ FAIL"}  ${allPassed}/${allTotal} tests passed`);
  console.log("");

  if (allFailed > 0) process.exit(1);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
