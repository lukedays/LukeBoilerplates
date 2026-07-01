using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Server.Features.Codegen;

// Descrição tipada de um endpoint. Cada slice expõe a sua lista; o composition
// root agrega e passa ao gerador.
public sealed record EndpointDef(
    string Name,
    string Method,
    string Pattern,
    Type? Request,
    Type Response
);

// Gera client/src/api/generated.ts por reflection nos tipos dos endpoints.
// Sem OpenAPI: lê os tipos C# direto, então os tipos batem 100%.
public static class TsGenerator
{
    private static readonly NullabilityInfoContext NullCtx = new();

    public static void Run(IReadOnlyList<EndpointDef> endpoints)
    {
        var objects = new List<Type>(); // records/classes -> interface
        var enums = new List<Type>(); // enums -> union de string
        Collect(endpoints, objects, enums);

        var sb = new StringBuilder();
        Header(sb);
        foreach (var e in enums)
            EmitEnum(sb, e);
        foreach (var o in objects)
            EmitInterface(sb, o);
        HttpCore(sb);
        foreach (var ep in endpoints)
            EmitHook(sb, ep);

        var outPath = ResolveOutPath();
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine($"generated -> {outPath}");
    }

    // --- Coleta de tipos (BFS a partir dos endpoints) ---
    private static void Collect(
        IReadOnlyList<EndpointDef> endpoints,
        List<Type> objects,
        List<Type> enums
    )
    {
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>();
        foreach (var ep in endpoints)
        {
            if (ep.Request is not null)
                queue.Enqueue(ep.Request);
            queue.Enqueue(ep.Response);
        }

        while (queue.Count > 0)
        {
            var t = Unwrap(queue.Dequeue());
            if (t is null || !seen.Add(t))
                continue;
            if (t.IsEnum)
            {
                enums.Add(t);
                continue;
            }
            if (!IsObject(t))
                continue;

            objects.Add(t);
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                queue.Enqueue(p.PropertyType);
        }
    }

    // Remove Nullable<T>, arrays e IEnumerable<T> até o tipo "folha".
    private static Type? Unwrap(Type t)
    {
        if (t == typeof(void))
            return null;
        var nn = Nullable.GetUnderlyingType(t);
        if (nn is not null)
            return Unwrap(nn);
        if (t.IsArray)
            return Unwrap(t.GetElementType()!);
        if (t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t) && t.IsGenericType)
            return Unwrap(t.GetGenericArguments()[0]);
        return t;
    }

    private static bool IsObject(Type t) =>
        t
            is { IsClass: true, IsPrimitive: false }
                or { IsValueType: true, IsPrimitive: false, IsEnum: false }
        && t != typeof(string)
        && t != typeof(decimal)
        && t != typeof(DateTime)
        && t != typeof(DateOnly)
        && t != typeof(Guid);

    // --- Mapeamento C# -> TS ---
    private static string TsType(Type t)
    {
        var nn = Nullable.GetUnderlyingType(t);
        if (nn is not null)
            return TsType(nn);
        if (t.IsArray)
            return TsType(t.GetElementType()!) + "[]";
        if (t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t) && t.IsGenericType)
            return TsType(t.GetGenericArguments()[0]) + "[]";
        if (t.IsEnum)
            return t.Name;
        if (t == typeof(bool))
            return "boolean";
        if (
            t == typeof(string)
            || t == typeof(Guid)
            || t == typeof(DateTime)
            || t == typeof(DateOnly)
        )
            return "string";
        if (t.IsPrimitive || t == typeof(decimal))
            return "number";
        return t.Name; // objeto -> interface
    }

    private static void EmitEnum(StringBuilder sb, Type e)
    {
        var values = string.Join(" | ", Enum.GetNames(e).Select(n => $"\"{n}\""));
        sb.AppendLine($"export type {e.Name} = {values};\n");
    }

    private static void EmitInterface(StringBuilder sb, Type t)
    {
        sb.AppendLine($"export interface {t.Name} {{");
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var nullable = IsNullable(p);
            var name = Camel(p.Name) + (nullable ? "?" : "");
            var ts = TsType(p.PropertyType) + (nullable ? " | null" : "");
            sb.AppendLine($"  {name}: {ts};");
        }
        sb.AppendLine("}\n");
    }

    private static bool IsNullable(PropertyInfo p) =>
        Nullable.GetUnderlyingType(p.PropertyType) is not null
        || NullCtx.Create(p).ReadState == NullabilityState.Nullable;

    // --- Hooks React Query ---
    private static void EmitHook(StringBuilder sb, EndpointDef ep)
    {
        var pascal = char.ToUpper(ep.Name[0]) + ep.Name[1..];
        var resp = ep.Response == typeof(void) ? "void" : TsType(ep.Response);
        var prms = PathParams(ep.Pattern); // [(name, tsType)]

        if (ep.Method == "GET")
            EmitQuery(sb, ep, pascal, resp, prms);
        else
            EmitMutation(sb, ep, pascal, resp, prms);
    }

    private static void EmitQuery(
        StringBuilder sb,
        EndpointDef ep,
        string pascal,
        string resp,
        List<(string name, string ts)> prms
    )
    {
        var args = prms.Select(p => $"{p.name}: {p.ts}")
            .Append($"options?: Partial<UseQueryOptions<{resp}>>");
        var key = string.Join(", ", new[] { $"\"{ep.Name}\"" }.Concat(prms.Select(p => p.name)));
        var path = ep.Pattern.Replace("{", "${").Replace("}", "}"); // {id} -> ${id}

        sb.AppendLine($"export function use{pascal}({string.Join(", ", args)}) {{");
        sb.AppendLine("  return useQuery({");
        sb.AppendLine($"    queryKey: [{key}],");
        sb.AppendLine($"    queryFn: () => http<{resp}>(\"GET\", `{path}`),");
        sb.AppendLine("    ...options,");
        sb.AppendLine("  });");
        sb.AppendLine("}\n");
    }

    private static void EmitMutation(
        StringBuilder sb,
        EndpointDef ep,
        string pascal,
        string resp,
        List<(string name, string ts)> prms
    )
    {
        var hasBody = ep.Request is not null;
        var req = hasBody ? TsType(ep.Request!) : null;

        // Tipo das variables da mutation conforme params/body.
        string vars;
        string varArg; // assinatura do mutationFn
        string bodyArg; // 3o argumento do http()
        string path = ep.Pattern.Replace("{", "${vars.").Replace("}", "}"); // {id} -> ${vars.id}

        if (prms.Count > 0 && hasBody)
        {
            vars =
                "{ "
                + string.Join("; ", prms.Select(p => $"{p.name}: {p.ts}"))
                + $"; body: {req} }}";
            varArg = $"vars: {vars}";
            bodyArg = ", vars.body";
        }
        else if (prms.Count > 0)
        {
            vars = "{ " + string.Join("; ", prms.Select(p => $"{p.name}: {p.ts}")) + " }";
            varArg = $"vars: {vars}";
            bodyArg = "";
        }
        else if (hasBody)
        {
            vars = req!;
            varArg = $"vars: {vars}";
            bodyArg = ", vars";
            path = ep.Pattern; // sem params
        }
        else
        {
            vars = "void";
            varArg = "";
            bodyArg = "";
            path = ep.Pattern;
        }

        sb.AppendLine(
            $"export function use{pascal}(options?: Partial<UseMutationOptions<{resp}, Error, {vars}>>) {{"
        );
        sb.AppendLine("  const qc = useQueryClient();");
        sb.AppendLine("  return useMutation({");
        sb.AppendLine(
            $"    mutationFn: ({varArg}) => http<{resp}>(\"{ep.Method}\", `{path}`{bodyArg}),"
        );
        sb.AppendLine("    onSuccess: () => qc.invalidateQueries(),");
        sb.AppendLine("    ...options,");
        sb.AppendLine("  });");
        sb.AppendLine("}\n");
    }

    // {id} no path -> param tipado. Convenção: id/*Id = number, resto = string.
    private static List<(string, string)> PathParams(string pattern) =>
        pattern
            .Split('/')
            .Where(s => s.StartsWith('{') && s.EndsWith('}'))
            .Select(s => s[1..^1])
            .Select(n => (n, n == "id" || n.EndsWith("Id") ? "number" : "string"))
            .ToList();

    // Mesma política que o ASP.NET usa ao serializar -> nomes batem com o JSON real.
    private static string Camel(string s) => JsonNamingPolicy.CamelCase.ConvertName(s);

    private static void Header(StringBuilder sb)
    {
        sb.AppendLine(
            "// AUTOGERADO por `dotnet run --project server -- generate`. Não editar a mão."
        );
        sb.AppendLine("import {");
        sb.AppendLine("  useQuery,");
        sb.AppendLine("  useMutation,");
        sb.AppendLine("  useQueryClient,");
        sb.AppendLine("  type UseQueryOptions,");
        sb.AppendLine("  type UseMutationOptions,");
        sb.AppendLine("} from \"@tanstack/react-query\";\n");
    }

    private static void HttpCore(StringBuilder sb)
    {
        sb.AppendLine(
            "const BASE_URL = import.meta.env.VITE_API_URL ?? \"http://localhost:5101\";\n"
        );
        sb.AppendLine(
            "async function http<T>(method: string, path: string, body?: unknown): Promise<T> {"
        );
        sb.AppendLine("  const res = await fetch(BASE_URL + path, {");
        sb.AppendLine("    method,");
        sb.AppendLine(
            "    headers: body !== undefined ? { \"Content-Type\": \"application/json\" } : undefined,"
        );
        sb.AppendLine("    body: body !== undefined ? JSON.stringify(body) : undefined,");
        sb.AppendLine("  });");
        sb.AppendLine("  if (!res.ok) throw new Error(`${method} ${path} -> ${res.status}`);");
        sb.AppendLine("  if (res.status === 204) return undefined as T;");
        sb.AppendLine("  return res.json() as Promise<T>;");
        sb.AppendLine("}\n");
    }

    private static string ResolveOutPath()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "client")))
            dir = Directory.GetParent(dir)?.FullName;
        dir ??= Directory.GetCurrentDirectory();
        return Path.Combine(dir, "client", "src", "api", "generated.ts");
    }
}
