using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Domain.Entities
{
    // Prevents duplicate processing of identical external requests
    // by storing a unique key for each idempotent operation.
    public class IdempotencyKey
    {
        public int Id { get; set; }
        public string ExternalKey { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;


    }
}
