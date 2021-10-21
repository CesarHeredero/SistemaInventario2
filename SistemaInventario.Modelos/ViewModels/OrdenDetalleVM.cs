using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaInventario.Modelos.ViewModels
{
    public class OrdenDetalleVM
    {
        public Compania Compania { get; set; }
        public Orden Orden { get; set; }
        public IEnumerable<OrdenDetalle>  OrdenDetalleLista { get; set; }
    }
}
