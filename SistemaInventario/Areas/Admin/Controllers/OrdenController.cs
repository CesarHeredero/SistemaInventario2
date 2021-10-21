using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

namespace SistemaInventario.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrdenController : Controller
    {
        private readonly IUnidadTrabajo _unidadTrabajo;

        [BindProperty]
        public OrdenDetalleVM OrdenDetalleVM { get; set; }

        public OrdenController(IUnidadTrabajo unidadTrabajo)
        {
            _unidadTrabajo = unidadTrabajo;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Detalle(int id)
        {
            OrdenDetalleVM = new OrdenDetalleVM()
            {
                Orden = _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == id, incluirPropiedades: "UsuarioAplicacion"),
                OrdenDetalleLista = _unidadTrabajo.OrdenDetalle.ObtenerTodos(o => o.OrdenId == id, incluirPropiedades:"Producto")
            };

            return View(OrdenDetalleVM);
        }

        [Authorize(Roles = DS.Role_Admin + "," + DS.Role_Ventas)]
        public IActionResult Procesar(int id)
        {
            Orden orden = _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == id);
            orden.EstdoOrden = DS.EstadoEnProceso;
            _unidadTrabajo.Guardar();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = DS.Role_Admin + "," + DS.Role_Ventas)]
        public IActionResult EnviarOrden()
        {
            Orden orden = _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == OrdenDetalleVM.Orden.Id);
            orden.NumeroEnvio = OrdenDetalleVM.Orden.NumeroEnvio;
            orden.Carrier = OrdenDetalleVM.Orden.Carrier;
            orden.EstdoOrden = DS.EstadoEnviado;
            orden.FechaEnvio = DateTime.Now;
            _unidadTrabajo.Guardar();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = DS.Role_Admin + "," + DS.Role_Ventas)]
        public IActionResult CancelarOrden(int id)
        {
            Orden orden = _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == id);

            if(orden.EstadoPago == DS.EstadoAprobado)
            {
                var options = new RefundCreateOptions
                {
                    Amount = Convert.ToInt32(orden.TotalOrden * 100),
                    Reason = RefundReasons.RequestedByCustomer,
                    Charge = orden.TransacionId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);
                orden.EstdoOrden = DS.EstadoDevuelto;
                orden.EstadoPago = DS.EstadoDevuelto;
            }
            else
            {
                orden.EstdoOrden = DS.EstadoCancelado;
                orden.EstadoPago = DS.EstadoCancelado;
            }
            _unidadTrabajo.Guardar();
            return RedirectToAction("Index");
        }

        #region
        [HttpGet]
        public IActionResult ObtenerOrdenLista(string estado)
        {
            var claimIdentidad = (ClaimsIdentity)User.Identity;
            var claim = claimIdentidad.FindFirst(ClaimTypes.NameIdentifier);
            IEnumerable<Orden> ordenLista;
            if (User.IsInRole(DS.Role_Admin) || User.IsInRole(DS.Role_Ventas))
            {
                ordenLista = _unidadTrabajo.Orden.ObtenerTodos(incluirPropiedades: "UsuarioAplicacion");
            }
            else
            {
                ordenLista = _unidadTrabajo.Orden.ObtenerTodos(o => o.UsuarioAplicacionId == claim.Value, incluirPropiedades: "UsuarioAplicacion");
            }

            switch (estado)
            {
                case "pendiente":
                    ordenLista = ordenLista.Where(o => o.EstadoPago == DS.PagoEstadoPendiente || o.EstadoPago == DS.PagoEstadoRetrasado);
                    break;
                case "aprobado":
                    ordenLista = ordenLista.Where(o => o.EstadoPago == DS.PagoEstadoAprobado);
                    break;
                case "rechazado":
                    ordenLista = ordenLista.Where(o => o.EstadoPago == DS.PagoEstadoRechazado || o.EstadoPago == DS.EstadoCancelado);
                    break;
                case "completado":
                    ordenLista = ordenLista.Where(o => o.EstadoPago == DS.EstadoEnviado);
                    break;
                default:
                    break;
            }


            return Json(new { data = ordenLista });
        }
        #endregion
    }
}
