using System.Reflection;
using System.Text.Json;
using Composta.Interfaces;
using Microsoft.Data.Sqlite;

namespace Composta.Services
{
    public class SqliteRepository<T> : IRepository<T> where T : class
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly PropertyInfo _idProperty;

        public SqliteRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string tidak boleh kosong.", nameof(connectionString));
            }

            _connectionString = connectionString;
            _tableName = typeof(T).Name;
            _idProperty = typeof(T).GetProperty("Id")
                ?? throw new InvalidOperationException($"Class {typeof(T).Name} harus memiliki property Id.");

            EnsureTable();
        }

        public string ConnectionString
        {
            get { return _connectionString; }
        }

        public void Add(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"INSERT INTO \"{_tableName}\" (Id, Data) VALUES ($id, $data)";
            command.Parameters.AddWithValue("$id", GetId(entity));
            command.Parameters.AddWithValue("$data", JsonSerializer.Serialize(entity));
            command.ExecuteNonQuery();
        }

        public T? GetById(string id)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT Data FROM \"{_tableName}\" WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);

            object? result = command.ExecuteScalar();
            return result is string json ? JsonSerializer.Deserialize<T>(json) : null;
        }

        public List<T> GetAll()
        {
            List<T> entities = new List<T>();

            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT Data FROM \"{_tableName}\"";

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                T? entity = JsonSerializer.Deserialize<T>(reader.GetString(0));
                if (entity != null)
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        public void Update(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            string id = GetId(entity);
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"UPDATE \"{_tableName}\" SET Data = $data WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$data", JsonSerializer.Serialize(entity));

            if (command.ExecuteNonQuery() == 0)
            {
                throw new KeyNotFoundException($"{_tableName} dengan Id {id} tidak ditemukan.");
            }
        }

        public void Delete(string id)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM \"{_tableName}\" WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        private void EnsureTable()
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"CREATE TABLE IF NOT EXISTS \"{_tableName}\" (Id TEXT PRIMARY KEY, Data TEXT NOT NULL)";
            command.ExecuteNonQuery();
        }

        private SqliteConnection OpenConnection()
        {
            SqliteConnection connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private string GetId(T entity)
        {
            string? id = _idProperty.GetValue(entity)?.ToString();
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException($"Id pada {_tableName} tidak boleh kosong.");
            }
            return id;
        }
    }
}
