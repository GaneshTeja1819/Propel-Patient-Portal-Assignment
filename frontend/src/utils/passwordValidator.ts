export type PasswordRuleId = 'minLength' | 'upperCase' | 'lowerCase' | 'digit';

export interface PasswordRule {
  id: PasswordRuleId;
  label: string;
  validate: (password: string) => boolean;
}

export interface PasswordValidationResult {
  isValid: boolean;
  failingRules: PasswordRule[];
}

const passwordRules: PasswordRule[] = [
  {
    id: 'minLength',
    label: 'Must be at least 8 characters long.',
    validate: (password) => password.length >= 8,
  },
  {
    id: 'upperCase',
    label: 'Must contain at least one uppercase letter.',
    validate: (password) => /[A-Z]/.test(password),
  },
  {
    id: 'lowerCase',
    label: 'Must contain at least one lowercase letter.',
    validate: (password) => /[a-z]/.test(password),
  },
  {
    id: 'digit',
    label: 'Must contain at least one digit.',
    validate: (password) => /\d/.test(password),
  },
];

export function evaluatePassword(password: string): PasswordValidationResult {
  const failingRules = passwordRules.filter((rule) => !rule.validate(password));

  return {
    isValid: failingRules.length === 0,
    failingRules,
  };
}
