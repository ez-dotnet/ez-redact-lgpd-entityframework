using EZ.Redact.Lgpd.EntityFramework.Sample.Models;
using Microsoft.EntityFrameworkCore;

namespace EZ.Redact.Lgpd.EntityFramework.Sample.Data;

public class PublicDbContext : DbContext
{
    public PublicDbContext(DbContextOptions<PublicDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>().HasData(
            new Cliente
            {
                Id = 100,
                Nome = "Carlos Mendes",
                Cpf = "111.222.333-44",
                Email = "carlos.mendes@email.com",
                Telefone = "(31) 9 6666-2222",
                Endereco = "Praça da Liberdade, 200 - Belo Horizonte/MG",
                Observacao = "Dado público - sem proteção LGPD neste contexto"
            });
    }
}
