using Microsoft.EntityFrameworkCore;
using SistemaPOS.Models;
using System.IO;

namespace SistemaPOS.Data
{
    //  DbContext 
    public class AppDbContext : DbContext
    {
        // =========================
        //  TABLAS (DbSet = tabla)
        // =========================

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentaDetalle> VentaDetalles { get; set; }

        public DbSet<Compra> Compras { get; set; }
        public DbSet<CompraDetalle> CompraDetalles { get; set; }

        public DbSet<Gasto> Gastos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }

        // =========================
        //  CONEXIÓN A SQLITE
        // =========================
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaPOS"
            );

            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, "pos.db");

            options.UseSqlite($"Data Source={path}");
        }

        //  CONFIGURACIÓN FLUENT API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //  PRODUCTO
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(p => p.Id); //  PK

                entity.Property(p => p.Codigo)
                    .IsRequired() // obligatorio
                    .HasMaxLength(50);

                entity.HasIndex(p => p.Codigo)
                    .IsUnique();

                entity.Property(p => p.Nombre)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(p => p.PrecioCompra)
                    .HasColumnType("decimal(10,2)");

                entity.Property(p => p.PrecioVenta)
                    .HasColumnType("decimal(10,2)");

                entity.Property(p => p.Cantidad)
                    .IsRequired();

                entity.HasOne<Categoria>()
                    .WithMany()
                    .HasForeignKey(p => p.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Proveedor>()
                    .WithMany()
                    .HasForeignKey(p => p.ProveedorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //  VENTA
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.Property(v => v.Total)
                    .HasColumnType("decimal(10,2)");

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(v => v.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //  VENTA DETALLE
            modelBuilder.Entity<VentaDetalle>(entity =>
            {
                entity.HasKey(vd => vd.Id);

                entity.Property(vd => vd.PrecioUnitario)
                    .HasColumnType("decimal(10,2)");

                entity.HasOne<Venta>()
                    .WithMany()
                    .HasForeignKey(vd => vd.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Producto>()
                    .WithMany()
                    .HasForeignKey(vd => vd.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //  COMPRA
            modelBuilder.Entity<Compra>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Total)
                    .HasColumnType("decimal(10,2)");

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(c => c.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            //  COMPRA DETALLE
            modelBuilder.Entity<CompraDetalle>(entity =>
            {
                entity.HasKey(cd => cd.Id);

                entity.Property(cd => cd.PrecioCompra)
                    .HasColumnType("decimal(10,2)");

                entity.HasOne<Compra>()
                    .WithMany()
                    .HasForeignKey(cd => cd.CompraId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Producto>()
                    .WithMany()
                    .HasForeignKey(cd => cd.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //  GASTO
            modelBuilder.Entity<Gasto>(entity =>
            {
                entity.HasKey(g => g.Id);

                entity.Property(g => g.Monto)
                    .HasColumnType("decimal(10,2)");

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(g => g.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });



            //  USUARIO
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Password)
                    .IsRequired();

                entity.Property(u => u.Rol)
                    .IsRequired()
                    .HasMaxLength(20);


                entity.Property(u => u.Activo)
                    .IsRequired()
                    .HasDefaultValue(true);
            });

            //  CATEGORIA
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            //  PROVEEDOR
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Nombre)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(p => p.Telefono)
                    .HasMaxLength(50);

                entity.Property(p => p.Email)
                    .HasMaxLength(100);
            });
        }
    }
}