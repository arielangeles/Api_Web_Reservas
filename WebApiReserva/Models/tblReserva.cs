//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebApiReserva.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class tblReserva
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tblReserva()
        {
            this.tblGrupoReserva = new HashSet<tblGrupoReserva>();
        }
    
        public int idReserva { get; set; }
        public int idCurso { get; set; }
        public byte idSemana { get; set; }
        public byte idDias { get; set; }
        public byte idHoraIn { get; set; }
        public byte idHoraF { get; set; }
        public int idReservante { get; set; }
        public bool EstadoReserva { get; set; }
        public System.DateTime FechaRegistro { get; set; }
        public System.DateTime FechaReserva { get; set; }
    
        public virtual tblCurso tblCurso { get; set; }
        public virtual tblDias tblDias { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tblGrupoReserva> tblGrupoReserva { get; set; }
        public virtual tblHoras tblHoras { get; set; }
        public virtual tblHoras tblHoras1 { get; set; }
        public virtual tblPersona tblPersona { get; set; }
        public virtual tblSemana tblSemana { get; set; }
    }
}
