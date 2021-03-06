using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaInventario.AccesoDatos.Data;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using SistemaInventario.Modelos.ViewModels;
using SistemaInventario.Utilidades;

namespace SistemaInventario.Areas.Inventario.Controllers
{
    [Area("Inventario")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnidadTrabajo _unidadTrabajo;
        private readonly ApplicationDbContext _db;

        [BindProperty]
        public CarroComprasVM CComprasVM { get; set; }

        public HomeController(ILogger<HomeController> logger, IUnidadTrabajo unidadTrabajo, ApplicationDbContext db)
        {
            _logger = logger;
            _unidadTrabajo = unidadTrabajo;
            _db = db;
        }

        public IActionResult Index()
        {
            IEnumerable<Producto> productoLista = _unidadTrabajo.Producto.ObtenerTodos(incluirPropiedades: "Categoria,Marca");
            var claimIdentidad = (ClaimsIdentity)User.Identity;
            var claim = claimIdentidad.FindFirst(ClaimTypes.NameIdentifier);
            if(claim != null)
            {
                var numeroProductos = _unidadTrabajo.CarroCompras.ObtenerTodos(c => c.UsuarioAplicacionId == claim.Value).ToList().Count();
                HttpContext.Session.SetInt32(DS.ssCarroCompras, numeroProductos);
            }
            return View(productoLista);
        }

        public IActionResult Detalle(int id)
        {
            CComprasVM = new CarroComprasVM();
            CComprasVM.Compania = _db.Compania.FirstOrDefault();
            CComprasVM.BodegaProducto = _db.BodegaProducto.
                Include(p => p.Producto).
                Include(p => p.Producto.Categoria).
                Include(p => p.Producto.Marca).
                FirstOrDefault(b => b.ProductoId == id && b.BodegaId == CComprasVM.Compania.BodegaVentaId);

            if(CComprasVM.BodegaProducto == null)
            {
                return RedirectToAction("Index");
            }
            else
            {
                CComprasVM.CarroCompras = new CarroCompras() 
                { 
                    Producto = CComprasVM.BodegaProducto.Producto,
                    ProductoId = CComprasVM.BodegaProducto.ProductoId
                };
                return View(CComprasVM);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Detalle(CarroComprasVM carroComprasVM)
        {
            var claimIdentidad = (ClaimsIdentity)User.Identity;
            var claim = claimIdentidad.FindFirst(ClaimTypes.NameIdentifier);
            carroComprasVM.CarroCompras.UsuarioAplicacionId = claim.Value;

            CarroCompras carroDb = _unidadTrabajo.CarroCompras.ObtenerPrimero(
                    u => u.UsuarioAplicacionId == carroComprasVM.CarroCompras.UsuarioAplicacionId
                    && u.ProductoId == carroComprasVM.CarroCompras.ProductoId,
                    incluirPropiedades: "Producto");

            if(carroDb == null)
            {
                // no hay usuario para el carro de compras
                _unidadTrabajo.CarroCompras.Ageregar(carroComprasVM.CarroCompras);
            }
            else
            {
                carroDb.Cantidad += carroComprasVM.CarroCompras.Cantidad;
                _unidadTrabajo.CarroCompras.Actualizar(carroDb);
            }

            _unidadTrabajo.Guardar();

            // agregar valor a la sesión
            var numeroProductos = _unidadTrabajo.CarroCompras.ObtenerTodos(c => c.UsuarioAplicacionId == carroComprasVM.CarroCompras.UsuarioAplicacionId).ToList().Count();
            HttpContext.Session.SetInt32(DS.ssCarroCompras, numeroProductos);
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
