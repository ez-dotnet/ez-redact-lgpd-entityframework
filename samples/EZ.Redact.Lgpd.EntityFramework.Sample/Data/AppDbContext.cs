using EZ.Redact.Lgpd.EntityFramework.Sample.Models;
using Microsoft.EntityFrameworkCore;

namespace EZ.Redact.Lgpd.EntityFramework.Sample.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>().HasData(
            new Cliente
            {
                Id = 1,
                Nome = "Felipe Siqueira",
                Cpf = "123.456.789-09",
                Email = "felipe.siqueira@email.com",
                Telefone = "(11) 9 8888-4444",
                Endereco = "Avenida Paulista, 1000 - São Paulo/SP",
                Observacao = "Cliente premium desde 2020"
            },
            new Cliente
            {
                Id = 2,
                Nome = "Ana Beatriz Oliveira",
                Cpf = "987.654.321-00",
                Email = "ana.oliveira@email.com",
                Telefone = "(21) 9 7777-3333",
                Endereco = "Rua Atlântica, 500 - Rio de Janeiro/RJ",
                Observacao = "Indicado por Felipe"
            });
    }
}
