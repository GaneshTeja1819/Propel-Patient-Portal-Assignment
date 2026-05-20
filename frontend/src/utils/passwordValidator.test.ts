import { describe, expect, it } from 'vitest';
import { evaluatePassword } from './passwordValidator';

describe('evaluatePassword', () => {
  it('returns all failures for an empty password', () => {
    const result = evaluatePassword('');

    expect(result.isValid).toBe(false);
    expect(result.failingRules.map((rule) => rule.id)).toEqual([
      'minLength',
      'upperCase',
      'lowerCase',
      'digit',
    ]);
  });

  it('returns valid for a password satisfying all rules', () => {
    const result = evaluatePassword('StrongPass1');

    expect(result.isValid).toBe(true);
    expect(result.failingRules).toEqual([]);
  });
});
