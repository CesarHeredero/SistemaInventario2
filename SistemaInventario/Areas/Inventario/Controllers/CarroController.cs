using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;
using SistemaInventario.AccesoDatos.Data;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using SistemaInventario.Modelos.ViewModels;
using SistemaInventario.Utilidades;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaInventario.Areas.Inventario.Controllers
{
    [Area("Inventario")]
    public class CarroController : Controller
    {
        private readonly IUnidadTrabajo _unidadTrabajo;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;

        [BindProperty]
        public CarroComprasVM CarroComprasVM { get; set; }

        public CarroController(IUnidadTrabajo unidadTrabajo, IEmailSender emailSender, UserManager<IdentityUser> userManager, ApplicationDbContext db)
        {
            _unidadTrabajo = unidadTrabajo;
            _emailSender = emailSender;
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Index()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            CarroComprasVM = new CarroComprasVM()
            {
                Orden = new Modelos.Orden(),
                CarroComprasLista = _unidadTrabajo.CarroCompras.ObtenerTodos(u => u.UsuarioAplicacionId == claim.Value, incluirPropiedades: "Producto")
            };

            CarroComprasVM.Orden.TotalOrden = 0;
            CarroComprasVM.Orden.UsuarioAplicacion = _unidadTrabajo.UsuarioAplicacion.ObtenerPrimero(u => u.Id == claim.Value);

            foreach (var lista in CarroComprasVM.CarroComprasLista)
            {
                lista.Precio = lista.Producto.Precio;
                CarroComprasVM.Orden.TotalOrden += (lista.Precio * lista.Cantidad);
            }

            return View(CarroComprasVM);
        }

        public IActionResult Mas(int carroId)
        {
            var carroCompras = _unidadTrabajo.CarroCompras.ObtenerPrimero(c => c.Id == carroId, incluirPropiedades: "Producto");
            carroCompras.Cantidad += 1;
            _unidadTrabajo.Guardar();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Menos(int carroId)
        {
            var carroCompras = _unidadTrabajo.CarroCompras.ObtenerPrimero(c => c.Id == carroId, incluirPropiedades: "Producto");
            if (carroCompras.Cantidad == 1)
            {
                var numeroProductos = _unidadTrabajo.CarroCompras.ObtenerTodos(u => u.UsuarioAplicacionId == carroCompras.UsuarioAplicacionId).ToList().Count();

                _unidadTrabajo.CarroCompras.Remover(carroCompras);
                _unidadTrabajo.Guardar();
                HttpContext.Session.SetInt32(DS.ssCarroCompras, numeroProductos - 1);
            }
            else
            {
                carroCompras.Cantidad -= 1;
                _unidadTrabajo.Guardar();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remover(int carroId)
        {
            var carroCompras = _unidadTrabajo.CarroCompras.ObtenerPrimero(c => c.Id == carroId, incluirPropiedades: "Producto");

            var numeroProductos = _unidadTrabajo.CarroCompras.ObtenerTodos(u => u.UsuarioAplicacionId == carroCompras.UsuarioAplicacionId).ToList().Count();

            _unidadTrabajo.CarroCompras.Remover(carroCompras);
            _unidadTrabajo.Guardar();
            HttpContext.Session.SetInt32(DS.ssCarroCompras, numeroProductos - 1);


            return RedirectToAction(nameof(Index));
        }

        public IActionResult Proceder()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            CarroComprasVM = new CarroComprasVM()
            {
                Orden = new Modelos.Orden(),
                CarroComprasLista = _unidadTrabajo.CarroCompras.ObtenerTodos(u => u.UsuarioAplicacionId == claim.Value, incluirPropiedades: "Producto")
            };

            CarroComprasVM.Orden.TotalOrden = 0;
            CarroComprasVM.Orden.UsuarioAplicacion = _unidadTrabajo.UsuarioAplicacion.ObtenerPrimero(u => u.Id == claim.Value);

            foreach (var lista in CarroComprasVM.CarroComprasLista)
            {
                lista.Precio = lista.Producto.Precio;
                CarroComprasVM.Orden.TotalOrden += (lista.Precio * lista.Cantidad);
            }

            CarroComprasVM.Orden.NombreCliente = CarroComprasVM.Orden.UsuarioAplicacion.Nombres + " " + CarroComprasVM.Orden.UsuarioAplicacion.Apellidos;
            CarroComprasVM.Orden.Telefono = CarroComprasVM.Orden.UsuarioAplicacion.PhoneNumber;
            CarroComprasVM.Orden.Direccion = CarroComprasVM.Orden.UsuarioAplicacion.Direccion;
            CarroComprasVM.Orden.Pais = CarroComprasVM.Orden.UsuarioAplicacion.Pais;
            CarroComprasVM.Orden.Ciudad = CarroComprasVM.Orden.UsuarioAplicacion.Ciudad;

            return View(CarroComprasVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Proceder")]
        public IActionResult ProcederPost(string stripeToken)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            CarroComprasVM.Orden.UsuarioAplicacion = _unidadTrabajo.UsuarioAplicacion.ObtenerPrimero(c => c.Id == claim.Value);
            CarroComprasVM.CarroComprasLista = _unidadTrabajo.CarroCompras.ObtenerTodos(c => c.UsuarioAplicacionId == claim.Value, incluirPropiedades: "Producto");
            CarroComprasVM.Orden.EstdoOrden = DS.EstadoPendiente;
           // CarroComprasVM.Orden.TransacionId = charge.id;
            CarroComprasVM.Orden.EstdoOrden = DS.PagoEstadoPendiente;
            CarroComprasVM.Orden.UsuarioAplicacionId = claim.Value;
            CarroComprasVM.Orden.FechaOrden = DateTime.Now;
            CarroComprasVM.Compania = _unidadTrabajo.Compania.ObtenerPrimero();

            _unidadTrabajo.Orden.Ageregar(CarroComprasVM.Orden);
            _unidadTrabajo.Guardar();

            foreach (var item in CarroComprasVM.CarroComprasLista)
            {
                OrdenDetalle ordenDetalle = new OrdenDetalle()
                {
                    ProductoId = item.ProductoId,
                    OrdenId = CarroComprasVM.Orden.Id,
                    Precio = item.Producto.Precio,
                    Cantidad = item.Cantidad
                };
                CarroComprasVM.Orden.TotalOrden += ordenDetalle.Cantidad * ordenDetalle.Precio;
                _unidadTrabajo.OrdenDetalle.Ageregar(ordenDetalle);
            }

            // Eliminar productos del carro de comprasa
            _unidadTrabajo.CarroCompras.RemoverRango(CarroComprasVM.CarroComprasLista);
            _unidadTrabajo.Guardar();
            HttpContext.Session.SetInt32(DS.ssCarroCompras, 0);

            if (stripeToken == null)
            {

            }
            else
            {
                //procesar pago
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(CarroComprasVM.Orden.TotalOrden * 100),
                    Currency = "eur",
                    Description = "Numero de Orden: " + CarroComprasVM.Orden.Id,
                    Source = stripeToken
                };
                var service = new ChargeService();
                Charge charge = service.Create(options);
                if (charge.BalanceTransactionId == null)
                {
                    CarroComprasVM.Orden.EstadoPago = DS.PagoEstadoRechazado;
                }
                else
                {
                    CarroComprasVM.Orden.TransacionId = charge.Id;
                }

                if(charge.Status.ToLower() == "succeeded")
                {
                    CarroComprasVM.Orden.EstadoPago = DS.PagoEstadoAprobado;
                    CarroComprasVM.Orden.EstdoOrden = DS.EstadoAprobado;
                    CarroComprasVM.Orden.FechaPago = DateTime.Now;

                    // actualiza el stock del inventario
                    foreach(var item in CarroComprasVM.CarroComprasLista)
                    {
                        var producto = _db.BodegaProducto.FirstOrDefault(b => b.ProductoId == item.ProductoId && b.BodegaId == CarroComprasVM.Compania.BodegaVentaId);

                        producto.Cantidad -= item.Cantidad;
                    }
                }
            }

            _unidadTrabajo.Guardar();
            return RedirectToAction("OrdenConfirmacion", "Carro", new { id = CarroComprasVM.Orden.Id });

        }

        public IActionResult OrdenConfirmacion(int id)
        {
            return View(id);
        }

        public IActionResult ImprimirOrden(int id)
        {
            CarroComprasVM = new CarroComprasVM();
            CarroComprasVM.Compania = _unidadTrabajo.Compania.ObtenerPrimero();
            CarroComprasVM.Orden = _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == id, incluirPropiedades: "UsuarioAplicacion");
            CarroComprasVM.OrdenDetalleLista = _unidadTrabajo.OrdenDetalle.ObtenerTodos(d => d.OrdenId == id, incluirPropiedades: "Producto");

            return new ViewAsPdf("ImprimirOrden", CarroComprasVM)
            {
                FileName = "Orden#" + CarroComprasVM.Orden.Id + ".pdf",
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 12"
            };  
        }
    }
}
