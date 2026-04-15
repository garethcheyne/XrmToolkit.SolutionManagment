import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text, Badge, Divider, Link,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  DismissRegular, ArrowRightRegular, BoxRegular,
  WarningRegular, CheckmarkCircleRegular, OpenRegular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  surface: {
    maxWidth: '820px',
    width: '90vw',
    maxHeight: '90vh',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    overflow: 'auto',
    maxHeight: '68vh',
  },
  summaryBar: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    padding: '10px 12px',
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorPaletteRedBorder2}`,
    fontSize: '13px',
  },
  successBar: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    padding: '10px 12px',
    backgroundColor: tokens.colorPaletteGreenBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorPaletteGreenBorder1}`,
    fontSize: '13px',
  },
  groupHeader: {
    fontSize: '11px',
    fontWeight: 600,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
    color: tokens.colorNeutralForeground3,
    padding: '4px 0 2px 0',
  },
  depCard: {
    display: 'flex',
    flexDirection: 'column',
    gap: '6px',
    padding: '10px 12px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  depRow: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    flexWrap: 'wrap',
  },
  metaRow: {
    fontSize: '11px',
    color: tokens.colorNeutralForeground3,
    fontFamily: tokens.fontFamilyMonospace,
    paddingLeft: '4px',
  },
  learnLink: {
    fontSize: '11px',
    color: tokens.colorBrandForeground1,
  },
  footer: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
});

export interface MissingDepResult {
  solution: string;
  target: string;
  requiredName: string;
  requiredType: string;
  requiredId: string;
  requiredSolution: string;
  dependentName: string;
  dependentType: string;
}

interface MissingDepsDialogProps {
  results: MissingDepResult[];
  open: boolean;
  onClose: () => void;
}

export function MissingDepsDialog({ results, open, onClose }: MissingDepsDialogProps) {
  const styles = useStyles();

  // Group by target then by solution
  const groups = results.reduce<Record<string, Record<string, MissingDepResult[]>>>((acc, r) => {
    if (!acc[r.target]) acc[r.target] = {};
    if (!acc[r.target]![r.solution]) acc[r.target]![r.solution] = [];
    acc[r.target]![r.solution]!.push(r);
    return acc;
  }, {});

  const targetCount = Object.keys(groups).length;
  const hasIssues = results.length > 0;

  return (
    <Dialog open={open} onOpenChange={(_e, d) => { if (!d.open) onClose(); }}>
      <DialogSurface className={styles.surface}>
        <DialogTitle action={
          <Button appearance="subtle" icon={<DismissRegular />} onClick={onClose} aria-label="Close" />
        }>
          Missing Dependencies Check
        </DialogTitle>
        <DialogBody>
          <DialogContent className={styles.content}>
            {!hasIssues ? (
              <div className={styles.successBar}>
                <CheckmarkCircleRegular fontSize={18} color={tokens.colorPaletteGreenForeground1} />
                <Text>No missing dependencies found. Safe to transfer.</Text>
              </div>
            ) : (
              <div className={styles.summaryBar}>
                <WarningRegular fontSize={18} color={tokens.colorPaletteRedForeground1} />
                <Text weight="semibold">{results.length} missing component{results.length !== 1 ? 's' : ''} across {targetCount} target{targetCount !== 1 ? 's' : ''}</Text>
              </div>
            )}

            {hasIssues && (
              <>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  The following components are required by your solutions but are not present on the target environments.
                  Install the required solutions first, or the import will fail.
                </Text>

                {Object.entries(groups).map(([target, solutionMap]) => (
                  <div key={target} style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                    <div className={styles.groupHeader}>
                      Target: {target}
                    </div>
                    {Object.entries(solutionMap).map(([solution, deps]) => (
                      <div key={solution} style={{ display: 'flex', flexDirection: 'column', gap: '4px', marginLeft: '8px' }}>
                        <Text size={200} weight="semibold" style={{ color: tokens.colorNeutralForeground2 }}>
                          Solution: {solution}
                        </Text>
                        {deps.map((dep, i) => (
                          <div key={i} className={styles.depCard}>
                            <div className={styles.depRow}>
                              <BoxRegular fontSize={14} color={tokens.colorBrandForeground1} />
                              <Text className={styles.depRow} weight="semibold" size={200}>{dep.dependentName}</Text>
                              {dep.dependentType && (
                                <Badge size="small" appearance="tint" color="informative">{dep.dependentType}</Badge>
                              )}
                              <ArrowRightRegular fontSize={14} color={tokens.colorNeutralForeground3} />
                              <Text size={200} style={{ color: tokens.colorPaletteRedForeground1, fontWeight: 600 }}>
                                {dep.requiredName || 'Unknown component'}
                              </Text>
                              {dep.requiredType && (
                                <Badge size="small" appearance="tint" color="danger">{dep.requiredType}</Badge>
                              )}
                            </div>
                            {(dep.requiredSolution || dep.requiredId) && (
                              <div className={styles.metaRow}>
                                {dep.requiredSolution && <span>solution: {dep.requiredSolution}  </span>}
                                {dep.requiredId && <span>id: {dep.requiredId}</span>}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    ))}
                    <Divider />
                  </div>
                ))}
              </>
            )}

            <div className={styles.footer}>
              <Text size={100} style={{ color: tokens.colorNeutralForeground4 }}>
                Learn more about solution dependencies on Microsoft Learn:
              </Text>
              <Link
                href="https://learn.microsoft.com/en-us/power-platform/alm/dependency-tracking-solution-components"
                target="_blank"
                className={styles.learnLink}
              >
                <OpenRegular fontSize={11} /> Dependency tracking for solution components
              </Link>
              <Link
                href="https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm#solution-dependencies"
                target="_blank"
                className={styles.learnLink}
              >
                <OpenRegular fontSize={11} /> Solution dependency concepts
              </Link>
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onClose}>Close</Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
