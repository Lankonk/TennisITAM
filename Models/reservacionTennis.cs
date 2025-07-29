using System.ComponentModel.DataAnnotations;

namespace TennisITAM.Models
{
    public class reservacionTennis
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Requerido")]
        public int idU1 { get; set;}
        [Required(ErrorMessage = "Requerido")]
        public int idU2 { get; set; }
        public int? idU3 { get; set; }
        public int? idU4 { get; set; }
        [Required(ErrorMessage = "Requerido")]
        public DateTime hReserva { get; set; }
        [Required(ErrorMessage = "Requerido")]
        public string recReservado { get; set; }


        public reservacionTennis()
        {

        }

        public reservacionTennis(int cu1, int cu2, DateTime hReserva, string recReservado)
        {
            this.idU1 = cu1;
            this.idU2 = cu2;
            this.hReserva = hReserva;
            this.recReservado = recReservado;
        }

        public reservacionTennis(int cu1, int cu2, int? cu3, int? cu4, DateTime hReserva, string recReservado)
        {
            this.idU1 = cu1;
            this.idU2 = cu2;
            this.idU3 = cu3;
            this.idU4 = cu4;
            this.hReserva = hReserva;
            this.recReservado = recReservado;
        }
    }
}
