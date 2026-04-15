export interface ParsedDep {
  dependentName: string;
  dependentType: string;
  requiredName: string;
  requiredType: string;
  solution?: string;
  id?: string;
}

export interface ParsedError {
  kind: 'missing-deps';
  intro: string;
  deps: ParsedDep[];
}

export function parseError(msg: string): ParsedError | null {
  const marker = 'Missing Dependencies:';
  const idx = msg.indexOf(marker);
  if (idx === -1) return null;

  const intro = msg.substring(0, idx).trim();
  const depsBlock = msg.substring(idx + marker.length).trim();
  const depChunks = depsBlock.split(/\n\n+/);

  const deps: ParsedDep[] = [];
  for (const chunk of depChunks) {
    const lines = chunk.split('\n').map((l) => l.trim()).filter(Boolean);
    if (lines.length === 0) continue;

    // Line 0: "• DependentName  (DependentType)"
    const depLine = (lines[0] ?? '').replace(/^•\s*/, '');
    const depTypeMatch = depLine.match(/^(.+?)\s{2,}\((.+)\)$/);
    const dependentName = depTypeMatch?.[1]?.trim() ?? depLine;
    const dependentType = depTypeMatch?.[2]?.trim() ?? '';

    // Line 1: "requires: RequiredName  (RequiredType)"
    const reqLine = lines.find((l) => l.startsWith('requires:')) ?? '';
    const reqBody = reqLine.replace(/^requires:\s*/, '');
    const reqTypeMatch = reqBody.match(/^(.+?)\s{2,}\((.+)\)$/);
    const requiredName = reqTypeMatch?.[1]?.trim() ?? reqBody;
    const requiredType = reqTypeMatch?.[2]?.trim() ?? '';

    // Optional: "solution: X   id: {guid}"
    const solLine = lines.find((l) => l.startsWith('solution:')) ?? '';
    const solMatch = solLine.match(/solution:\s*([^\s]+(?:\s[^\s]+)*?)\s{2,}id:\s*(.+)/);
    const solution = solMatch
      ? solMatch[1]?.trim()
      : solLine.replace(/^solution:\s*/, '').trim() || undefined;
    const id = solMatch ? solMatch[2]?.trim() : undefined;

    deps.push({ dependentName, dependentType, requiredName, requiredType, solution: solution || undefined, id });
  }

  if (deps.length === 0) return null;
  return { kind: 'missing-deps', intro, deps };
}
