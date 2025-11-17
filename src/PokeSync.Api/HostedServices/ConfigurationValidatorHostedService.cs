using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PokeSync.Api.Options;

namespace PokeSync.Api.HostedServices
{
    /// <summary>
    /// Valide la configuration au démarrage (connection string, token interne, origins).
    /// Ne bloque pas le démarrage : log des warning en cas de problème.
    /// </summary>
    public sealed class ConfigurationValidatorHostedService : IHostedService
    {
        private readonly ILogger<ConfigurationValidatorHostedService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SecurityOptions _security;

        public ConfigurationValidatorHostedService(
            ILogger<ConfigurationValidatorHostedService> logger,
            IConfiguration configuration,
            IOptions<SecurityOptions> securityOptions)
        {
            _logger = logger;
            _configuration = configuration;
            _security = securityOptions.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ValidateConnectionString();
            ValidateInternalToken();
            ValidateAllowedOrigins();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void ValidateConnectionString()
        {
            // tu utilises "Default" comme nom de CS, on reste cohérent avec ça
            var cs = _configuration.GetConnectionString("Default");

            if (string.IsNullOrWhiteSpace(cs))
            {
                _logger.LogWarning("Configuration: connection string 'Default' is missing or empty.");
            }
        }

        private void ValidateInternalToken()
        {
            if (string.IsNullOrWhiteSpace(_security.InternalToken))
            {
                _logger.LogWarning("Configuration: Security.InternalToken is missing or empty.");
                return;
            }

            if (_security.InternalToken.StartsWith("change-me", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Configuration: Security.InternalToken uses a placeholder value ('change-me-*'). Replace it with a real secret (user-secrets / env vars).");
            }
        }

        private void ValidateAllowedOrigins()
        {
            if (_security.AllowedOrigins is null || _security.AllowedOrigins.Length == 0)
            {
                _logger.LogWarning("Configuration: Security.AllowedOrigins is empty. CORS will block all browser calls.");
                return;
            }

            if (_security.AllowedOrigins.Any(o => o == "*" || o == "/*"))
            {
                _logger.LogWarning("Configuration: Security.AllowedOrigins contains a wildcard ('*'). This weakens CORS restrictions.");
            }
        }
    }
}
