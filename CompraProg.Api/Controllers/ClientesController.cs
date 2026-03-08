using CompraProg.Infrastructure.Persistence;
using CompraProg.Infrastructure.Persistence.Entities;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompraProg.Api.Controllers;

[ApiController]
[Route("api/clientes")]

public class ClientesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(AppDbContext db, ILogger<ClientesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /* Contrato Base: POST /api/clientes/adesao
       Regras Aplicadas:
       RN-001 campos obrigatorios (validacao simples)
       RN-002 CPF Unico
       RN-003 valor mensal minimo (sugrstao 100)
       RN 005 ativo = true
       RN-006 data adesao registrada
    */

    [HttpPost("adesao")]
    public async Task<IActionResult> Adesao([FromBody] AdesaoRequest request)
    {
        //Validacao simples de campos obrigatorios
        if (string.IsNullOrWhiteSpace(request.Nome) ||
            string.IsNullOrWhiteSpace(request.CPF) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { codigo = "DADOS_INVALIDOS", erro = "Nome, CPF e Email sao obrigatorios." });
        }

        //RN-003: Valor minimo sugerido 100
        if (request.ValorMensal < 100)
        {
            return BadRequest(new { codigo = "VALOR_MENSAL_INVALIDO", erro = "O valor mensal minimo e 100." });
        }

        //RN-002 CPF Unico
        var cpfJaExiste = await _db.Clientes.AnyAsync(x => x.CPF == request.CPF);
        if (cpfJaExiste)
        {
            return BadRequest(new { codigo = "CLIENTE_CPF_DUPLICADO", erro = "CPF ja cadastrado no sistema" });
        }

        var cliente = new ClienteEntity
        {
            Nome = request.Nome.Trim(),
            CPF = request.CPF.Trim(),
            Email = request.Email.Trim(),
            ValorMensal = request.ValorMensal,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        _logger.LogInformation("cliente_adesao_ok clienteId={ClienteId} cpf={CPF}", cliente.Id, cliente.CPF);

        var custodia = new CustodiaEntity
        {
            ClienteId = cliente.Id,
            IsMaster = false
        };

        _db.Custodias.Add(custodia);
        await _db.SaveChangesAsync();

        return Created("", new
        {
            clienteId = cliente.Id,
            nome = cliente.Nome,
            cpf = cliente.CPF,
            email = cliente.Email,
            valormensal = cliente.ValorMensal,
            ativo = cliente.Ativo,
            dataAdesao = cliente.DataAdesao
        });
    }



}

//DTO (Entrada do Endpoint), DTO fica na AP por enquanto para simplificar
public record AdesaoRequest(string Nome, string CPF, string Email, decimal ValorMensal);