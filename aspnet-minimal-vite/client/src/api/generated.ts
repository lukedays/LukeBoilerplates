// AUTOGERADO por `dotnet run --project server -- generate`. Não editar a mão.
import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
  type UseMutationOptions,
} from "@tanstack/react-query";

export interface TodoDto {
  id: number;
  title: string;
  done: boolean;
}

export interface CreateTodoRequest {
  title: string;
}

const BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5101";

async function http<T>(method: string, path: string, body?: unknown): Promise<T> {
  const res = await fetch(BASE_URL + path, {
    method,
    headers: body !== undefined ? { "Content-Type": "application/json" } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) throw new Error(`${method} ${path} -> ${res.status}`);
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export function useGetTodos(options?: Partial<UseQueryOptions<TodoDto[]>>) {
  return useQuery({
    queryKey: ["getTodos"],
    queryFn: () => http<TodoDto[]>("GET", `/api/todos`),
    ...options,
  });
}

export function useGetTodo(id: number, options?: Partial<UseQueryOptions<TodoDto>>) {
  return useQuery({
    queryKey: ["getTodo", id],
    queryFn: () => http<TodoDto>("GET", `/api/todos/${id}`),
    ...options,
  });
}

export function useCreateTodo(options?: Partial<UseMutationOptions<TodoDto, Error, CreateTodoRequest>>) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (vars: CreateTodoRequest) => http<TodoDto>("POST", `/api/todos`, vars),
    onSuccess: () => qc.invalidateQueries(),
    ...options,
  });
}

