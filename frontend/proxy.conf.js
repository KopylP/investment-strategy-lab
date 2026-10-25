// The API runs on its own port; the browser talks to one origin (cookie auth with SameSite=Strict).
// Under Aspire the API address comes from the service reference, standalone it is the API's launch profile.
const target = process.env['services__api__http__0'] ?? process.env['API_HTTP'] ?? 'http://localhost:5180';

module.exports = {
  '/api': { target, secure: false, changeOrigin: false },
};
