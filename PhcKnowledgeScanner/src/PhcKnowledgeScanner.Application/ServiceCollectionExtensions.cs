using Microsoft.Extensions.DependencyInjection;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Analysis;
using PhcKnowledgeScanner.Analysis.DeclaredRelations;
using PhcKnowledgeScanner.Analysis.Knowledge;
using PhcKnowledgeScanner.Application.Services;
using PhcKnowledgeScanner.Collectors.Phc;
using PhcKnowledgeScanner.Collectors.Sql;
using PhcKnowledgeScanner.Engine;

namespace PhcKnowledgeScanner.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhcKnowledgeScanner(this IServiceCollection services)
    {
        services.AddSingleton<SqlConnectionTester>();
        services.AddSingleton<ICollector, SqlServerStructureCollector>();
        services.AddSingleton<ICollector, PhcCustomizationsCollector>();
        services.AddSingleton<ICollector, PhcDeclaredRelationsCollector>();
        services.AddSingleton<ScanEngine>(sp => new ScanEngine(sp.GetServices<ICollector>()));
        services.AddSingleton<ForeignKeyRelationAnalyzer>();
        services.AddSingleton<SchemaOwnershipInferenceAnalyzer>();
        services.AddSingleton<DeclaredRelationInterpretationService>();
        services.AddSingleton<RelationalKnowledgeProjector>();
        services.AddSingleton<IWorkspaceExportService, WorkspaceExportService>();
        services.AddSingleton<IDatabaseSchemaSnapshotService, DatabaseSchemaSnapshotService>();
        services.AddSingleton<IConnectionAppService, ConnectionAppService>();
        services.AddSingleton<IScanAppService, ScanAppService>();
        services.AddSingleton<IInventoryAppService, InventoryAppService>();
        return services;
    }
}
