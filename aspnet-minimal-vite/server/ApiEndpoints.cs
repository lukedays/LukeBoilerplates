using Server.Features.Todos;
using Server.Shared.Codegen;

namespace Server;

// Composition root do codegen: agrega o manifesto de cada slice.
// Adicionou uma feature? Inclua o manifesto dela aqui.
public static class ApiEndpoints
{
    public static IReadOnlyList<EndpointDef> All =>
    [
        .. TodosApi.Endpoints,
    ];
}
