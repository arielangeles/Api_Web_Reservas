﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using WebApiReserva.Models;
using WebApiReserva.Utilities;
using static WebApiReserva.Utilities.LogUtilities;


namespace WebApiReserva.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class UserController : ApiController
    {
        private ReservaEntities db = new ReservaEntities();
        private Logger log = new Logger();

        /// <summary>
        /// Obtiene a todas las personas registradas
        /// </summary>
        // GET: api/User
        [ResponseType(typeof(tblPersona))]
        [HttpGet]
        public IHttpActionResult Get()
        {
            // GetPersona Select *  from tblPersona -sp
            var usuario = db.tblPersona.ToList();
            Good(log);

            var result = MergeLogResult(log, usuario);
            return Ok(result);
        }


        /// <summary>
        /// Obtiene los datos del usuario con el ID
        /// </summary>
        /// <param name="id">ID del usuario</param>
        // GET: api/User/5
        [ResponseType(typeof(tblPersona))]
        [HttpGet]
        public IHttpActionResult GetUsuarioById(int id)
        {
            if (!UserExists(id))
            {
                log.Ok = false;
                log.ErrorMessage = "El usuario no existe";
                return Ok(log);
            }

            var user = db.GetUsuario(id).ToList();
            var result = MergeLogResult(log, user);

            return Ok(result);
        }

        /// <summary>
        /// Valida si la persona existe, y que el usuario no este ya registrado. Luego, la registra.
        /// </summary>
        // POST: api/User
        [ResponseType(typeof(tblUsuario))]
        [HttpPost]
        public IHttpActionResult ValidateUserRegister([FromBody]tblUsuario user)
        {
            if (!PersonaExists(user.idUsuario))
            {
                log.Ok = false;
                log.ErrorMessage = "Esta persona no esta registrada";
                return Ok(log);
            }

            if (UserExists(user.idUsuario))
            {
                log.Ok = false;
                log.ErrorMessage = "Este usuario ya esta registrado";
                return Ok(log);
            }

            try
            {
                user.Pass = CryptoPass.Hash(user.Pass);
                db.Entry(user).State = EntityState.Added;
                db.tblUsuario.Add(user);
                db.SaveChanges();
                Good(log);
            }
            catch (Exception)
            {
                log.Ok = false;
                log.ErrorMessage = "Hubo un problema al agregar el usuario";
            }


            return Ok(log);
        }

        /// <summary>
        /// Valida si el usuario introdujo los datos correctos a la hora de iniciar sesion.
        /// </summary>
        // POST: api/User
        [HttpPost]
        public IHttpActionResult ValidateUserLogin([FromBody]tblUsuario user)
        {
            Good(log);
            if (!UserExists(user.idUsuario))
            {
                log.Ok = false;
                log.ErrorMessage = "ID/Matricula no registrada";
                return Ok(log);
            }
            
            var validPassword = db.GetPassword(user.idUsuario).FirstOrDefault();

            if (CryptoPass.Hash(user.Pass) != validPassword)
            {
                log.Ok = false;
                log.ErrorMessage = "Usuario/contraseña no valida";
                return Ok(log);
            }         

            return Ok(log);
        }

        /// <summary>
        /// Edita al usuario y valida que este exista.
        /// </summary>
        // PUT: api/User/5
        [HttpPut]
        public IHttpActionResult EditUser(int id, tblUsuario user)
        {

            if (id != user.idUsuario)
            {
                return BadRequest();
            }

            db.Entry(user).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                log.Ok = false;
                if (!UserExists(id))
                {
                    log.ErrorMessage = "El usuario no existe";
                    return Ok(log);
                }
                else
                {
                    log.ErrorMessage = "Hubo un error al editar el usuario";
                    return Ok(log);
                }
            }

            return Ok(log);
        }

        /// <summary>
        /// Verifica que el usuario exista a traves del ID
        /// </summary>
        public IHttpActionResult VerifyUserExists(int id)
        {
           
            if (db.tblUsuario.Count(u => u.idUsuario == id) > 0)
            {
                Good(log);
            }            
            else
            {
                log.Ok = false;
                log.ErrorMessage = "El usuario no existe";
            }
            return Ok(log);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool PersonaExists(int id)
        {
            return db.tblPersona.Count(e => e.idPersona == id) > 0;
        }

        private bool UserExists(int id)
        {
            return db.tblUsuario.Count(e => e.idUsuario == id) > 0;
        }

    }
}