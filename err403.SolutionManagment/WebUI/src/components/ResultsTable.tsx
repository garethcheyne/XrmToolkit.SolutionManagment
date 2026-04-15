import { Text, makeStyles, tokens } from '@fluentui/react-components';
import { CheckmarkCircleFilled, DismissCircleFilled } from '@fluentui/react-icons';

const useStyles = makeStyles({
  wrapper: { maxHeight: '400px', overflow: 'auto' },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
    tableLayout: 'fixed',
    '& th': {
      textAlign: 'left',
      padding: '6px 10px',
      fontSize: '12px',
      fontWeight: 600,
      borderBottom: `2px solid ${tokens.colorNeutralStroke1}`,
      color: tokens.colorNeutralForeground3,
      whiteSpace: 'nowrap',
    },
    '& td': {
      padding: '8px 10px',
      fontSize: '13px',
      borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
      verticalAlign: 'top',
      wordBreak: 'break-word',
    },
    '& tbody tr:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  successRow: { backgroundColor: tokens.colorPaletteGreenBackground1 },
  errorRow: { backgroundColor: tokens.colorPaletteRedBackground1 },
  errorText: { color: tokens.colorPaletteRedForeground1, whiteSpace: 'pre-wrap' as const },
  successText: { color: tokens.colorPaletteGreenForeground1 },
});

interface ResultRow {
  success: boolean;
  name: string;
  target: string;
  detail?: string;
  elapsed?: string;
}

interface ResultsTableProps {
  rows: ResultRow[];
}

export function ResultsTable({ rows }: ResultsTableProps) {
  const styles = useStyles();

  return (
    <div className={styles.wrapper}>
      <table className={styles.table}>
        <colgroup>
          <col style={{ width: 28 }} />
          <col style={{ width: '30%' }} />
          <col style={{ width: '18%' }} />
          <col style={{ width: '12%' }} />
          <col />
        </colgroup>
        <thead>
          <tr>
            <th />
            <th>Name</th>
            <th>Target</th>
            <th>Time</th>
            <th>Details</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r, i) => (
            <tr key={i} className={r.success ? styles.successRow : styles.errorRow}>
              <td style={{ textAlign: 'center' }}>
                {r.success
                  ? <CheckmarkCircleFilled color={tokens.colorPaletteGreenForeground1} fontSize={16} />
                  : <DismissCircleFilled color={tokens.colorPaletteRedForeground1} fontSize={16} />}
              </td>
              <td><Text size={200} weight="semibold">{r.name}</Text></td>
              <td><Text size={200}>{r.target}</Text></td>
              <td><Text size={200}>{r.elapsed ?? ''}</Text></td>
              <td>
                {r.success
                  ? <Text size={200} className={styles.successText}>OK</Text>
                  : <Text size={200} className={styles.errorText}>{r.detail}</Text>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
