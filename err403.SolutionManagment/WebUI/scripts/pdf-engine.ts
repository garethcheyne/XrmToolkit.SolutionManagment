/**
 * Branded PDF Engine — Solution Management (err403)
 *
 * Provides consistent styles, header, footer and Playwright-based
 * rendering for all test/project PDF documents.
 *
 * Theme: Power Platform blue (#0078d4) + purple (#742774) on dark navy.
 *
 * Usage:
 *   import { renderBrandedPdf, BrandedPdfOptions } from "./pdf-engine";
 *   await renderBrandedPdf({ title, subtitle, bodyHtml, outputPath });
 */

import { writeFileSync } from "node:fs";

// ─── Types ───────────────────────────────────────────────────────────────────

export interface BrandedPdfOptions {
  title: string;
  subtitle?: string;
  bodyHtml: string;
  outputPath: string;
  orientation?: "portrait" | "landscape";
  timestamp?: string;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

export function escapeHtml(str: string): string {
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

export function getTimestamp(override?: string): string {
  if (override) return override;
  return new Date()
    .toISOString()
    .replace("T", " ")
    .replace(/\.\d+Z$/, " UTC");
}

// ─── Shared CSS ───────────────────────────────────────────────────────────────

export const BRAND_CSS = `
  @page { size: A4; margin: 20mm 15mm; }
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body {
    font-family: "Segoe UI", -apple-system, BlinkMacSystemFont, Roboto, "Helvetica Neue", sans-serif;
    color: #1e293b; line-height: 1.5; font-size: 11px;
    background: #f8fafc;
  }

  /* ── Accent bar — Power Platform blue→purple gradient ── */
  .accent-bar { height: 6px; background: linear-gradient(90deg, #0078d4 0%, #106ebe 30%, #742774 70%, #a857a8 100%); border-radius: 0 0 4px 4px; }

  /* ── Header ── */
  .header-block { margin: 16px 0 24px 0; padding: 20px 24px 18px 24px; background: linear-gradient(135deg, #001f3f 0%, #002d5c 50%, #1a1a2e 100%); border-radius: 12px; position: relative; overflow: hidden; }
  .header-block::before { content: ''; position: absolute; top: -40px; right: -40px; width: 220px; height: 220px; background: radial-gradient(circle, rgba(0,120,212,0.18) 0%, transparent 70%); }
  .header-block::after { content: ''; position: absolute; bottom: -30px; left: 25%; width: 160px; height: 160px; background: radial-gradient(circle, rgba(116,39,116,0.12) 0%, transparent 70%); }
  .header-inner { display: flex; justify-content: space-between; align-items: center; position: relative; z-index: 1; }
  .header-left { display: flex; align-items: center; gap: 14px; }
  .header-text { display: flex; flex-direction: column; }
  .wordmark { font-size: 22px; font-weight: 800; letter-spacing: -0.5px; background: linear-gradient(90deg, #0078d4, #a857a8); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; line-height: 1.2; }
  .header-subtitle { font-size: 9.5px; color: rgba(148,163,184,0.9); letter-spacing: 2px; text-transform: uppercase; font-weight: 500; margin-top: 2px; }
  .document-badge { text-align: right; padding: 8px 16px; background: rgba(0,120,212,0.12); border-radius: 8px; border: 1px solid rgba(0,120,212,0.30); }
  .badge-label { display: block; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 1.5px; color: #fff; }
  .badge-meta { font-size: 9px; color: rgba(148,163,184,0.85); margin-top: 1px; display: block; }

  /* ── KPI row ── */
  .kpi-row { display: flex; gap: 12px; margin-bottom: 22px; }
  .kpi { flex: 1; text-align: center; padding: 12px 8px; border-radius: 8px; background: #fff; border: 1px solid #e2e8f0; }
  .kpi-value { font-size: 22px; font-weight: 800; color: #0078d4; }
  .kpi-label { font-size: 9px; text-transform: uppercase; letter-spacing: 1px; color: #64748b; font-weight: 600; }

  /* ── Status banners ── */
  .status-banner { display: flex; align-items: center; gap: 16px; padding: 14px 20px; border-radius: 10px; margin-bottom: 20px; }
  .status-banner.pass { background: linear-gradient(135deg, #f0fdf4, #dcfce7); border: 1px solid #86efac; }
  .status-banner.fail { background: linear-gradient(135deg, #fef2f2, #fee2e2); border: 1px solid #fca5a5; }
  .status-icon { font-size: 28px; }
  .status-label { font-size: 18px; font-weight: 700; }
  .status-banner.pass .status-label { color: #166534; }
  .status-banner.fail .status-label { color: #991b1b; }

  /* ── Tables ── */
  table { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
  th { background: linear-gradient(135deg, #001f3f, #002d5c); color: #fff; padding: 7px 10px; text-align: left; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px; }
  td { padding: 6px 10px; border-bottom: 1px solid #e2e8f0; font-size: 10.5px; }
  tr:nth-child(even) td { background: #f0f7ff; }
  .center { text-align: center; }

  /* ── Badges ── */
  .badge { display: inline-block; padding: 2px 10px; border-radius: 12px; font-size: 9px; font-weight: 700; letter-spacing: 0.5px; }
  .badge.pass { background: #dcfce7; color: #166534; }
  .badge.fail { background: #fee2e2; color: #991b1b; }
  .badge.blue { background: #dbeafe; color: #1e40af; }
  .badge.purple { background: #f3e8ff; color: #6b21a8; }
  .pass-icon { color: #16a34a; font-weight: 700; font-size: 13px; }
  .fail-icon { color: #dc2626; font-weight: 700; font-size: 13px; }
  .skip-icon { color: #94a3b8; font-size: 13px; }

  /* ── Section titles ── */
  .section-title { font-size: 14px; font-weight: 700; color: #0a0a0a; margin: 20px 0 10px 0; padding-bottom: 4px; border-bottom: 2px solid #0078d4; }
  .section-title.purple { border-bottom-color: #742774; }

  /* ── Suite blocks ── */
  .suite-block { margin-bottom: 18px; page-break-inside: avoid; }
  .suite-header { display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 6px; }
  .suite-name { font-size: 12px; font-weight: 700; color: #1e293b; }
  .suite-meta { font-size: 10px; color: #64748b; }
  .test-table th, .test-table td { font-size: 10px; }
  .test-table th { font-size: 9.5px; }

  /* ── Failure details ── */
  .failures { margin-top: 8px; }
  .failure-item { margin-bottom: 8px; padding: 8px 12px; background: #fef2f2; border-left: 3px solid #dc2626; border-radius: 4px; }
  .failure-name { font-weight: 700; font-size: 10px; color: #991b1b; margin-bottom: 4px; }
  .failure-detail { font-family: "Cascadia Code", "Consolas", monospace; font-size: 9px; color: #64748b; white-space: pre-wrap; word-break: break-word; }

  /* ── Footer ── */
  .footer { margin-top: 30px; padding: 12px 0 0 0; border-top: 2px solid #0078d4; display: flex; justify-content: space-between; align-items: center; font-size: 9px; color: #94a3b8; }
  .footer-brand { display: flex; align-items: center; gap: 6px; }
  .footer-dot { width: 5px; height: 5px; border-radius: 50%; background: linear-gradient(135deg, #0078d4, #742774); display: inline-block; }
  .footer-team { color: #0078d4; font-weight: 600; }
`;

// ─── HTML Document Shell ──────────────────────────────────────────────────────

export function buildBrandedHtml(options: BrandedPdfOptions): string {
  const ts = getTimestamp(options.timestamp);

  return `<!DOCTYPE html>
<html lang="en">
<head><meta charset="utf-8"><title>Solution Management — ${escapeHtml(options.title)}</title>
<style>${BRAND_CSS}</style>
</head>
<body>
  <div class="accent-bar"></div>

  <div class="header-block">
    <div class="header-inner">
      <div class="header-left">
        <div class="header-text">
          <div class="wordmark">Solution Management</div>
          <div class="header-subtitle">XrmToolBox Plugin &nbsp;·&nbsp; err403</div>
        </div>
      </div>
      <div class="document-badge">
        <span class="badge-label">${escapeHtml(options.title)}</span>
        <span class="badge-meta">${escapeHtml(options.subtitle || ts)}</span>
      </div>
    </div>
  </div>

  ${options.bodyHtml}

  <div class="footer">
    <div class="footer-brand">
      <span class="footer-dot"></span>
      <span class="footer-team">err403</span>
      <span>— Solution Management · XrmToolBox Plugin</span>
    </div>
    <span>${escapeHtml(ts)}</span>
  </div>
</body>
</html>`;
}

// ─── Render PDF via Playwright ────────────────────────────────────────────────

export async function renderBrandedPdf(options: BrandedPdfOptions): Promise<void> {
  const { chromium } = await import("playwright-core");

  const browser = await chromium.launch({
    headless: true,
    args: [
      "--no-sandbox",
      "--disable-setuid-sandbox",
      "--disable-dev-shm-usage",
      "--disable-gpu",
    ],
  });

  try {
    const page = await browser.newPage();
    const html = buildBrandedHtml(options);
    await page.setContent(html, { waitUntil: "networkidle", timeout: 15000 });

    const isLandscape = options.orientation === "landscape";
    const pdfBuffer = await page.pdf({
      format: "A4",
      landscape: isLandscape,
      margin: { top: "20mm", right: "15mm", bottom: "20mm", left: "15mm" },
      printBackground: true,
    });

    writeFileSync(options.outputPath, pdfBuffer);
    await page.close();
  } finally {
    await browser.close();
  }
}
