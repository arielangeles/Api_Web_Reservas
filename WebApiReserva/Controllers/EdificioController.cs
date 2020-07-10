﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApiReserva.Models;
using WebApiReserva.Utilities;
using static WebApiReserva.Utilities.LogUtilities;

namespace WebApiReserva.Controllers
{
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
            var curso = db.tblCurso.Where(c => c.idEdificio == id).FirstOrDefault();

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
        public IHttpActionResult GetCursosDisponibles(Date date)
        {
            Good(log);
            if(date.idSemana > 12 || date.idDia > 7)
            {
                log.Ok = false;
                log.ErrorMessage = "Esa semana/dia no existe";
                return Ok(log);
            }

            var clase = db.tblClase.Where(c => c.idDias == date.idDia).ToList();
            int ocupado = 0, disponible = 0;
            List<string> cursos = new List<string>();

            foreach (var c in clase)
            {
                for (int i = 0; i < 16; i++)
                {
                    if (i >= c.idHoraIn && i <= c.idHoraF) ocupado++;
                    else disponible++;
                }
                
                if(disponible != 0)
                {
                    var course = db.tblCurso.Where(l => l.idCurso == c.idCurso).FirstOrDefault();
                    cursos.Add(course.NumCurso);                     
                }
            }
                      
            var reserva = db.GetReservaSemana(date.idSemana).ToList();

            foreach (var r in reserva)
            {
                if(r.idDias == date.idDia)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        if (i >= r.idHoraIn && i <= r.idHoraF) ocupado++;
                        else disponible++;
                    }

                    var course = db.tblCurso.Where(l => l.idCurso == r.idCurso).FirstOrDefault();

                    if (disponible != 0 && !cursos.Contains(course.NumCurso))
                    {
                        cursos.Add(course.NumCurso);
                    }
                }
            }

            if (disponible == 0)
            {
                log.Ok = false;
                log.ErrorMessage = "No hay cursos disponibles";
                return Ok(log);
            }

            List<CursoEdificio> listaResult = new List<CursoEdificio>();
            int cantidadEdificios = db.tblEdificio.Select(e => e.idEdificio).Distinct().Count();
            int edificioActual = 0;

            for (int i = 0; i < cantidadEdificios; i++)
            {
                CursoEdificio cursoEdificio = new CursoEdificio();
                edificioActual = i+1;
                cursoEdificio.Edificio = db.tblEdificio.Where(e => e.idEdificio == edificioActual).Select(l => l.Edificio).FirstOrDefault();

                for (int j = 0; j < cursos.Count(); j++)
                {
                    string cursoActual = cursos[j];
                    if(db.tblCurso.Where(c => c.NumCurso == cursoActual).Select(l => l.idEdificio).FirstOrDefault() == edificioActual)
                    {
                        cursoEdificio.Cursos.Add(cursoActual);
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
