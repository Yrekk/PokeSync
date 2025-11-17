namespace PokeSync.Api.Options
{
    /// <summary>
    /// Options liées à la sécurité interne (token MuleSoft + CORS front).
    /// Bindée sur la section "Security" de la configuration.
    /// </summary>
    public sealed class SecurityOptions
    {
        public const string SectionName = "Security";

        /// <summary>
        /// Clé secrète partagée avec MuleSoft pour sécuriser les endpoints internes.
        /// Doit être fournie via user-secrets (dev) ou variables d'environnement (prod).
        /// </summary>
        public string InternalToken { get; init; } = string.Empty;

        /// <summary>
        /// Liste des origines front autorisées pour le CORS.
        /// Exemple: http://localhost:4200, https://pokesync.yrekk.dev
        /// </summary>
        public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
    }
}
