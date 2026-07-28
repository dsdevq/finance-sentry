namespace FinanceSentry.Modules.Rag;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Rag.Domain;
using FinanceSentry.Modules.Rag.Infrastructure.Embeddings;
using FinanceSentry.Modules.Rag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class RagModule
{
    internal sealed class ModuleRegistrar : IModuleRegistrar
    {
        public void Register(IServiceCollection services, IConfiguration config)
            => services.AddRagModule(config);
    }

    public static IServiceCollection AddRagModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<RagDbContext>(
            o => o.UseNpgsql(
                config.GetConnectionString("Default")!,
                b => b.MigrationsHistoryTable("__ef_migrations_history_rag", "public")));

        services.AddScoped<ICorpusRepository, EfCorpusRepository>();

        // Stub is the active embedding client until a real model (BGE-M3 via Ollama) is wired in.
        services.AddSingleton<IEmbeddingClient, DeterministicStubEmbeddingClient>();

        return services;
    }
}
