/* MovieMind frontend settings.
 * Film data uses the same-origin /api endpoints. Personal view preferences stay local.
 */
window.MOVIEMIND_CONFIG = Object.freeze({
  version: '4.0',
  storageMode: 'database',
  apiBaseUrl: '/api',
  authEnabled: false,
});
