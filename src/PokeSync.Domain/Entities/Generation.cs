using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Domain.Entities
{
    public class Generation
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;

        //Navigation
        public ICollection<Pokemon> Pokemons { get; set; } = new List<Pokemon>();   
    }
}
