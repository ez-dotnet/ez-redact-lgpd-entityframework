using EZ.Redact.Lgpd.Core.Attributes;

namespace EZ.Redact.Lgpd.EntityFramework.Sample.Models;

public class Cliente
{
    public int Id { get; set; }

    [NomeData]
    public string Nome { get; set; } = string.Empty;

    [CPFData]
    public string Cpf { get; set; } = string.Empty;

    [EmailData]
    public string Email { get; set; } = string.Empty;

    [TelefoneData]
    public string Telefone { get; set; } = string.Empty;

    [EnderecoData]
    public string Endereco { get; set; } = string.Empty;

    public string Observacao { get; set; } = string.Empty;
}
