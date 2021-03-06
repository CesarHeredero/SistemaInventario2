using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using SistemaInventario.Utilidades;

namespace SistemaInventario.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = DS.Role_Admin)]
    public class CategoriaController : Controller
    {
        private readonly IUnidadTrabajo _unidadTrabajo;

        public CategoriaController(IUnidadTrabajo unidadTrabajo)
        {
            _unidadTrabajo = unidadTrabajo;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            Categoria categoria = new Categoria();
            if(id == null)
            {
                return View(categoria);
            }
            else
            {
                categoria = _unidadTrabajo.Categoria.Obtener(id.GetValueOrDefault());
                if(categoria == null)
                {
                    return NotFound();
                }

                return View(categoria);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                if (categoria.Id == 0)
                {
                    _unidadTrabajo.Categoria.Ageregar(categoria);
                }
                else
                {
                    _unidadTrabajo.Categoria.Actualizar(categoria);
                }
                _unidadTrabajo.Guardar();
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }



        #region Api
        // Admin/Categoria/obtenerTodos
        [HttpGet]
        public IActionResult ObtenerTodos()
        {
            var todos = _unidadTrabajo.Categoria.ObtenerTodos();
            return Json(new { data = todos });
        }

        // Admin/Categoria/obtener/1
        [HttpGet]
        public IActionResult Obtener(int id)
        {
            var todos = _unidadTrabajo.Categoria.Obtener(id);
            return Json(new { data = todos });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var categoriaDb = _unidadTrabajo.Categoria.Obtener(id);
            if(categoriaDb == null)
            {
                return Json(new { success = false, message = "Error al borrar" });
            }
            _unidadTrabajo.Categoria.Remover(categoriaDb);
            _unidadTrabajo.Guardar();
            return Json(new { success = true, message = "Categoría borrada con exito" });
        }
        #endregion

    }
}
