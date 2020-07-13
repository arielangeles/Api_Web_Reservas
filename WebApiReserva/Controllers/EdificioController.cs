﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApiReserva.Models;
using WebApiReserva.Utilities;
using static WebApiReserva.Utilities.LogUtilities;

namespace WebApiReserva.Controllers
{
    //[EnableCors(origins: "*", headers: "*", methods: "*")]
    public class EdificioController : ApiController
    {
        private ReservaEntities db = new ReservaEntities();
        private Logger log = new Logger();

        /// <summary>
        /// Obtiene todos los edificios registradas
        /// </summary>
        // GET: api/Edificio
        [HttpGet]
        [ActionName("GetAll")]
        public IHttpActionResult GetEdificio()
        {
            Good(log);
            // GetEdificio Select * from tblEdificio
            var edif = db.tblEdificio.ToList();

            var result = MergeLogResult(log, edif);

            return Ok(result);
        }

        /// <summary>
        /// Obtiene todos los cursos de un edificio con el ID del edificio.
        /// </summary>
        // GET: api/Edificio/{id}
        [HttpGet]
        [ActionName("GetCursos")]
        public IHttpActionResult GetCursosEdificio(int id)
        {
            // GetCursoEdif Select top 1 * from 
            var curso = db.tblCurso.Where(c => c.idEdificio == id).ToList();

            if (curso == null)
            {
                log.Ok = false;
                log.ErrorMessage = "El edificio no existe";
                return Ok(log);
            }

            Good(log);
            var result = MergeLogResult(log, curso);

            return Ok(result);
        }

        /// <summary>
        /// Obtiene todos los cursos disponibles con el dia y la semana.
        /// </summary>
        [HttpGet]
        public IHttpActionResult GetCursosDisponibles([FromUri]Date date)
        {
            Good(log);
            if(date.idSemana > 12 || date.idDia > 7)
            {
                log.Ok = false;
                log.ErrorMessage = "Esa semana/dia no existe";
                return Ok(log);
            }

            List<int> cursosClasesDisp = new List<int>();
            var freeClass = db.GetCursosDisponibleClase(date.idHora, date.idDia).ToList();
            foreach (var clasi in freeClass)
            {
                cursosClasesDisp.Add(clasi.idCurso);
            }

            List<int> cursosReservaDisp = new List<int>();
            var freeRes = db.GetCursosDisponibleReserva(date.idHora, date.idDia, date.idSemana).ToList();
            foreach (var resi in freeRes)
            {
                cursosReservaDisp.Add(resi.idCurso);
            }

            List<int> filterCursosDisp = new List<int>();
            for (int i = 0; i < cursosClasesDisp.Count; i++)
            {
                for (int j = 0; j < cursosReservaDisp.Count; j++)
                {
                    if (cursosClasesDisp[i] == cursosReservaDisp[j])
                    {
                        filterCursosDisp.Add(cursosClasesDisp[j]);
                    }
                }
              
            }

          

            //var clase = db.tblClase.Where(c => c.idDias == date.idDia).ToList();
            //int ocupado = 0, disponible = 0, cont = 0;
            //List<int> cursos = new List<int>();
            //if (clase != null)
            //{
            //    foreach (var c in clase)
            //    {
            //        ocupado = 0;
            //        disponible = 0;

            //        for (int i = 0; i < 15; i++)
            //        {
            //            if (i >= (c.idHoraIn - 7) && i < (c.idHoraF - 7)) ocupado++;
            //            else disponible++;
            //        }

            //        var course = db.tblCurso.Where(l => l.idCurso == c.idCurso).FirstOrDefault();
            //        if (disponible != 0 && !cursos.Contains(course.idCurso))
            //        {                        
            //            cursos.Add(course.idCurso);
            //        }
            //        cont++;
            //    }
            //}
            //else
            //{
            //    log.Ok = false;
            //    log.ErrorMessage = "Aparentemente no hay clases este dia";
            //    return Ok(log);
            //}
           
            //if(cont > 0)
            //{
            //    log.Ok = false;
            //    log.ErrorMessage = "Entro al foreach de clases";
            //    return Ok(log);
            //}

            // Filtrar por semana y por dia
            //var reserva = db.tblReserva.Where(r => r.idSemana == date.idSemana && r.idDias == date.idDia).ToList();
            ////var reserva = db.GetReservaSemana(date.idSemana).ToList();
            //if (reserva != null)
            //{
            //    foreach (var r in reserva)
            //    {
            //        ocupado = 0;
            //        disponible = 0;

            //        for (int i = 0; i < 15; i++)
            //        {
            //            if (i >= (r.idHoraIn - 7) && i < (r.idHoraF - 7)) ocupado++;
            //            else disponible++;
            //        }

            //        var course = db.tblCurso.Where(l => l.idCurso == r.idCurso).FirstOrDefault();

            //        if (disponible != 0 && !cursos.Contains(course.idCurso))
            //        {
            //            cursos.Add(course.idCurso);
            //        }
            //    }

            //}
            //else
            //{
            //    disponible++;
            //}

            //if (disponible == 0)
            //{
            //    log.Ok = false;
            //    log.ErrorMessage = "No hay cursos disponibles";
            //    return Ok(log);
            //}

            //if (cursos.Count > 0)
            //{
            //    log.Ok = false;
            //    log.ErrorMessage = "Lista sin filtro";
            //    var res = MergeLogResult(log, cursos);
            //    return Ok(res);
            //}

            List<CursoEdificio> listaResult = new List<CursoEdificio>();
            int cantidadEdificios = db.tblEdificio.Select(e => e.idEdificio).ToList().Count;
            int edificioActual = 0;



            for (int i = 0; i < cantidadEdificios; i++)
            {
                CursoEdificio cursoEdificio = new CursoEdificio();
                edificioActual = i+1;
                cursoEdificio.Edificio = db.tblEdificio.Where(e => e.idEdificio == edificioActual).Select(l => l.Edificio).FirstOrDefault();

                for (int j = 0; j < filterCursosDisp.Count; j++)
                {
                    int idcursoActual = filterCursosDisp[j];
                    if(db.tblCurso.Where(c => c.idCurso == edificioActual).FirstOrDefault() != null)
                    {
                        var numCurso = db.tblCurso.Where(c => c.idCurso == idcursoActual).Select(l => l.NumCurso).FirstOrDefault();
                        cursoEdificio.Cursos.Add(numCurso);
                    }
                }
                listaResult.Add(cursoEdificio);
            }

            var result = MergeLogResult(log, listaResult);

            return Ok(result);

        }

        public class Date
        {
            public int idSemana { get; set; }
            public int idDia { get; set; }
            public int idHora { get; set; }
        }

        public class CursoEdificio
        {
            public CursoEdificio()
            {
                Cursos = new List<string>();
            }
            public string Edificio { get; set; }
            public List<string> Cursos { get; set; }
        }


    }
}
