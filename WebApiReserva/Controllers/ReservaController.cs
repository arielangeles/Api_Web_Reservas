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
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ReservaController : ApiController
    {
        private ReservaEntities db = new ReservaEntities();
        private Logger log = new Logger();

        public class ReservaPersonas
        {
            public tblReserva Reserva { get; set; }
            public List<int?> IdPersonas { get; set; }
        }

        /// <summary>
        /// Obtiene todas las reservas registradas
        /// </summary>
        // GET: api/Reserva
        [ResponseType(typeof(tblReserva))]
        public IHttpActionResult GetAll()
        {
            Good(log);
            // Get reserva where Estado = 1/true - sp
            var reserva = db.tblReserva.Where(r => r.EstadoReserva == true).ToList();
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
            //  Get reserva where Estado = 1/true and idReservante == id
            var reserva = db.tblReserva.Where(r=> r.idReservante == id).ToList();
            var result = MergeLogResult(log, reserva);
            return Ok(result);
        }

        /// <summary>
        /// Obtiene todas las reservas a las que pertenece una persona con el id
        /// </summary>
        // GET: api/Reserva
        //[ResponseType(typeof(tblReserva))]
        public IHttpActionResult GetReservaGrupo(int id)
        {
            Good(log);
            if (!UserExists(id))
            {
                log.Ok = false;
                log.ErrorMessage = "El usuario no existe";
                return Ok(log);
            }

            // Get GrupoReserva where idPersona = @id - sp
            var grupoRes = db.tblGrupoReserva.Where(g => g.idPersona == id).ToList();
            List<tblReserva> reservas = new List<tblReserva>();
            foreach (var grupo in grupoRes)
            {
                // GetReserva where idReserva = @idReserva and Estado = 1 - sp
                var res = db.tblReserva.Where(r => r.idReserva == grupo.idReserva).ToList();
                foreach (var r in res)
                {
                    reservas.Add(r);
                }
                
            }

            var result = MergeLogResult(log, reservas);

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
            /* Method to get Horario de Reservas by Semana */
            // GetReservaCursoSemana(int numeroSemana, idCurso) Select * from tblReserva where idSemana = numeroSemana and 
            //var semana = db.GetReservaSemana(id).ToList();
            var semana = db.tblReserva.Where(r => r.idSemana == id && r.idCurso == idCurso).ToList();

            List<int[]> horario = GetSemanaList(semana, idCurso);

            var result = MergeLogResult(log, horario);

            return Ok(result.ToList());
        }

        /// <summary>
        /// Obtiene un arreglo de las personas que pertenecen a una reserva con el ID de reserva.
        /// </summary>
        // GET: api/Reserva
        [HttpGet()]
        [ActionName("GetPersonasByReserva")]
        public IHttpActionResult GetReservaById(int id)
        {
            if (!ReservaExists(id) || !ReservaIsActive(id))
            {
                log.Ok = false;
                log.ErrorMessage = "Esta reserva no existe";
            }
            List<int> idList = new List<int>();
            // Get ReservaById Select top 1 idReservante from tblReserva where idReserva = @idReserva and Estado = 1 -sp
            int idPersona = db.tblReserva.Where(r => r.idReserva == id).Select(c => c.idReservante).FirstOrDefault();
            idList.Add(idPersona);
            // Get GrupoReservaById Select idPersona from tblGrupoReserva where idReserva = @idReserva and Estado = 1 -sp
            var idsGrupo = db.tblGrupoReserva.Where(r => r.idReserva == id).Select(c => c.idPersona).ToList();
            
            if(idsGrupo != null) foreach (int ids in idsGrupo) idList.Add(ids);

            List<Persona> personas = new List<Persona>();
            for (int i = 0; i < (idsGrupo.Count() + 1); i++)
            {               
                int idPersonaActual = idList[i];
                // GetIdPersona Select top 1 idPersona where idPersona = @idPersonaActual - sp
                int idP = db.tblPersona.Where(c => c.idPersona == idPersonaActual).Select(c => c.idPersona).FirstOrDefault();
                // GetNamePersona Select top 1 Nombre where idPersona = @idPersonaActual - sp
                string name = db.tblPersona.Where(c => c.idPersona == idPersonaActual).Select(c => c.Nombre).FirstOrDefault();
                Persona persona = new Persona() { IdPersona = idP, Nombre= name};
                personas.Add(persona);
            }

            var result = MergeLogResult(log, personas);
            return Ok(result);

        }

        public class Persona
        {
            public Persona()
            {
            }
            public int IdPersona { get; set; }
            public string Nombre { get; set; }
        }

        /// <summary>
        /// Agrega una reserva nueva
        /// </summary>
        //POST: api/Reserva
        [HttpPost]
        //[ResponseType(typeof(ReservaPersonas))]
        [ActionName("Add")]
        public IHttpActionResult AddReserva([FromBody] ReservaPersonas reservaP)
        {
            if (ReservaExists(reservaP.Reserva.idReserva))
            {
                log.Ok = false;
                log.ErrorMessage = "Esta reserva ya esta registrada";
                return Ok(log);

            }

            try
            {
                db.tblReserva.Add(reservaP.Reserva);
                db.SaveChanges();             
            }
            catch (Exception ex)
            {
                log.Ok = false;
                log.ErrorMessage = "Error al agregar la reserva " + ex;
                return Ok(log);
            }

            Good(log);
            if (reservaP.IdPersonas.Count != 0)
            {
                foreach (var persona in reservaP.IdPersonas)
                {
                    tblGrupoReserva grupo = new tblGrupoReserva() { idReserva = reservaP.Reserva.idReserva, idPersona = persona.Value};
                    try
                    {
                        db.tblGrupoReserva.Add(grupo);
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
            var v = db.tblReserva.Where(r => r.idReservante == id).FirstOrDefault();
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

        // Cuando son reservas en tblReserva, se elimina la reserva completa
        [HttpPut]
        public IHttpActionResult EliminarReserva(int id, tblReserva reserva)
        {
            if (reserva.idReservante != id)
            {
                log.Ok = false;
                log.ErrorMessage = "Este usuario no es el dueño de la reserva";
                return Ok(log);
            }

            Remove(reserva);
           
            return Ok(reserva);
        }

        // Si son grupos, te puedes salir del grupo (si tiene el limite todavia, se mantiene, sino, se dice que esta desocupado)
        [HttpPut]
        public IHttpActionResult SalirGrupoReserva(tblGrupoReserva grupoRes)
        {
            tblReserva reserva = db.tblReserva.Find(grupoRes.idReserva);
            if (reserva == null)
            {
                log.Ok = false;
                log.ErrorMessage = "Esta reserva no existe";
                return Ok(log);
            }

            db.tblGrupoReserva.Remove(grupoRes);
            db.SaveChanges();

            var cantGrupo = db.tblGrupoReserva.Where(g => g.idReserva == grupoRes.idReserva).Distinct().ToList().Count();

            //var pers = db.tblPersonaTipo.Where(p => p.idPersona == grupoRes.idPersona).ToList();
            // GetTipo Select Top 1 
            var res = db.tblPersonaTipo.Where(p => p.idPersona == reserva.idReservante).Select(d => d.idTipo).ToList();

            int estudiante = 0, profesor = 0, tutor = 0;
            foreach (int estado in res)
            {
                if (estado == 6) estudiante++; // Es estudiante
                if (estado == 7) profesor++; // Es profesor
                if (estado == 8) tutor++; // Es tutor

            }

            if (estudiante == 1 && tutor == 0 && profesor == 0)
            {
                if (cantGrupo < 3) 
                {
                    var reservaActual = db.tblReserva.Where(r => r.idReserva == grupoRes.idReserva).FirstOrDefault();
                    Remove(reservaActual);
                
                } // Liberar reserva
            }

            return Ok(reserva);

        }



        //[NonAction]
        private int[] GetHoras(int nDia, List<tblReserva> semana, int idCurso)
        {
            int[] dia = new int[15];
            // GetClaseCurso Select * from tblClase where idCurso = idCurso
            var clase = db.tblClase.Where(c => c.idCurso == idCurso).ToList();

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
                        //tblHoras horaIn = db.tblHoras.Where(h => h.idHoras == c.idHoraIn).FirstOrDefault();
                        //tblHoras horaF = db.tblHoras.Where(h => h.idHoras == c.idHoraF).FirstOrDefault();
                        //for (int l = 0; l < 16; l++)
                        //{
                        //    if (l >= (c.idHoraIn - 7) && l < (c.idHoraF - 7))
                        //    {
                        //        if (dia[l] == 1) dia[l] = 3; // Solapado
                        //        else dia[l] = 2; // Clase
                        //    }
                        //}

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
                    //tblHoras horaIn = db.tblHoras.Where(h => h.idHoras == d.idHoraIn).FirstOrDefault();
                    //tblHoras horaF = db.tblHoras.Where(h => h.idHoras == d.idHoraF).FirstOrDefault();
                    //for (int i = 0; i < 15; i++)
                    //{
                    //    if (i >= (d.idHoraIn - 7) && i < (d.idHoraF - 7))
                    //    {
                    //        //if (dia[i] == 2) dia[i] = 3; // Solapado
                    //        /*else */
                    //        dia[i] = 1; // Reservado
                    //    }
                    //    else /*if (dia[i] != 2)*/
                    //    {
                    //        dia[i] = 0; // Disponible    
                    //    }

                    //}

                    for (int i = (d.idHoraIn - 7); i < (d.idHoraF - 7); i++)
                    {
                        dia[i] = 1; // Reservado
                    }

                    //if (i < (d.idHoraIn - 7) && i > (d.idHoraF - 7))
                    //{
                    //    dia[i] = 0; // libre
                    //}


                }
            }
            return dia;
        }

        //[NonAction]
        private List<int[]> GetSemanaList(List<tblReserva> semana, int idCurso)
        {
            List<int[]> semanaList = new List<int[]>();

            for (int i = 1; i < 8; i++)
            {
                semanaList.Add(GetHoras(i, semana, idCurso));
            }
            return semanaList;
        }

        private bool ReservaExists(int id)
        {
            return db.tblReserva.Count(e => e.idReserva == id) > 0;
        }

        private bool ReservaIsActive(int id)
        {
            return db.tblReserva.Where(c => c.idReserva == id && c.EstadoReserva == true).FirstOrDefault() != null;
        }

        private bool UserExists(int id)
        {
            return db.tblPersona.Count(e => e.idPersona == id) > 0;
        }

        private void Remove(tblReserva reserva)
        {
            reserva.EstadoReserva = false;
            db.SaveChanges();
        }
    }
}
