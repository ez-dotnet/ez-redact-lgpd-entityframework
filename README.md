# EZ.Redact.Lgpd.EntityFramework

[![NuGet Version](https://img.shields.io/badge/nuget-v1.0.0-blue.svg)](https://www.nuget.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0+](https://img.shields.io/badge/.NET-8.0%2B%20|%209.0%2B%20|%2010.0%2B-512bd4.svg)](https://dotnet.microsoft.com/download)

Extensão do Entity Framework Core para o [EZ.Redact.Lgpd.Core](https://github.com/ez-dotnet/ez-redact-lgpd-core). Redige dados pessoais automaticamente durante a **leitura** de entidades, sem precisar chamar `ILGPDRedactService` manualmente.

Basta decorar suas models com os atributos do `EZ.Redact.Lgpd.Core` e usar `UseRedaction()` na query — a redação acontece de forma transparente no momento da materialização.

---

## Instalação

```bash
dotnet add package EZ.Redact.Lgpd.EntityFramework
```

Registre os serviços no DI:

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.EnableRedaction(options => options.ApplyDiscriminator = false);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddLGPDRedaction()
    .AddEntityFrameworkRedaction(options =>
    {
        options.UseDbContext<AppDbContext>();
    });
```

> `AddEntityFrameworkRedaction()` registra o `LgpdRedactionInterceptor` e o configura automaticamente nos DbContexts especificados.

## Configuração

### `LGPDRedactOptions`

| Propriedade | Padrão | Descrição |
| :--- | :--- | :--- |
| `MaskChar` | `'*'` | Caractere usado no mascaramento |
| `Guid` | `new()` | Opções de redação de GUID (ver abaixo) |
| `HmacKey` | `null` | Chave HMAC em Base64 (obrigatória se `HmacFor` não estiver vazio) |
| `HmacKeyId` | `1` | Identificador da chave para rotação |
| `HmacFor` | `HashSet<>` vazio | Tipos de dado que devem usar HMAC em vez de masking |

### `GuidOptions`

| Propriedade | Padrão | Descrição |
| :--- | :--- | :--- |
| `PrefixHexCount` | `4` | Quantidade de hex digits preservados no prefixo |
| `SuffixHexCount` | `4` | Quantidade de hex digits preservados no sufixo |

### Três formas de configurar

**1. Em código (`Action<LGPDRedactOptions>`)**
```csharp
builder.Services.AddLGPDRedaction(options =>
{
    options.MaskChar = '#';
    options.Guid.PrefixHexCount = 6;
    options.HmacKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    options.HmacFor.Add(DadoPessoal.CPF);
});
```

**2. Via `IConfiguration` (appsettings.json + env vars)**
```csharp
builder.Services.AddLGPDRedaction(builder.Configuration);
```

```json
{
  "LGPD": {
    "MaskChar": "#",
    "Guid": { "PrefixHexCount": 6 },
    "HmacFor": ["CPF"],
    "HmacKeyId": 1
  }
}
```

A `HmacKey` **não deve** ficar no `appsettings.json`. Use variável de ambiente ou User Secrets:

```bash
export LGPD__HmacKey="suachavebase64aqui=="
```

**3. Combinando ambas**
```csharp
builder.Services.AddLGPDRedaction(options =>
{
    options.HmacKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
});
builder.Services.PostConfigure<LGPDRedactOptions>(opts =>
{
    opts.HmacFor.Add(DadoPessoal.CPF);
});
```

---

## Uso

Decore suas entidades com os atributos do `EZ.Redact.Lgpd.Core`:

```csharp
using EZ.Redact.Lgpd.Core.Attributes;

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
```

### Consulta com redação

Use `UseRedaction()` na query para ativar a redação:

```csharp
using EZ.Redact.Lgpd.EntityFramework;

var clientes = await _db.Clientes
    .UseRedaction()
    .Where(c => c.Ativo)
    .ToListAsync();
```

### Consulta sem redação

Sem `UseRedaction()`, os dados retornam sem alteração:

```csharp
var clientes = await _db.Clientes
    .Where(c => c.Ativo)
    .ToListAsync();
```

### Saída redigida

| Campo | Original | Redigido |
| :--- | :--- | :--- |
| Nome | `Felipe Siqueira` | `F***** S*******` |
| Cpf | `123.456.789-09` | `123.***.***-09` |
| Email | `felipe.siqueira@email.com` | `f**************@email.com` |
| Telefone | `(11) 9 8888-4444` | `(11) 9 ****-4444` |
| Endereco | `Avenida Paulista, 1000` | `A****** P*******, ****` |

---

## Como funciona

1. `UseRedaction()` adiciona `TagWith("lgpd-redact")` na query, que vira um comentário no SQL gerado
2. O `LgpdRedactionInterceptor` implementa `IDbCommandInterceptor` e `IMaterializationInterceptor`
3. Antes da execução, o interceptor verifica se o comando SQL contém a tag `-- lgpd-redact`
4. Durante a materialização de cada entidade, as propriedades marcadas com atributos LGPD são inspecionadas via delegates compilados (performance em memória)
5. Para cada propriedade com valor `string`, o `ILGPDRedactService.Redact()` é chamado e o valor é substituído

> A redação só ocorre em operações de **leitura** (SELECT). Operações de INSERT, UPDATE e DELETE nunca são afetadas.

---

## Atributos Suportados

Os atributos são definidos pelo pacote [EZ.Redact.Lgpd.Core](https://github.com/ez-dotnet/ez-redact-lgpd-core) e funcionam com qualquer entidade do EF Core.

### Identificação Pessoal

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[NomeData]` | Mantém apenas as iniciais de cada palavra | `Maria da Silva` | `M**** d* S****` |
| `[CPFData]` | Preserva 3 primeiros e 2 últimos dígitos | `123.456.789-01` | `123.***.***-01` |
| `[CNPJData]` | Preserva raiz (2 caracteres) e radical (6 últimos) | `12.345.678/0001-90` | `12.***.***/0001-90` |
| `[EmailData]` | Preserva inicial e domínio | `felipe.siqueira@gmail.com` | `f**************@gmail.com` |
| `[TelefoneData]` | Preserva DDD, 1 dígito após DDD e 4 últimos | `(11) 98888-4444` | `(11) 9****-4444` |
| `[EnderecoData]` | Mantém apenas as iniciais, oculta números | `Avenida Paulista, 1000` | `A****** P*******, ****` |
| `[DataGenericaData]` | Preserva ano, mascara dia/mês | `15/03/1990` | `**/**/1990` |

### Documentos Oficiais

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[CNHData]` | Preserva 3 primeiros e 2 últimos dígitos | `12345678901` | `123******01` |
| `[TituloEleitorData]` | Preserva 4 primeiros e 4 últimos dígitos | `1234.5678.9012` | `1234.****.9012` |
| `[PISData]` | Preserva 3 primeiros e dígito verificador | `123.45678.90-1` | `123.*****.**-1` |
| `[CNSData]` | Preserva 3 primeiros e 4 últimos | `123 4567 8901 2345` | `123 **** **** 2345` |
| `[CTPSData]` | Preserva 3 primeiros e 3 últimos | `1234567890` | `123****890` |
| `[CertidaoData]` | Preserva 6 primeiros e 2 verificadores | `123456.78.1234.5.6.7890.1.12345-67` | `123456.**.****.*.*.****.*.*****-67` |
| `[PassaporteData]` | Preserva prefixo letras e 2 últimos dígitos | `AB123456` | `AB****56` |
| `[RNEData]` | Preserva letra prefixo e dígito verificador | `V1234567-8` | `V*******-8` |

### Financeiro

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[CartaoCreditoData]` | Preserva 4 primeiros e 4 últimos dígitos | `4532 1178 9012 3456` | `4532 **** **** 3456` |
| `[ContaBancariaData]` | Preserva operação e dígito, mascara conta | `013.123456-7` | `013.******-7` |
| `[PixData]` | Mascara chave aleatória mantendo 4 primeiros e 8 últimos | `e8d26618-2e11-4b22-8d26-66182e114b22` | `e8d2****-****-****-****-****2e114b22` |

### Redes e Localização

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[EnderecoIPData]` | Mascara os 2 últimos octetos (IPv4) e os últimos 3 grupos (IPv6) | `192.168.1.100` | `192.168.*.***` |
| `[MacAddressData]` | Preserva prefixo OUI (3 primeiros bytes) | `00:1A:2B:3C:4D:5E` | `00:1A:2B:**:**:**` |
| `[CEPData]` | Mascara os 3 últimos dígitos | `01310-900` | `01310-***` |
| `[GeolocalizacaoData]` | Mascara parte decimal de latitude e longitude | `-23.5505, -46.6333` | `-23.****, -46.****` |

### Veículo

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[PlacaData]` | Mascara números (padrão antigo) e caracteres após prefixo (Mercosul) | `ABC-1234` | `ABC-****` |
| `[RenavamData]` | Preserva 3 primeiros e 3 últimos dígitos | `12345678901` | `123*****901` |

### Técnico

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[GuidData]` | Mascara GUID mantendo 4 primeiros e 4 últimos hex dígitos | `e8d26618-2e11-4b22-8d26-66182e114b22` | `e8d2****-****-****-****-*******4b22` |

---

## Samples

Um projeto de exemplo na pasta `samples/`:

| Projeto | Descrição |
| :--- | :--- |
| [`EZ.Redact.Lgpd.EntityFramework.Sample`](samples/EZ.Redact.Lgpd.EntityFramework.Sample) | Minimal API com InMemory Database e endpoints `/clientes/redacted`, `/clientes/raw` e `/publico/clientes` |

```bash
dotnet run --project samples/EZ.Redact.Lgpd.EntityFramework.Sample
curl http://localhost:5000/clientes/redacted
curl http://localhost:5000/clientes/raw
```

---

## Licença

Distribuído sob a licença MIT.
