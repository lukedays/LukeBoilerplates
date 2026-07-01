import { useState } from 'react'
import { useGetTodos, useCreateTodo } from './api/generated'

// Exemplo de consumo dos hooks gerados a partir do backend.
function App() {
  const [title, setTitle] = useState('')
  const todos = useGetTodos()
  const createTodo = useCreateTodo()

  function handleAdd() {
    if (!title.trim()) return
    createTodo.mutate({ title })
    setTitle('')
  }

  return (
    <main className="mx-auto mt-8 max-w-md px-4 font-sans">
      <h1 className="mb-4 text-2xl font-bold">Todos</h1>

      <div className="flex gap-2">
        <input
          value={title}
          onChange={e => setTitle(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleAdd()}
          placeholder="Nova tarefa"
          className="flex-1 rounded border border-gray-300 px-3 py-2 outline-none focus:border-gray-500"
        />
        <button
          type="button"
          onClick={handleAdd}
          disabled={createTodo.isPending}
          className="rounded bg-gray-900 px-4 py-2 text-white disabled:opacity-50"
        >
          Adicionar
        </button>
      </div>

      {todos.isPending && <p className="mt-4 text-gray-500">Carregando...</p>}
      {todos.isError && <p className="mt-4 text-red-600">Erro: {todos.error.message}</p>}

      <ul className="mt-4 flex flex-col gap-1">
        {todos.data?.map(todo => (
          <li key={todo.id} className={todo.done ? 'text-gray-400 line-through' : ''}>
            {todo.title}
          </li>
        ))}
      </ul>
    </main>
  )
}

export default App
