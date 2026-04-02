using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
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
                GananciaUnitaria = p.GananciaUnitaria ?? 0,
                PorcentajeGanancia = p.PorcentajeGanancia ?? 0,
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
                GananciaUnitaria = p.GananciaUnitaria ?? 0,
                PorcentajeGanancia = p.PorcentajeGanancia ?? 0,
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
                GananciaUnitaria = 0,
                PorcentajeGanancia = 0,
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
                  //  producto.Cantidad = item.Total;
                    producto.CategoriaId = item.CategoriaId;
                    producto.ProveedorId = item.ProveedorId;
                    producto.TipoVentaId = item.TipoVentaId;
                    producto.Activo = item.Activo;

                    CalcularGanancia(producto);

                    producto.Cantidad = item.Total;
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

                    CalcularGanancia(nuevoProd);

                    _db.Productos.Add(nuevoProd);

                }
            }

            _db.SaveChanges();
        }


        public void ExportarAExcel(string rutaArchivo)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Productos");
                var datos = _db.Productos.ToList();

                string[] headers = { "Id", "Codigo", "Nombre", "PrecioCompra", "PrecioVenta", "Cantidad", "CantidadMinima", "TipoVentaId", "CategoriaId", "ProveedorId", "Activo" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                    ws.Cell(1, i + 1).Style.Font.Bold = true;
                }

                int f = 2;
                foreach (var p in datos)
                {
                    ws.Cell(f, 1).Value = p.Id;
                    ws.Cell(f, 2).Value = p.Codigo;
                    ws.Cell(f, 3).Value = p.Nombre;
                    ws.Cell(f, 4).Value = p.PrecioCompra;
                    ws.Cell(f, 5).Value = p.PrecioVenta;
                    ws.Cell(f, 6).Value = p.Cantidad;
                    ws.Cell(f, 7).Value = p.CantidadMinima;
                    ws.Cell(f, 8).Value = p.TipoVentaId;
                    ws.Cell(f, 9).Value = p.CategoriaId;
                    ws.Cell(f, 10).Value = p.ProveedorId;
                    ws.Cell(f, 11).Value = p.Activo ? 1 : 0;
                    f++;
                }
                ws.Columns().AdjustToContents();
                workbook.SaveAs(rutaArchivo);
            }
        }

        public void ImportarDesdeExcel(string rutaArchivo)
        {
            using (var trans = _db.Database.BeginTransaction())
            {
                try
                {
                    using (var wb = new XLWorkbook(rutaArchivo))
                    {
                        var ws = wb.Worksheet(1);
                        var filas = ws.RangeUsed().RowsUsed().Skip(1);

                        foreach (var f in filas)
                        {
                            string cod = LimpiarTexto(f.Cell(2).GetValue<string>());
                            if (string.IsNullOrEmpty(cod)) continue;

                            string nombre = LimpiarTexto(f.Cell(3).GetValue<string>());

                            var p = _db.Productos.FirstOrDefault(x => x.Codigo == cod);

                            if (p == null)
                            {
                                _db.Productos.Add(new Producto
                                {
                                    Codigo = cod,
                                    Nombre = nombre,
                                    PrecioCompra = LimpiarYConvertirDecimal(f.Cell(4).GetValue<string>()),
                                    PrecioVenta = LimpiarYConvertirDecimal(f.Cell(5).GetValue<string>()),
                                    Cantidad = f.Cell(6).GetValue<int>(),
                                    CantidadMinima = f.Cell(7).GetValue<int>(),
                                    TipoVentaId = f.Cell(8).GetValue<int>() == 0 ? 1 : f.Cell(8).GetValue<int>(),
                                    CategoriaId = f.Cell(9).GetValue<int>() == 0 ? 1 : f.Cell(9).GetValue<int>(),
                                    ProveedorId = f.Cell(10).GetValue<int>() == 0 ? 1 : f.Cell(10).GetValue<int>(),
                                    Activo = f.Cell(11).GetValue<int>() == 1
                                });
                            }
                            else
                            {
                                p.Nombre = nombre;
                                p.PrecioCompra = LimpiarYConvertirDecimal(f.Cell(4).GetValue<string>());
                                p.PrecioVenta = LimpiarYConvertirDecimal(f.Cell(5).GetValue<string>());
                                p.Cantidad = f.Cell(6).GetValue<int>();
                                p.Activo = f.Cell(11).GetValue<int>() == 1;
                            }
                        }

                        _db.SaveChanges();
                        trans.Commit();
                    }
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        private decimal LimpiarYConvertirDecimal(string valorRaw)
        {
            if (string.IsNullOrEmpty(valorRaw)) return 0;
            string limpio = valorRaw
                .Replace(" ", "")
                .Replace("\u00A0", "")
                .Replace("₡", "")
                .Trim();

            decimal.TryParse(limpio, out decimal resultado);
            return resultado;
        }

        public string EliminarMarcados(List<InventarioItemDTO> lista)
        {
            var marcados = lista.Where(x => x.IsSelected).ToList();

            int eliminados = 0;
            int desactivados = 0;

            foreach (var item in marcados)
            {
                var producto = _db.Productos.FirstOrDefault(p => p.Codigo == item.Codigo);

                if (producto == null) continue;

                // 🔥 CONSULTA DIRECTA (SIN INCLUDE)
                bool tieneVentas = _db.VentaDetalles.Any(v => v.ProductoId == producto.Id);
                bool tieneCompras = _db.CompraDetalles.Any(c => c.ProductoId == producto.Id);

                try
                {
                    if (tieneVentas || tieneCompras)
                    {
                        // 🔒 Tiene historial → soft delete
                        producto.Activo = false;
                        desactivados++;
                    }
                    else
                    {
                        // 🧹 Basura → hard delete
                        _db.Productos.Remove(producto);
                        eliminados++;
                    }

                    _db.SaveChanges();
                }
                catch
                {
                    // 🛟 Fallback por FK u otros casos
                    producto.Activo = false;
                    _db.SaveChanges();
                    desactivados++;
                }
            }

            return $"Eliminados: {eliminados} | Desactivados: {desactivados}";
        }

        private string LimpiarTexto(string valor)
        {
            return valor?.Trim().ToUpper();
        }

        //CALCULA GANANCIA UNITARIA Y PORCENTAJE DE GANANCIA PARA UN PRODUCTO DADO SUS PRECIOS DE COMPRA Y VENTA
        private void CalcularGanancia(Producto p)
        {
            p.GananciaUnitaria = p.PrecioVenta - p.PrecioCompra;

            if (p.PrecioCompra > 0)
            {
                p.PorcentajeGanancia = (p.GananciaUnitaria / p.PrecioCompra) * 100;
            }
            else
            {
                p.PorcentajeGanancia = 0;
            }
        }
    }
}
