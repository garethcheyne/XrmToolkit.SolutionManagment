/* ------------------------------------------------------------------
 *  DocsViewer.tsx – In-app documentation viewer with sidebar
 *  navigation and markdown rendering.
 * ----------------------------------------------------------------- */

import { useState, useMemo, useCallback, type ReactNode } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Input,
  Tree,
  TreeItem,
  TreeItemLayout,
  MessageBar,
  MessageBarBody,
  TabList,
  Tab,
  type SelectTabData,
  Divider,
  Tooltip,
  Button,
} from '@fluentui/react-components';
import {
  SearchRegular,
  BookRegular,
  ChevronRightRegular,
  ArrowExpandRegular,
} from '@fluentui/react-icons';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { sections, overviewPage, allPages } from '../docsData';
import { postMessage } from '../bridge';

/* ----------------------------------------------------------------
 *  Styles
 * --------------------------------------------------------------- */

const useStyles = makeStyles({
  root: {
    display: 'flex',
    height: '100%',
    overflow: 'hidden',
  },
  sidebar: {
    width: '260px',
    minWidth: '260px',
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'hidden',
  },
  sidebarHeader: {
    padding: '12px 16px 8px',
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    flexShrink: 0,
    justifyContent: 'space-between',
  },
  sidebarHeaderLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  searchBox: {
    padding: '0 12px 8px',
    flexShrink: 0,
  },
  navTree: {
    flex: 1,
    overflowY: 'auto',
    padding: '0 4px 12px',
  },
  navItem: {
    cursor: 'pointer',
    borderRadius: tokens.borderRadiusMedium,
  },
  navItemActive: {
    cursor: 'pointer',
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground1Selected,
    fontWeight: tokens.fontWeightSemibold,
  },
  content: {
    flex: 1,
    minWidth: 0,
    overflowY: 'auto',
    overflowX: 'hidden',
    padding: '24px 40px 48px',
  },
  contentInner: {
    maxWidth: '720px',
    wordWrap: 'break-word' as const,
    overflowWrap: 'break-word' as const,
  },
  // Markdown styles
  mdH1: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightBold,
    marginTop: '0',
    marginBottom: '16px',
    lineHeight: tokens.lineHeightHero800,
    color: tokens.colorNeutralForeground1,
  },
  mdH2: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    marginTop: '32px',
    marginBottom: '12px',
    lineHeight: tokens.lineHeightBase500,
    color: tokens.colorNeutralForeground1,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    paddingBottom: '6px',
  },
  mdH3: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    marginTop: '24px',
    marginBottom: '8px',
    color: tokens.colorNeutralForeground1,
  },
  mdP: {
    marginTop: '0',
    marginBottom: '12px',
    lineHeight: '1.65',
    color: tokens.colorNeutralForeground1,
  },
  mdA: {
    color: tokens.colorBrandForeground1,
    textDecoration: 'none',
    ':hover': {
      textDecoration: 'underline',
    },
  },
  mdCode: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    backgroundColor: tokens.colorNeutralBackground4,
    padding: '2px 6px',
    borderRadius: tokens.borderRadiusSmall,
  },
  mdPre: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    backgroundColor: tokens.colorNeutralBackground4,
    padding: '12px 16px',
    borderRadius: tokens.borderRadiusMedium,
    overflowX: 'auto',
    marginBottom: '16px',
  },
  mdTable: {
    width: '100%',
    borderCollapse: 'collapse',
    marginBottom: '16px',
    fontSize: tokens.fontSizeBase300,
    tableLayout: 'fixed' as const,
  },
  mdTableWrap: {
    overflowX: 'auto' as const,
    marginBottom: '16px',
  },
  mdTh: {
    textAlign: 'left',
    padding: '8px 12px',
    borderBottom: `2px solid ${tokens.colorNeutralStroke1}`,
    fontWeight: tokens.fontWeightSemibold,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  mdTd: {
    padding: '8px 12px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  mdUl: {
    paddingLeft: '24px',
    marginBottom: '12px',
  },
  mdOl: {
    paddingLeft: '24px',
    marginBottom: '12px',
  },
  mdLi: {
    marginBottom: '4px',
    lineHeight: '1.6',
  },
  mdHr: {
    border: 'none',
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    marginTop: '24px',
    marginBottom: '24px',
  },
  mdBlockquote: {
    margin: '0 0 12px 0',
    padding: '0',
    borderLeft: 'none',
  },
  breadcrumb: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    marginBottom: '16px',
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  breadcrumbLink: {
    cursor: 'pointer',
    color: tokens.colorBrandForeground1,
    ':hover': { textDecoration: 'underline' },
  },
  stepsContainer: {
    borderLeft: `2px solid ${tokens.colorBrandStroke1}`,
    marginLeft: '8px',
    paddingLeft: '20px',
    marginBottom: '16px',
  },
  stepItem: {
    position: 'relative' as const,
    marginBottom: '16px',
    '::before': {
      content: '""',
      position: 'absolute' as const,
      left: '-27px',
      top: '4px',
      width: '12px',
      height: '12px',
      borderRadius: '50%',
      backgroundColor: tokens.colorBrandBackground,
    },
  },
  tabsContainer: {
    marginBottom: '16px',
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
  },
  tabContent: {
    padding: '12px 16px',
  },
  searchResults: {
    padding: '8px 12px',
    cursor: 'pointer',
    borderRadius: tokens.borderRadiusMedium,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  searchResultSection: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

/* ----------------------------------------------------------------
 *  Custom block preprocessor
 * --------------------------------------------------------------- */

interface CalloutBlock {
  type: 'callout';
  intent: 'info' | 'success' | 'warning' | 'error';
  content: string;
}

interface StepsBlock {
  type: 'steps';
  content: string;
}

interface TabsBlock {
  type: 'tabs';
  tabs: { label: string; content: string }[];
}

type Block = { type: 'md'; content: string } | CalloutBlock | StepsBlock | TabsBlock;

function parseBlocks(markdown: string): Block[] {
  const blocks: Block[] = [];
  const lines = markdown.split('\n');
  let i = 0;

  while (i < lines.length) {
    const line = lines[i]!;

    // :::steps block
    if (line.trim() === ':::steps') {
      i++;
      const stepLines: string[] = [];
      while (i < lines.length && (lines[i] ?? '').trim() !== ':::') {
        stepLines.push(lines[i] ?? '');
        i++;
      }
      i++; // skip closing :::
      blocks.push({ type: 'steps', content: stepLines.join('\n') });
      continue;
    }

    // :::tabs block
    if (line.trim() === ':::tabs') {
      i++;
      const tabs: { label: string; content: string }[] = [];
      let currentLabel = '';
      let currentLines: string[] = [];

      while (i < lines.length && (lines[i] ?? '').trim() !== ':::') {
        const ln = lines[i] ?? '';
        const tabMatch = ln.match(/^@tab\s+(.+)$/);
        if (tabMatch) {
          if (currentLabel) {
            tabs.push({ label: currentLabel, content: currentLines.join('\n').trim() });
          }
          currentLabel = tabMatch[1] ?? '';
          currentLines = [];
        } else {
          currentLines.push(ln);
        }
        i++;
      }
      if (currentLabel) {
        tabs.push({ label: currentLabel, content: currentLines.join('\n').trim() });
      }
      i++; // skip closing :::
      blocks.push({ type: 'tabs', tabs });
      continue;
    }

    // GitHub-style callouts: > [!TIP], > [!WARNING], etc.
    const calloutMatch = line.match(/^>\s*\[!(TIP|NOTE|WARNING|CAUTION|IMPORTANT)\]/i);
    if (calloutMatch) {
      const intentMap: Record<string, CalloutBlock['intent']> = {
        TIP: 'success',
        NOTE: 'info',
        WARNING: 'warning',
        CAUTION: 'error',
        IMPORTANT: 'warning',
      };
      const intent = intentMap[calloutMatch[1]!.toUpperCase()] ?? 'info';
      i++;
      const calloutLines: string[] = [];
      while (i < lines.length && (lines[i] ?? '').startsWith('>')) {
        calloutLines.push((lines[i] ?? '').replace(/^>\s?/, ''));
        i++;
      }
      blocks.push({ type: 'callout', intent, content: calloutLines.join('\n').trim() });
      continue;
    }

    // Regular markdown line
    // Accumulate consecutive markdown lines
    const mdLines: string[] = [line];
    i++;
    while (i < lines.length) {
      const nextLine = lines[i] ?? '';
      if (nextLine.trim() === ':::steps' || nextLine.trim() === ':::tabs') break;
      if (nextLine.match(/^>\s*\[!(TIP|NOTE|WARNING|CAUTION|IMPORTANT)\]/i)) break;
      mdLines.push(nextLine);
      i++;
    }
    const content = mdLines.join('\n').trim();
    if (content) {
      blocks.push({ type: 'md', content });
    }
  }

  return blocks;
}

/* ----------------------------------------------------------------
 *  Sub-components
 * --------------------------------------------------------------- */

function CalloutBlock_({ intent, content }: { intent: CalloutBlock['intent']; content: string }) {
  return (
    <MessageBar intent={intent} style={{ marginBottom: 12 }}>
      <MessageBarBody>
        <MarkdownContent body={content} />
      </MessageBarBody>
    </MessageBar>
  );
}

function StepsBlock_({ content }: { content: string }) {
  const styles = useStyles();
  return (
    <div className={styles.stepsContainer}>
      <MarkdownContent body={content} />
    </div>
  );
}

function TabsBlock_({ tabs }: { tabs: TabsBlock['tabs'] }) {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState(tabs[0]?.label ?? '');

  const handleSelect = (_: unknown, data: SelectTabData) => {
    setActiveTab(data.value as string);
  };

  const activeContent = tabs.find((t) => t.label === activeTab)?.content ?? '';

  return (
    <div className={styles.tabsContainer}>
      <TabList selectedValue={activeTab} onTabSelect={handleSelect} size="small">
        {tabs.map((t) => (
          <Tab key={t.label} value={t.label}>
            {t.label}
          </Tab>
        ))}
      </TabList>
      <div className={styles.tabContent}>
        <MarkdownContent body={activeContent} />
      </div>
    </div>
  );
}

/** Renders raw markdown using react-markdown with Fluent UI styled elements */
function MarkdownContent({ body }: { body: string }) {
  const styles = useStyles();

  const components = useMemo(
    () => ({
      h1: ({ children }: { children?: ReactNode }) => <h1 className={styles.mdH1}>{children}</h1>,
      h2: ({ children }: { children?: ReactNode }) => <h2 className={styles.mdH2}>{children}</h2>,
      h3: ({ children }: { children?: ReactNode }) => <h3 className={styles.mdH3}>{children}</h3>,
      p: ({ children }: { children?: ReactNode }) => <p className={styles.mdP}>{children}</p>,
      a: ({ href, children }: { href?: string; children?: ReactNode }) => (
        <a className={styles.mdA} href={href} target="_blank" rel="noopener noreferrer">
          {children}
        </a>
      ),
      code: ({ className, children }: { className?: string; children?: ReactNode }) => {
        // Inline code vs code block
        if (className) {
          return <code className={styles.mdCode}>{children}</code>;
        }
        return <code className={styles.mdCode}>{children}</code>;
      },
      pre: ({ children }: { children?: ReactNode }) => (
        <pre className={styles.mdPre}>{children}</pre>
      ),
      table: ({ children }: { children?: ReactNode }) => (
        <div className={styles.mdTableWrap}>
          <table className={styles.mdTable}>{children}</table>
        </div>
      ),
      th: ({ children }: { children?: ReactNode }) => (
        <th className={styles.mdTh}>{children}</th>
      ),
      td: ({ children }: { children?: ReactNode }) => (
        <td className={styles.mdTd}>{children}</td>
      ),
      ul: ({ children }: { children?: ReactNode }) => (
        <ul className={styles.mdUl}>{children}</ul>
      ),
      ol: ({ children }: { children?: ReactNode }) => (
        <ol className={styles.mdOl}>{children}</ol>
      ),
      li: ({ children }: { children?: ReactNode }) => (
        <li className={styles.mdLi}>{children}</li>
      ),
      hr: () => <hr className={styles.mdHr} />,
      blockquote: ({ children }: { children?: ReactNode }) => (
        <div className={styles.mdBlockquote}>{children}</div>
      ),
    }),
    [styles],
  );

  return (
    <ReactMarkdown remarkPlugins={[remarkGfm]} components={components}>
      {body}
    </ReactMarkdown>
  );
}

/* ----------------------------------------------------------------
 *  Main DocsViewer component
 * --------------------------------------------------------------- */

export function DocsViewer() {
  const styles = useStyles();
  const [currentSlug, setCurrentSlug] = useState<string>('');
  const [search, setSearch] = useState('');

  // Determine current page
  const currentPage = useMemo(() => {
    if (!currentSlug && overviewPage) return overviewPage;
    // Check section index pages
    for (const section of sections) {
      if (section.slug === currentSlug && section.index) return section.index;
      for (const page of section.pages) {
        if (page.slug === currentSlug) return page;
      }
    }
    return overviewPage;
  }, [currentSlug]);

  // Search results
  const searchResults = useMemo(() => {
    if (!search || search.length < 2) return [];
    const q = search.toLowerCase();
    return allPages
      .filter(
        (p) =>
          p.title.toLowerCase().includes(q) ||
          p.body.toLowerCase().includes(q),
      )
      .slice(0, 10);
  }, [search]);

  // Find section for breadcrumb
  const currentSection = useMemo(() => {
    if (!currentSlug) return null;
    return sections.find(
      (s) =>
        s.slug === currentSlug || s.pages.some((p) => p.slug === currentSlug),
    );
  }, [currentSlug]);

  const navigate = useCallback((slug: string) => {
    setCurrentSlug(slug);
    setSearch('');
  }, []);

  // Parse blocks for current page
  const blocks = useMemo(() => {
    if (!currentPage) return [];
    return parseBlocks(currentPage.body);
  }, [currentPage]);

  const isHelpOnly = !!(window as unknown as { __helpOnly?: boolean }).__helpOnly;

  return (
    <div className={styles.root}>
      {/* Sidebar */}
      <div className={styles.sidebar}>
        <div className={styles.sidebarHeader}>
          <div className={styles.sidebarHeaderLeft}>
            <BookRegular fontSize={20} />
            <Text weight="semibold" size={400}>
              Documentation
            </Text>
          </div>
          {!isHelpOnly && (
            <Tooltip content="Open in separate window" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<ArrowExpandRegular />}
                onClick={() => postMessage({ action: 'popOutHelp' })}
              />
            </Tooltip>
          )}
        </div>

        <div className={styles.searchBox}>
          <Input
            contentBefore={<SearchRegular />}
            placeholder="Search docs..."
            size="small"
            value={search}
            onChange={(_, d) => setSearch(d.value)}
            style={{ width: '100%' }}
          />
        </div>

        {search && searchResults.length > 0 ? (
          <div className={styles.navTree}>
            {searchResults.map((page) => {
              const section = sections.find(
                (s) =>
                  s.slug === page.slug ||
                  s.pages.some((p) => p.slug === page.slug),
              );
              return (
                <div
                  key={page.slug}
                  className={styles.searchResults}
                  onClick={() => navigate(page.slug)}
                >
                  <Text size={300} weight="semibold">
                    {page.title}
                  </Text>
                  {section && (
                    <div className={styles.searchResultSection}>
                      {section.title}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        ) : (
          <div className={styles.navTree}>
            {/* Overview link */}
            {overviewPage && (
              <div
                className={
                  currentSlug === '' ? styles.navItemActive : styles.navItem
                }
                style={{ padding: '4px 8px', marginBottom: 4 }}
                onClick={() => navigate('')}
              >
                <Text size={300} weight={currentSlug === '' ? 'semibold' : 'regular'}>
                  Overview
                </Text>
              </div>
            )}

            <Divider style={{ margin: '4px 0 8px' }} />

            <Tree aria-label="Documentation navigation">
              {sections.map((section) => (
                <TreeItem
                  key={section.slug}
                  itemType="branch"
                  open
                >
                  <TreeItemLayout
                    onClick={() => navigate(section.slug)}
                    className={
                      currentSlug === section.slug
                        ? styles.navItemActive
                        : styles.navItem
                    }
                  >
                    <Text
                      size={300}
                      weight={currentSlug === section.slug ? 'semibold' : 'semibold'}
                    >
                      {section.title}
                    </Text>
                  </TreeItemLayout>
                  <Tree>
                    {section.pages.map((page) => (
                      <TreeItem key={page.slug} itemType="leaf">
                        <TreeItemLayout
                          onClick={() => navigate(page.slug)}
                          className={
                            currentSlug === page.slug
                              ? styles.navItemActive
                              : styles.navItem
                          }
                        >
                          <Text
                            size={200}
                            weight={
                              currentSlug === page.slug ? 'semibold' : 'regular'
                            }
                          >
                            {page.title}
                          </Text>
                        </TreeItemLayout>
                      </TreeItem>
                    ))}
                  </Tree>
                </TreeItem>
              ))}
            </Tree>
          </div>
        )}
      </div>

      {/* Content area */}
      <div className={styles.content}>
        <div className={styles.contentInner}>
        {/* Breadcrumb */}
        {currentSection && (
          <div className={styles.breadcrumb}>
            <span className={styles.breadcrumbLink} onClick={() => navigate('')}>
              Docs
            </span>
            <ChevronRightRegular fontSize={12} />
            <span
              className={styles.breadcrumbLink}
              onClick={() => navigate(currentSection.slug)}
            >
              {currentSection.title}
            </span>
            {currentPage && currentPage.slug !== currentSection.slug && (
              <>
                <ChevronRightRegular fontSize={12} />
                <span>{currentPage.title}</span>
              </>
            )}
          </div>
        )}

        {/* Page title */}
        {currentPage && (
          <h1 className={styles.mdH1}>{currentPage.title}</h1>
        )}

        {/* Rendered blocks */}
        {blocks.map((block, idx) => {
          switch (block.type) {
            case 'callout':
              return <CalloutBlock_ key={idx} intent={block.intent} content={block.content} />;
            case 'steps':
              return <StepsBlock_ key={idx} content={block.content} />;
            case 'tabs':
              return <TabsBlock_ key={idx} tabs={block.tabs} />;
            case 'md':
              return <MarkdownContent key={idx} body={block.content} />;
          }
        })}

        {!currentPage && (
          <Text size={400}>Select a page from the navigation to get started.</Text>
        )}
        </div>
      </div>
    </div>
  );
}
