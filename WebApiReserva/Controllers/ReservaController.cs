﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Http.ModelBinding;
using WebApiReserva.Models;
using WebApiReserva.Utilities;
using static WebApiReserva.Utilities.LogUtilities;

namespace WebApiReserva.Controllers
{
    public class ReservaController : ApiController
    {
        private ReservaEntities db = new ReservaEntities();
        private Logger log = new Logger();        

        /// <summary>
        /// Obtiene todas las reservas registradas
        /// </summary>
        // GET: api/Reserva
        [ResponseType(typeof(tblReserva))]
        public IHttpActionResult GetAll()
        {
            Good(log);
            // Get reserva where Estado = 1/true - sp
            //var reserva = db.tblReserva.Where(r => r.EstadoReserva == true).ToList(); // - El que funcionaba
            // SELECT * FROM tblReservas WHERE EstadoReserva = 1
            var reserva = db.GetAllReservas().ToList(); // -sp
            var result = MergeLogResult(log, reserva);
            return Ok(result);
        }

        /// <summary>
        /// Obtiene todas las reservas registradas por la persona con el id
        /// </summary>
        // GET: api/Reserva
        [ResponseType(typeof(tblReserva))]
        public IHttpActionResult GetReserva(int id)
        {
            Good(log);
            //Get reserva where Estado = 1/true and idReservante == id
            //var reserva = db.tblReserva.Where(r=> r.idReservante == id && r.EstadoReserva == true).ToList(); // - El que funcionaba
            // SELECT * FROM tblReserva WHERE idReservante = @id AND EstadoReserva = 1
            var reserva = db.GetReservaByIdPersona(id).ToList(); // - sp
            var result = MergeLogResult(log, reserva);
            return Ok(result);
        }

        /// <summary>
        /// Obtiene todas las reservas a las que pertenece una persona con el id
        /// </summary>
        // GET: api/Reserva
        [HttpGet]
        [ActionName("GetReservaGrupo")]
        //[ResponseType(typeof(tblReserva))]
        public IHttpActionResult GetReservaG(int id)
        {
            Good(log);
            if (!UserExists(id))
            {
                log.Ok = false;
                log.ErrorMessage = "El usuario no existe";
                return Ok(log);
            }
            var getResGrupo = db.GetReservaGrupo(id).ToList();

            var result = MergeLogResult(log, getResGrupo);

            return Ok(result);
        }


        /// <summary>
        /// Obtiene un arreglo del horario por semana.
        /// </summary>
        // GET: api/Reserva/5
        [HttpGet]
        [ActionName("GetSemana")]
        public IHttpActionResult GetHorarioBySemana(int id, int idCurso) // id = numeroSemana
        {
            var semana = db.GetReservaSemana(id, idCurso).ToList(); // - sp
            //var semana = db.tblReserva.Where(r => r.idSemana == id && r.idCurso == idCurso).ToList(); // - El que funcionaba

            List<int[]> horario = GetSemanaList(semana, idCurso);

            var result = MergeLogResult(log, horario);

            return Ok(result.ToList());
        }

        /// <summary>
        /// Obtiene un arreglo de las personas que pertenecen a una reserva con el ID de reserva.
        /// </summary>
        // GET: api/Reserva
        [HttpGet]
        [ActionName("GetPersonasByReserva")]
        public IHttpActionResult GetReservaById(int id)
        {
            var reserva = db.tblReserva.Find(id);
            if(reserva == null)
            {
                log.Ok = false;
                log.ErrorMessage = "Esta reserva no existe";
                return Ok(log);
            }
            List<int> idList = new List<int>();
            // Get ReservaById Select top 1 idReservante from tblReserva where idReserva = @idReserva and Estado = 1
            //int idPersona = db.tblReserva.Where(r => r.idReserva == id).Select(c => c.idReservante).FirstOrDefault(); // - El que funcionaba
            // SELECT idReservante FROM tblReserva WHERE idReserva = @idReserva AND EstadoResera = 1
            int idPersona = db.GetIdPersonaReserva(id).FirstOrDefault().Value; // - sp
            idList.Add(idPersona);
            // Get GrupoReservaById Select idPersona from tblGrupoReserva where idReserva = @idReserva and Estado = 1
            //var idsGrupo = db.tblGrupoReserva.Where(r => r.idReserva == id).Select(c => c.idPersona).ToList(); // - El que funcionaba
            // SELECT * FROM tblGrupoReserva WHERE idReserva = @idReserva AND EstadoReserva = 1
            var idsGrupo = db.GetIdsGrupoReserva(id).ToList(); // -sp
            
            if(idsGrupo != null) foreach (var ids in idsGrupo) idList.Add(ids.idPersona);

            List<Persona> personas = new List<Persona>();
            for (int i = 0; i < (idsGrupo.Count() + 1); i++)
            {               
                int idPersonaActual = idList[i];
                // GetIdPersona Select top 1 idPersona where idPersona = @idPersonaActual - sp
                //int idP = db.tblPersona.Where(c => c.idPersona == idPersonaActual).Select(c => c.idPersona).FirstOrDefault(); - El que funcionaba
                // SELECT IdPersona FROM tblPersona WHERE idPersona = @idPersona 
                int idP = db.GetIdPersona(idPersonaActual).First().GetValueOrDefault(); // -sp
                // GetNamePersona Select top 1 Nombre where idPersona = @idPersonaActual - sp
                //string name = db.tblPersona.Where(c => c.idPersona == idPersonaActual).Select(c => c.Nombre).FirstOrDefault(); - El que funcionaba
                // SELECT Nombre FROM tblPersona WHERE idPersona = @idPersona 
                string name = db.GetNombrePersona(idPersonaActual).FirstOrDefault(); // -sp
                Persona persona = new Persona() { IdPersona = idP, Nombre= name};
                personas.Add(persona);
            }

            var result = MergeLogResult(log, personas);
            return Ok(result);

        }
       

        /// <summary>
        /// Agrega una reserva nueva
        /// </summary>
        //POST: api/Reserva
        [HttpPost]
        [ActionName("Add")]
        public IHttpActionResult AddReserva([FromBody] ReservaPersonas reservaP)
        {

            try
            {
                // INSERT INTO tblReserva ( idCurso, idSemana, idDia, idHoraIn, idHoraF, idReservante, idFechaReserva ) VALUES (@idCurso, @idSemana, @idDia, @idHoraIn, @idHoraF, @idReservante, @idFechaReserva)
                db.AddReserseva(reservaP.Reserva.idCurso, reservaP.Reserva.idSemana, reservaP.Reserva.idDia, reservaP.Reserva.idHoraIn, reservaP.Reserva.idHoraF, reservaP.Reserva.idReservante, reservaP.Reserva.FechaReserva);
                db.SaveChanges();             
            }
            catch (Exception ex)
            {
                log.Ok = false;
                log.ErrorMessage = "Error al agregar la reserva " + ex;
                return Ok(log);
            }

            Good(log);
            // SELECT TOP 1 idReserva FROM tblReserva ORDER BY idReserva DESC
            var IdReserva = db.getLastReserva().FirstOrDefault().idReserva;
            if (reservaP.IdPersonas.Count != 0)
            {
                foreach (var persona in reservaP.IdPersonas)
                {
                    tblGrupoReserva grupo = new tblGrupoReserva() { idReserva = IdReserva, idPersona = persona.Value };
                    try
                    {
                        //db.tblGrupoReserva.Add(grupo); // - El que funcionaba
                        // INSERT INTO tblGrupoReserva (idReserva, idPersona) VALUES (@idReserva, @idPersona)
                        db.AddGrupoReserva(IdReserva, persona.Value); // -sp
                        db.SaveChanges();
                    }
                    catch (Exception)
                    {
                        log.Ok = false;
                        log.ErrorMessage = "Hubo un error al agregar los integrantes";
                    }

                }
            }

            return Ok(log);
        }

        /// <summary>
        /// Edita una reserva y valida que esta exista.
        /// </summary>
        // PUT: api/Reserva/5
        [HttpPut]
        public IHttpActionResult EditReserva(int id, [FromBody]tblReserva reserva)
        {
            Good(log);
            //var v = db.tblReserva.Where(r => r.idReservante == id).FirstOrDefault(); // - El que funcionaba
            // SELECT * FROM tblReserva WHERE idReservante = @idReservante AND EstadoReserva = 1
            var v = db.GetReservaByIdPersona(id).ToList(); // -sp
            if (v == null)
            {
                log.Ok = false;
                log.ErrorMessage = "El usuario no ha hecho una reserva";
                return Ok(log);
            }

            try
            {
                db.Entry(reserva).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception)
            {
                log.Ok = false;
                log.ErrorMessage = "Hubo un error al editar el usuario";
                return Ok(log);
            }

            return Ok(log);

        }

        /// <summary>
        /// Elimina la reserva con el idReserva
        /// </summary>
        [HttpPut]
        public IHttpActionResult EliminarReserva(int id)
        {
            try
            {
                //UPDATE tblReserva SET EstadoReserva = 0 WHERE idReserva = @idReserva
                db.deleteReserva(id);
                db.SaveChanges();
            }
            catch (Exception)
            {
                log.Ok = false;
                log.ErrorMessage = "Hubo un problema al eliminar la reserva";
            }
            
            //Remove(reserva);
            Good(log);
            return Ok(log);
        }


        /// <summary>
        /// Elimina al integrante que quiere salir del grupo, y si no cumple con los requisitos la reserva se elimina
        /// </summary>
        [HttpPut]
        public IHttpActionResult SalirGrupoReserva(int id)
        {
            //var res = db.tblGrupoReserva.Where(g => g.idGrupoReserva == id).FirstOrDefault(); // - El que funcionaba
            // SELECT * FROM tblGrupoReserva WHERE idGrupoReserva = @idGrupoReserva
            var res = db.GetGrupoReservaById(id).FirstOrDefault(); // -sp
            
            //tblReserva reserva = db.tblReserva.Find(res.idReserva); // -El que funcionaba
            
            var reserva = db.GetReservaByIdRes(res.idReserva).FirstOrDefault(); // -sp
            if (reserva == null)
            {
                log.Ok = false;
                log.ErrorMessage = "Esta reserva no existe";
                return Ok(log);
            }

            try
            {
                db.sp_DeletePersonaRes(id);
                db.SaveChanges();
            }
            catch (Exception)
            {
                log.Ok = false;
                log.ErrorMessage = "Hubo un error al eliminar al usuario";
                return Ok(log);
            }
           
            var cantidadGrupo = db.CantidadPersonasGrupoReserva(res.idReserva).FirstOrDefault().Value;

            //var resi = db.tblPersonaTipo.Where(p => p.idPersona == reserva.idReservante).Select(d => d.idTipo).ToList(); // -El que funcionaba
            var resi = db.GetEstadoTipo(reserva.idReservante).ToList(); // -sp

            int estudiante = 0, profesor = 0, tutor = 0;
            foreach (int estado in resi)
            {
                if (estado == 6) estudiante++; // Es estudiante
                else if (estado == 7) profesor++; // Es profesor
                else if (estado == 8) tutor++; // Es tutor

            }

            if (estudiante == 1 && tutor == 0 && profesor == 0)
            {
                if (cantidadGrupo < 3) 
                {
                    //var reservaActual = db.tblReserva.Where(r => r.idReserva == res.idReserva).FirstOrDefault(); // - El que funcionaba
                    var reservaActual = db.GetReservaByIdRes(res.idReserva).FirstOrDefault(); // -sp
                    try
                    {
                        db.deleteReserva(reservaActual.idReserva);
                        db.SaveChanges();
                    }
                    catch (Exception)
                    {
                        log.Ok = false;
                        log.ErrorMessage = "Hubo un problema al eliminar la reserva";
                        return Ok(log);
                    }
                    
                
                } // Liberar reserva
            }
            Good(log);

            return Ok(log);

        }

        public class GrupoReserva
        {
            public int idPersona { get; set; }
            public int idReserva { get; set; }
        }



        //[NonAction]
        private int[] GetHoras(int nDia, List<GetReservaSemana_sp> semana, int idCurso)
        {
            int[] dia = new int[15];
            // GetClaseCurso Select * from tblClase where idCurso = idCurso
            //var clase = db.tblClase.Where(c => c.idCurso == idCurso).ToList(); // - El que funcionaba
            var clase = db.GetClase(idCurso).ToList(); // -sp

            for (int i = 0; i < 15; i++)
            {
                dia[i] = 0; // disponible
            }

            if(clase != null)
            {
                foreach (var c in clase)
                {
                    if (c.idDias == nDia)
                    {

                        for (int i = (c.idHoraIn - 7); i < (c.idHoraF - 7); i++)
                        {
                            dia[i] = 2; //Clase
                        }

                    }

                }

            }

           
            
            foreach (var d in semana)
            {
                if (d.idDias == nDia)
                {


                    for (int i = (d.idHoraIn - 7); i < (d.idHoraF - 7); i++)
                    {
                        dia[i] = 1; // Reservado
                    }

                }
            }
            return dia;
        }

        //[NonAction]
        private List<int[]> GetSemanaList(List<GetReservaSemana_sp> semana, int idCurso)
        {
            List<int[]> semanaList = new List<int[]>();

            for (int i = 1; i < 8; i++)
            {
                semanaList.Add(GetHoras(i, semana, idCurso));
            }
            return semanaList;
        }

        private bool UserExists(int id)
        {
            return db.tblPersona.Count(e => e.idPersona == id) > 0;
        }



        public class ReservaR
        {
            public int idCurso { get; set; }
            public int idSemana { get; set; }
            public int idDia { get; set; }
            public int idHoraIn { get; set; }
            public int idHoraF { get; set; }
            public int idReservante { get; set; }
            public DateTime FechaReserva { get; set; }
        }
        public class ReservaPersonas
        {
            public ReservaR Reserva { get; set; }
            public List<int?> IdPersonas { get; set; }
        }
        public class Persona
        {
            public Persona()
            {
            }
            public int IdPersona { get; set; }
            public string Nombre { get; set; }
        }
    }
}
