using System.Collections.Generic;

namespace Models
{
    public class Univerzitet
    {
        public string Id { get; set; }
        public string Naziv { get; set; }
        public string Opis { get; set; }
        public string Kontakt { get; set; }
        public string Adresa { get; set; }
        public int Skolarina { get; set; }

        //public Grad Grad { get; set; }
        //public List<Stipendija> Stipendije { get; set; }
        //public List<Program> Programi { get; set; }
        //public List<Sertifikat> Sertifikati { get; set; }
    }
}