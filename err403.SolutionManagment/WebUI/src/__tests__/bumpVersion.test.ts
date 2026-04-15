import { describe, it, expect, vi } from 'vitest';
import { bumpVersion } from '../utils/versionUtils';

describe('bumpVersion', () => {
  describe('Major policy', () => {
    it('increments major, zeros all others', () => {
      expect(bumpVersion('1.2.3.4', 'Major')).toBe('2.0.0.0');
    });
    it('handles version starting at 0', () => {
      expect(bumpVersion('0.0.0.0', 'Major')).toBe('1.0.0.0');
    });
    it('resets minor/build/revision even when non-zero', () => {
      expect(bumpVersion('5.9.99.999', 'Major')).toBe('6.0.0.0');
    });
  });

  describe('Minor policy', () => {
    it('increments minor, zeros build & revision', () => {
      expect(bumpVersion('1.2.3.4', 'Minor')).toBe('1.3.0.0');
    });
    it('preserves major', () => {
      expect(bumpVersion('3.0.0.0', 'Minor')).toBe('3.1.0.0');
    });
  });

  describe('Build policy', () => {
    it('increments build, zeros revision', () => {
      expect(bumpVersion('1.2.3.4', 'Build')).toBe('1.2.4.0');
    });
    it('preserves major and minor', () => {
      expect(bumpVersion('2.5.0.0', 'Build')).toBe('2.5.1.0');
    });
  });

  describe('Revision policy', () => {
    it('increments only revision', () => {
      expect(bumpVersion('1.2.3.4', 'Revision')).toBe('1.2.3.5');
    });
    it('preserves major, minor and build', () => {
      expect(bumpVersion('2.0.1.0', 'Revision')).toBe('2.0.1.1');
    });
  });

  describe('Date policy', () => {
    it('returns a date-based version with counter starting at 1 for new date', () => {
      // Freeze time to a known date
      const fakeDate = new Date(2026, 3, 12, 0, 0, 0); // 2026-04-12
      vi.useFakeTimers();
      vi.setSystemTime(fakeDate);

      const result = bumpVersion('1.0.0.0', 'Date', 'yyyy.MM.dd.x');
      expect(result).toBe('2026.04.12.1');

      vi.useRealTimers();
    });

    it('increments counter when same date prefix already exists', () => {
      const fakeDate = new Date(2026, 3, 12, 0, 0, 0);
      vi.useFakeTimers();
      vi.setSystemTime(fakeDate);

      const result = bumpVersion('2026.04.12.3', 'Date', 'yyyy.MM.dd.x');
      expect(result).toBe('2026.04.12.4');

      vi.useRealTimers();
    });

    it('resets counter to 1 when date has changed', () => {
      const fakeDate = new Date(2026, 3, 12, 0, 0, 0);
      vi.useFakeTimers();
      vi.setSystemTime(fakeDate);

      const result = bumpVersion('2026.04.11.7', 'Date', 'yyyy.MM.dd.x');
      expect(result).toBe('2026.04.12.1');

      vi.useRealTimers();
    });

    it('handles mask without x placeholder', () => {
      const fakeDate = new Date(2026, 3, 12, 0, 0, 0);
      vi.useFakeTimers();
      vi.setSystemTime(fakeDate);

      const result = bumpVersion('1.0.0.0', 'Date', 'yyyy.MM.dd');
      expect(result).toBe('2026.04.12');

      vi.useRealTimers();
    });
  });

  describe('Default / unknown policy', () => {
    it('returns the original version unchanged', () => {
      expect(bumpVersion('1.2.3.4', 'Skip')).toBe('1.2.3.4');
    });
    it('returns original for empty string policy', () => {
      expect(bumpVersion('2.0.0.0', '')).toBe('2.0.0.0');
    });
  });

  describe('Edge cases — short version strings', () => {
    it('pads a 1-part version string', () => {
      expect(bumpVersion('3', 'Minor')).toBe('3.1.0.0');
    });
    it('pads a 2-part version string', () => {
      expect(bumpVersion('1.5', 'Build')).toBe('1.5.1.0');
    });
    it('handles 3-part version string', () => {
      expect(bumpVersion('1.2.3', 'Revision')).toBe('1.2.3.1');
    });
    it('treats non-numeric parts as 0', () => {
      expect(bumpVersion('a.b.c.d', 'Major')).toBe('1.0.0.0');
    });
  });
});
