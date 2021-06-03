using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaInventario.Modelos.ViewModels
{
    public class CarroComprasVM
    {
        public Compania Compania { get; set; }
        public BodegaProducto BodegaProducto { get; set; }
        public CarroCompras CarroCompras { get; set; }
    }
}
