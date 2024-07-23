using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Models
{
    public class Grad
    {
        public string Id { get; set; }
        public string Naziv { get; set; }
        public int Troskovi { get; set; }
        
        //public Drzava Drzava { get; set; }
        //public List<StudentskiDom> StudentskiDomovi { get; set; }
        //public List<Univerzitet> Univerziteti { get; set; }
    }
}