/** Compute a bumped version string (mirrors C# BumpVersion logic). */
export function bumpVersion(currentVersion: string, policy: string, dateMask = 'yyyy.MM.dd.x'): string {
  const parts = currentVersion.split('.');
  while (parts.length < 4) parts.push('0');
  const maj = parseInt(parts[0] ?? '0', 10) || 0;
  const min = parseInt(parts[1] ?? '0', 10) || 0;
  const bld = parseInt(parts[2] ?? '0', 10) || 0;
  const rev = parseInt(parts[3] ?? '0', 10) || 0;

  switch (policy) {
    case 'Major': return `${maj + 1}.0.0.0`;
    case 'Minor': return `${maj}.${min + 1}.0.0`;
    case 'Build': return `${maj}.${min}.${bld + 1}.0`;
    case 'Revision': return `${maj}.${min}.${bld}.${rev + 1}`;
    case 'Date': {
      const now = new Date();
      const dateBase = dateMask
        .replace('yyyy', String(now.getFullYear()))
        .replace('MM', String(now.getMonth() + 1).padStart(2, '0'))
        .replace('dd', String(now.getDate()).padStart(2, '0'))
        .replace('HHmm', `${String(now.getHours()).padStart(2, '0')}${String(now.getMinutes()).padStart(2, '0')}`);
      const xIdx = dateBase.indexOf('x');
      if (xIdx < 0) return dateBase;
      const prefix = dateBase.substring(0, xIdx);
      if (currentVersion.startsWith(prefix)) {
        const lastPart = currentVersion.substring(prefix.length);
        const lastNum = parseInt(lastPart, 10) || 0;
        return prefix + (lastNum + 1);
      }
      return dateBase.replace('x', '1');
    }
    default: return currentVersion;
  }
}
