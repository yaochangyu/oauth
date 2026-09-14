/**
 * Scenarios registry
 */
import { runLoginConsent } from './login-consent.js';

export const scenarios = {
  'login-consent': runLoginConsent,
};

export function getScenario(name) {
  return scenarios[name];
}

export function listScenarios() {
  return Object.keys(scenarios);
}
