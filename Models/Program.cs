using System.Collections.Generic;

namespace Models
{
    public class Program
    {
        public string Id { get; set; }
        public string Naziv { get; set; }
        public int Trajanje { get; set; }
        public int BrojMesta { get; set; }
        public string NivoStudija { get; set; }
        public string Opis { get; set; }
        public string Jezik { get; set; }
        public string  Univerzitet { get; set; }

        public double Procenat { get; set; }

        //public Univerzitet Univerzitet { get; set; }
        //public List<Oblast> Oblasti { get; set; }
    }
}