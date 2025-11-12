public sealed class SystemConfig
{
    public int Id { get; set; } = 1;                   // singleton
    public DateTimeOffset? LastSyncUtc { get; set; }   // null si jamais synchronisé
    public bool BootstrapInProgress { get; set; }      // true si bootstrap en cours
}
