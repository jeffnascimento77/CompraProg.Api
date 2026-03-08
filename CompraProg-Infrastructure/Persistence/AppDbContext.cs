using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProg.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;


namespace CompraProg.Infrastructure.Persistence;
// DbContext = configuracao do EF Core para conversar com o Banco.
// Aqui registramos asa tabelas (DbSet) e constraints (ex: CPF Unico).
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ClienteEntity> Clientes => Set<ClienteEntity>();

    public DbSet<CestaEntity> Cestas => Set<CestaEntity>();
    public DbSet<ItemCestaEntity> ItensCesta => Set<ItemCestaEntity>();

    public DbSet<CustodiaEntity> Custodias => Set<CustodiaEntity>();

    public DbSet<PosicaoEntity> Posicoes => Set<PosicaoEntity>();

    public DbSet<MovimentoEntity> Movimentos => Set<MovimentoEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //CPF Unico (RN-002)
        modelBuilder.Entity<ClienteEntity>()
            .HasIndex(x => x.CPF)
            .IsUnique();

        //Relacao Cesta 1:N Itens
        modelBuilder.Entity<CestaEntity>()
            .HasMany(x => x.Itens)
            .WithOne(x => x.Cesta!)
            .HasForeignKey(x => x.CestaId);

        //Index para facilitar pegar cesta ativa
        modelBuilder.Entity<CestaEntity>()
            .HasIndex(x => x.Ativa);

        
        base.OnModelCreating(modelBuilder);

        //Indice Unico para evitar duplicar o mesmo ticker/data
        modelBuilder.Entity<CotacaoEntity>()
            .HasIndex(x => new { x.Ticker, x.DataPregao })
            .IsUnique();

        modelBuilder.Entity<CustodiaEntity>()
            .HasMany(x => x.Posicoes)
            .WithOne(x => x.Custodia)
            .HasForeignKey(x => x.CustodiaId);
    }

    public DbSet<CotacaoEntity> Cotacoes => Set<CotacaoEntity>();
}