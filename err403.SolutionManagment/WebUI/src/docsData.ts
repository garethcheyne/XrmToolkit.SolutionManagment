/* ------------------------------------------------------------------
 *  docsData.ts – build a navigation tree + content map from the
 *  markdown files in docs/solution-management/ at build time.
 *  Vite's import.meta.glob eagerly bundles everything.
 * ----------------------------------------------------------------- */

// Import all markdown files as raw text
const mdModules = import.meta.glob(
  '../docs/solution-management/**/*.md',
  { eager: true, query: '?raw', import: 'default' },
) as Record<string, string>;

// Import all _meta.json files
const metaModules = import.meta.glob(
  '../docs/solution-management/**/_meta.json',
  { eager: true, import: 'default' },
) as Record<string, Record<string, { title: string; icon?: string }>>;

/* ----------------------------------------------------------------
 *  Types
 * --------------------------------------------------------------- */

export interface DocPage {
  /** Unique slug path, e.g. "settings/export" */
  slug: string;
  /** Display title from frontmatter or _meta.json */
  title: string;
  /** Raw markdown body (frontmatter stripped) */
  body: string;
}

export interface DocSection {
  slug: string;
  title: string;
  /** index page for section */
  index?: DocPage;
  /** sub-pages in order defined by _meta.json */
  pages: DocPage[];
}

/* ----------------------------------------------------------------
 *  Helpers
 * --------------------------------------------------------------- */

/** Strip YAML frontmatter, return { title, excerpt, body } */
function parseFrontmatter(raw: string): { title: string; excerpt: string; body: string } {
  const match = raw.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n([\s\S]*)$/);
  if (!match) return { title: '', excerpt: '', body: raw };
  const fm = match[1] ?? '';
  const body = match[2] ?? raw;
  const titleMatch = fm.match(/^title:\s*(.+)$/m);
  const excerptMatch = fm.match(/^excerpt:\s*(.+)$/m);
  return {
    title: titleMatch?.[1]?.trim() ?? '',
    excerpt: excerptMatch?.[1]?.trim() ?? '',
    body,
  };
}

/** Convert a glob path like ../../docs/solution-management/settings/export.md → "settings/export" */
function toSlug(globPath: string): string {
  return globPath
    .replace(/^.*docs\/solution-management\//, '')
    .replace(/\.md$/, '')
    .replace(/\/index$/, '');       // section index → section slug
}

/* ----------------------------------------------------------------
 *  Build content map   slug → DocPage
 * --------------------------------------------------------------- */

const contentMap = new Map<string, DocPage>();

for (const [path, raw] of Object.entries(mdModules)) {
  const slug = toSlug(path);
  const { title, body } = parseFrontmatter(raw);
  contentMap.set(slug, { slug, title, body });
}

/* ----------------------------------------------------------------
 *  Build ordered section list from _meta.json
 * --------------------------------------------------------------- */

// Root _meta defines section order
const rootMetaKey = Object.keys(metaModules).find((k) =>
  k.endsWith('solution-management/_meta.json'),
);
const rootMeta = rootMetaKey ? metaModules[rootMetaKey] : {};

export const sections: DocSection[] = Object.entries(rootMeta ?? {}).map(
  ([sectionSlug, { title }]) => {
    // Find this section's _meta.json
    const sectionMetaKey = Object.keys(metaModules).find((k) =>
      k.endsWith(`/${sectionSlug}/_meta.json`),
    );
    const sectionMeta = (sectionMetaKey ? metaModules[sectionMetaKey] : {}) ?? {};

    // Section index page
    const indexPage = contentMap.get(sectionSlug);

    // Sub-pages in _meta order
    const pages: DocPage[] = Object.entries(sectionMeta)
      .map(([pageSlug, { title: pageTitle }]) => {
        const fullSlug = `${sectionSlug}/${pageSlug}`;
        const page = contentMap.get(fullSlug);
        if (!page) return null;
        // Override title from _meta if available
        return { ...page, title: pageTitle || page.title };
      })
      .filter(Boolean) as DocPage[];

    return { slug: sectionSlug, title, index: indexPage, pages };
  },
);

/** Overview page (root index.md) */
export const overviewPage = contentMap.get('') ?? contentMap.get('index') ?? null;

/** All pages in flat list for search */
export const allPages: DocPage[] = Array.from(contentMap.values());

/** Lookup a page by slug */
export function getPage(slug: string): DocPage | undefined {
  return contentMap.get(slug);
}
