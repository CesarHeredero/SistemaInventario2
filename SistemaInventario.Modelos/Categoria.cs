using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaInventario.Modelos
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        [Display(Name = "Nombre Categoría")]
        public string Nombre { get; set; }
        [Required]
        public bool Estado { get; set; }
    }
}
