import {
  Dialog, DialogSurface, DialogTitle, DialogBody,
  DialogContent, DialogActions,
  Button, Text, Badge, Divider,
  makeStyles, tokens,
} from '@fluentui/react-components';
import {
  DismissRegular, ArrowDownloadRegular, ArrowRightRegular,
  LinkRegular, BoxRegular,
} from '@fluentui/react-icons';
import { parseError } from '../utils/parseErrorUtils';

const useStyles = makeStyles({
  surface: {
    maxWidth: '780px',
    width: '90vw',
    maxHeight: '90vh',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    overflow: 'auto',
    maxHeight: '70vh',
  },
  intro: {
    padding: '12px 14px',
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorPaletteRedBorder2}`,
    fontSize: '13px',
    color: tokens.colorNeutralForeground1,
  },
  sectionTitle: {
    fontSize: '11px',
    fontWeight: 600,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
    color: tokens.colorNeutralForeground3,
  },
  depCard: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    padding: '12px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  depRow: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    flexWrap: 'wrap',
  },
  depName: {
    fontWeight: 600,
    fontSize: '13px',
  },
  requiresRow: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    paddingLeft: '4px',
    flexWrap: 'wrap',
  },
  arrowIcon: {
    color: tokens.colorNeutralForeground3,
    flexShrink: 0,
  },
  metaRow: {
    display: 'flex',
    gap: '16px',
    paddingLeft: '4px',
    flexWrap: 'wrap',
  },
  metaItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    fontSize: '11px',
    color: tokens.colorNeutralForeground3,
  },
  metaValue: {
    fontFamily: 'Consolas, monospace',
    fontSize: '11px',
  },
  errorBlock: {
    fontFamily: 'Consolas, "Courier New", monospace',
    fontSize: '12px',
    lineHeight: '1.5',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
    padding: '16px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    color: tokens.colorNeutralForeground1,
    userSelect: 'text',
  },
  divider: { flexGrow: 0 },
});

function downloadError(title: string, errorMessage: string) {
  const blob = new Blob([errorMessage], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${title.replace(/[^a-zA-Z0-9_-]/g, '_')}_error.txt`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

interface ErrorDetailDialogProps {
  open: boolean;
  title: string;
  errorMessage: string;
  onClose: () => void;
}

export function ErrorDetailDialog({ open, title, errorMessage, onClose }: ErrorDetailDialogProps) {
  const styles = useStyles();
  const parsed = parseError(errorMessage);

  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) onClose(); }}>
      <DialogSurface className={styles.surface}>
        <DialogTitle>{title}</DialogTitle>
        <DialogBody>
          <DialogContent className={styles.content}>
            {parsed ? (
              <>
                {parsed.intro && (
                  <div className={styles.intro}>
                    <Text>{parsed.intro}</Text>
                  </div>
                )}

                <Divider className={styles.divider} />
                <Text className={styles.sectionTitle}>
                  Missing Dependencies ({parsed.deps.length})
                </Text>

                {parsed.deps.map((dep, i) => (
                  <div key={i} className={styles.depCard}>
                    {/* Dependent component */}
                    <div className={styles.depRow}>
                      <BoxRegular fontSize={14} style={{ color: tokens.colorPaletteRedForeground1, flexShrink: 0 }} />
                      <Text className={styles.depName}>{dep.dependentName}</Text>
                      {dep.dependentType && (
                        <Badge appearance="outline" size="small" color="danger">{dep.dependentType}</Badge>
                      )}
                    </div>

                    {/* Requires arrow */}
                    <div className={styles.requiresRow}>
                      <ArrowRightRegular className={styles.arrowIcon} fontSize={14} />
                      <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>requires</Text>
                      <LinkRegular fontSize={14} style={{ color: tokens.colorPaletteYellowForeground2, flexShrink: 0 }} />
                      <Text weight="semibold" size={200}>{dep.requiredName}</Text>
                      {dep.requiredType && (
                        <Badge appearance="outline" size="small" color="warning">{dep.requiredType}</Badge>
                      )}
                    </div>

                    {/* Metadata: solution + id */}
                    {(dep.solution || dep.id) && (
                      <div className={styles.metaRow}>
                        {dep.solution && (
                          <div className={styles.metaItem}>
                            <Text size={100}>Solution:</Text>
                            <Text size={100} className={styles.metaValue}>{dep.solution}</Text>
                          </div>
                        )}
                        {dep.id && (
                          <div className={styles.metaItem}>
                            <Text size={100}>ID:</Text>
                            <Text size={100} className={styles.metaValue}>{dep.id}</Text>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </>
            ) : (
              <div className={styles.errorBlock}>{errorMessage}</div>
            )}
          </DialogContent>
          <DialogActions>
            <Button
              appearance="secondary"
              icon={<ArrowDownloadRegular />}
              onClick={() => downloadError(title, errorMessage)}
            >
              Download
            </Button>
            <Button appearance="primary" icon={<DismissRegular />} onClick={onClose}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
