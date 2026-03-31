using SistemaPOS.Data;
using SistemaPOS.Models;
using SistemaPOS.Models.DTOs.Inventario;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaPOS.Services
{
    public class InventarioService
    {
        private readonly AppDbContext _db;

        public InventarioService(AppDbContext db)
        {
            _db = db;
        }

        public Producto ObtenerPorCodigo(string codigo)
        {
            return _db.Productos.FirstOrDefault(p => p.Codigo == codigo);
        }

        public InventarioItemDTO CrearDesdeProducto(Producto p)
        {
            return new InventarioItemDTO
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                PrecioCompra = p.PrecioCompra,
                PrecioVenta = p.PrecioVenta,
                StockActual = p.Cantidad,
                CategoriaId = p.CategoriaId,
                ProveedorId = p.ProveedorId,
                TipoVentaId = p.TipoVentaId,
                Ajuste = 0,
                EsNuevo = false
            };
        }

        // 🔹 Query para EF (uso en DB, no rompe performance)
        public IQueryable<InventarioItemDTO> QueryInventario()
        {
            return _db.Productos.Select(p => new InventarioItemDTO
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                PrecioCompra = p.PrecioCompra,
                PrecioVenta = p.PrecioVenta,
                StockActual = p.Cantidad,
                CategoriaId = p.CategoriaId,
                ProveedorId = p.ProveedorId,
                TipoVentaId = p.TipoVentaId,
                Ajuste = 0,
                EsNuevo = false
            });
        }

        // FUNCION PARA CREAR UN NUEVO PRODUCTO CON DTO EN MEMORIA (NO SE GUARDA EN DB HASTA QUE SE LLAME A UN MÉTODO DE GUARDADO)
        public InventarioItemDTO CrearNuevoProducto(string codigo, int categoriaId, int proveedorId)
        {
            return new InventarioItemDTO
            {
                Codigo = codigo,
                Nombre = "NUEVO",
                Ajuste = 0,
                StockActual = 0,
                EsNuevo = true,
                TipoVentaId = 1,
                CategoriaId = categoriaId,
                ProveedorId = proveedorId
            };
        }

        public void GuardarItems(List<InventarioItemDTO> items)
        {
            foreach (var item in items)
            {
                var producto = _db.Productos.FirstOrDefault(p => p.Codigo == item.Codigo);

                if (producto != null)
                {
                    // 🔹 ACTUALIZAR
                    producto.Nombre = item.Nombre.Trim().ToUpper();
                    producto.PrecioCompra = item.PrecioCompra;
                    producto.PrecioVenta = item.PrecioVenta;
                    producto.Cantidad = item.Total;
                    producto.CategoriaId = item.CategoriaId;
                    producto.ProveedorId = item.ProveedorId;
                    producto.TipoVentaId = item.TipoVentaId;
                    producto.Activo = item.Activo;
                }
                else if (item.EsNuevo)
                {
                    // 🔹 INSERTAR
                    var nuevoProd = new Producto
                    {
                        Codigo = item.Codigo,
                        Nombre = item.Nombre.Trim().ToUpper(),
                        PrecioCompra = item.PrecioCompra,
                        PrecioVenta = item.PrecioVenta,
                        Cantidad = item.Total,
                        CategoriaId = item.CategoriaId,
                        ProveedorId = item.ProveedorId,
                        TipoVentaId = item.TipoVentaId,
                        Activo = true
                    };

                    _db.Productos.Add(nuevoProd);
                }
            }

            _db.SaveChanges();
        }
    }
}
