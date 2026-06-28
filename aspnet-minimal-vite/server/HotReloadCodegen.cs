#if DEBUG
using Server.Shared.Codegen;

// Faz o codegen rodar também com hot reload ligado (dotnet watch sem rebuild).
// O runtime chama UpdateApplication após aplicar cada delta de hot reload.
// Vive no composition root porque conhece a agregação de endpoints das features.
// Docs: https://learn.microsoft.com/visualstudio/debugger/hot-reload-metadataupdatehandler
[assembly: System.Reflection.Metadata.MetadataUpdateHandlerAttribute(typeof(Server.HotReloadCodegen))]

namespace Server;

internal static class HotReloadCodegen
{
    // Limpa caches inferidos de metadata. Não temos nenhum, mas a assinatura é esperada.
    internal static void ClearCache(Type[]? updatedTypes) { }

    // Regenera client/src/api/generated.ts a cada edição aplicada por hot reload.
    internal static void UpdateApplication(Type[]? updatedTypes)
    {
        try { TsGenerator.Run(ApiEndpoints.All); }
        catch (Exception ex) { Console.WriteLine($"[codegen] falhou: {ex.Message}"); }
    }
}
#endif
