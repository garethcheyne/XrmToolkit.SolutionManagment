import { describe, it, expect } from 'vitest';
import { parseError } from '../utils/parseErrorUtils';

// ─── Re-usable sample input ────────────────────────────────────────────────

const SINGLE_DEP = `Solution import failed.

Missing Dependencies:
  • Some Workflow  (Workflow)
    requires: Base Process  (Workflow)
    solution: CoreSolution   id: {11111111-2222-3333-4444-555555555555}`;

const TWO_DEPS = `Import error occurred.

Missing Dependencies:
  • My Canvas App  (Model-driven App)
    requires: Shared Component  (Web Resource)
    solution: SharedLib   id: {AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}

  • My Plugin Step  (SDK Message Processing Step)
    requires: Custom Entity  (Entity)`;

const NO_SOLUTION_LINE = `Validation failed.

Missing Dependencies:
  • Form X  (System Form)
    requires: Entity Y  (Entity)`;

// ─── Tests ──────────────────────────────────────────────────────────────────

describe('parseError', () => {
  describe('returns null for non-dep messages', () => {
    it('returns null for a plain error string', () => {
      expect(parseError('Something went wrong')).toBeNull();
    });

    it('returns null for empty string', () => {
      expect(parseError('')).toBeNull();
    });

    it('returns null when marker is absent', () => {
      expect(parseError('Exception type: ...\nMessage: Cancelled by user')).toBeNull();
    });

    it('returns null when marker is present but no deps follow', () => {
      // "Missing Dependencies:" with nothing after
      expect(parseError('Some intro\nMissing Dependencies:\n')).toBeNull();
    });
  });

  describe('parses intro correctly', () => {
    it('captures text before the marker as intro', () => {
      const result = parseError(SINGLE_DEP);
      expect(result?.intro).toBe('Solution import failed.');
    });

    it('trims whitespace from intro', () => {
      const msg = '   Trimmed intro   \nMissing Dependencies:\n  • A  (Entity)\n    requires: B  (Entity)';
      const result = parseError(msg);
      expect(result?.intro).toBe('Trimmed intro');
    });
  });

  describe('parses single dependency', () => {
    it('returns kind = missing-deps', () => {
      expect(parseError(SINGLE_DEP)?.kind).toBe('missing-deps');
    });

    it('extracts exactly 1 dep', () => {
      expect(parseError(SINGLE_DEP)?.deps).toHaveLength(1);
    });

    it('extracts dependent name correctly', () => {
      expect(parseError(SINGLE_DEP)?.deps[0].dependentName).toBe('Some Workflow');
    });

    it('extracts dependent type correctly', () => {
      expect(parseError(SINGLE_DEP)?.deps[0].dependentType).toBe('Workflow');
    });

    it('extracts required name correctly', () => {
      expect(parseError(SINGLE_DEP)?.deps[0].requiredName).toBe('Base Process');
    });

    it('extracts required type correctly', () => {
      expect(parseError(SINGLE_DEP)?.deps[0].requiredType).toBe('Workflow');
    });

    it('extracts solution name', () => {
      expect(parseError(SINGLE_DEP)?.deps[0].solution).toBe('CoreSolution');
    });

    it('extracts id', () => {
      expect(parseError(SINGLE_DEP)?.deps[0].id).toBe('{11111111-2222-3333-4444-555555555555}');
    });
  });

  describe('parses multiple dependencies', () => {
    it('returns 2 deps', () => {
      expect(parseError(TWO_DEPS)?.deps).toHaveLength(2);
    });

    it('first dep has correct dependentName', () => {
      expect(parseError(TWO_DEPS)?.deps[0].dependentName).toBe('My Canvas App');
    });

    it('second dep has correct dependentName', () => {
      expect(parseError(TWO_DEPS)?.deps[1].dependentName).toBe('My Plugin Step');
    });

    it('second dep has no solution or id (optional fields)', () => {
      const dep = parseError(TWO_DEPS)?.deps[1];
      expect(dep?.solution).toBeUndefined();
      expect(dep?.id).toBeUndefined();
    });
  });

  describe('handles missing optional fields', () => {
    it('works when solution line is absent', () => {
      const result = parseError(NO_SOLUTION_LINE);
      expect(result).not.toBeNull();
      expect(result?.deps[0].solution).toBeUndefined();
      expect(result?.deps[0].id).toBeUndefined();
    });
  });

  describe('strips bullet prefix', () => {
    it('handles bullet with leading whitespace', () => {
      const msg = 'Intro\nMissing Dependencies:\n  •   My Component  (Entity)\n    requires: Base  (Entity)';
      const result = parseError(msg);
      expect(result?.deps[0].dependentName).toBe('My Component');
    });
  });
});
