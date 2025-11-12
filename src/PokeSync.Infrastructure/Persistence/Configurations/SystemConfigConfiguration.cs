using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.LastSyncUtc)
         .HasColumnType("datetimeoffset"); // UTC par convention
    }
}
