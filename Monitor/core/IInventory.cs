using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zappy
{
    public interface IInventory
    {
        public Inventory Inventory { get; }

        /// <summary>
        /// Encabezado mostrado en el InventoryPanel para identificar qué se está viendo
        /// (p. ej. "Casilla (x, y)" o "Jugador #id — equipo — Nv.L").
        /// </summary>
        public string DisplayTitle { get; }
    }
}
