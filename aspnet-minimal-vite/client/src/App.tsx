import { useState } from 'react'
import { useGetTodos, useCreateTodo } from './api/generated'
import './App.css'

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
    <main style={{ maxWidth: 480, margin: '2rem auto', fontFamily: 'sans-serif' }}>
      <h1>Todos</h1>

      <div style={{ display: 'flex', gap: 8 }}>
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
          placeholder="Nova tarefa"
          style={{ flex: 1 }}
        />
        <button type="button" onClick={handleAdd} disabled={createTodo.isPending}>
          Adicionar
        </button>
      </div>

      {todos.isPending && <p>Carregando...</p>}
      {todos.isError && <p>Erro: {todos.error.message}</p>}

      <ul>
        {todos.data?.map((todo) => (
          <li key={todo.id} style={{ textDecoration: todo.done ? 'line-through' : 'none' }}>
            {todo.title}
          </li>
        ))}
      </ul>
    </main>
  )
}

export default App
