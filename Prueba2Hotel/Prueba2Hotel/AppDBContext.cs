
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Prueba2Hotel
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        public DbSet<Reserva> Reserva { get; set; }
        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Habitacion> Habitacion { get; set; }
        public DbSet<ServiciosAdicionales> ServiciosAdicionales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Cedula).IsRequired().HasMaxLength(10);
                entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Apellido).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Correo).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Telefono).IsRequired().HasMaxLength(10);
                entity.Property(c => c.Direccion).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Habitacion>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.NumHabitacion).IsRequired().HasMaxLength(4).IsUnicode();
                entity.Property(h => h.NumMaximoPersonas).IsRequired();
                entity.Property(h => h.Descripcion).IsRequired().HasMaxLength(255);
                entity.Property(h => h.Tipo).IsRequired().HasMaxLength(100);
                entity.Property(h => h.Estado).HasMaxLength(100);
            });

            modelBuilder.Entity<Reserva>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Entrada).IsRequired();
                entity.Property(r => r.Salida).IsRequired();
                entity.Property(r => r.Precio).IsRequired().HasColumnType("decimal(10, 2)");
                entity.Property(r => r.CedulaCliente).IsRequired().HasMaxLength(10);
                entity.Property(r => r.NumHabitacion).IsRequired().HasMaxLength(4);

                entity.HasOne(r => r.Cliente)
                      .WithMany(c => c.Reservas)
                      .HasForeignKey(r => r.ClienteId);

                entity.HasOne(r => r.Habitacion)
                      .WithMany(h => h.Reservas)
                      .HasForeignKey(r => r.HabitacionId);
            });

            modelBuilder.Entity<ServiciosAdicionales>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Descripcion).IsRequired().HasMaxLength(255);
                entity.Property(s => s.Costo).IsRequired().HasColumnType("decimal(10, 2)");

                entity.HasOne(s => s.Reserva)
                      .WithMany(r => r.ServiciosAdicionales)
                      .HasForeignKey(s => s.ReservaId);
            });
        }
    }

    public class Cliente
    {
        [JsonIgnore]
        public int Id { get; set; }
        public required string Cedula { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public required string Correo { get; set; }
        public required string Telefono { get; set; }
        public required string Direccion {  get; set; }
        [JsonIgnore]
        public List<Reserva>? Reservas { get; set; }
    }

    public class Habitacion
    {
        [JsonIgnore]
        public int Id { get; set; }
        public required string Tipo { get; set; }
        public required string NumHabitacion { get; set; }
        public required int NumMaximoPersonas { get; set; }
        public required string Descripcion { get; set; }

        [JsonIgnore]
        public string? Estado { get; set; }

        [JsonIgnore]
        public List<Reserva>? Reservas { get; set; }
    }

    public class Reserva
    {
        [JsonIgnore]
        public int? Id { get; set; }
        [JsonIgnore]
        public int? ClienteId { get; set; }
        [JsonIgnore]
        public int? HabitacionId { get; set; }
        public required DateTime? Entrada { get; set; }
        public required DateTime? Salida { get; set; }
        public required decimal? Precio { get; set; }
        public required string? CedulaCliente { get; set; }
        public required string? NumHabitacion { get; set; }

        [JsonIgnore]
        public Cliente? Cliente { get; set; }
        [JsonIgnore]
        public Habitacion? Habitacion { get; set; }
        [JsonIgnore]
        public List<ServiciosAdicionales>? ServiciosAdicionales { get; set; }
    }

    public class ServiciosAdicionales
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public required int? ReservaId { get; set; }
        public required string? Descripcion { get; set; }
        public required decimal? Costo { get; set; }
        [JsonIgnore]
        public Reserva? Reserva { get; set; }
    }
}