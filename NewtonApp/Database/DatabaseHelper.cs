using System;
using System.Collections.Generic;
using Npgsql;
using NewtonApp.Models;

namespace NewtonApp.Database
{
    public class DatabaseHelper
    {
        
        private readonly string _connectionString = "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=integraldb";

        public DatabaseHelper()
        {
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS solutions (
                        id SERIAL PRIMARY KEY,
                        solution_time TIMESTAMP NOT NULL,
                        equation TEXT NOT NULL,
                        initial_guess DOUBLE PRECISION NOT NULL,
                        root DOUBLE PRECISION NOT NULL,
                        iterations INTEGER NOT NULL,
                        error DOUBLE PRECISION NOT NULL
                    )";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB error: {ex.Message}");
            }
        }

        public bool SaveSolution(SolutionRecord record)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                string sql = @"INSERT INTO solutions 
                    (solution_time, equation, initial_guess, root, iterations, error)
                    VALUES (@time, @eq, @init, @root, @iter, @err)";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@time", record.SolutionTime);
                cmd.Parameters.AddWithValue("@eq", record.Equation);
                cmd.Parameters.AddWithValue("@init", record.InitialGuess);
                cmd.Parameters.AddWithValue("@root", record.Root);
                cmd.Parameters.AddWithValue("@iter", record.IterationsCount);
                cmd.Parameters.AddWithValue("@err", record.Error);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
                return false;
            }
        }

        public List<SolutionRecord> GetAllSolutions()
        {
            var list = new List<SolutionRecord>();
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                string sql = "SELECT id, solution_time, equation, initial_guess, root, iterations, error FROM solutions ORDER BY solution_time DESC";
                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new SolutionRecord
                    {
                        Id = reader.GetInt32(0),
                        SolutionTime = reader.GetDateTime(1),
                        Equation = reader.GetString(2),
                        InitialGuess = reader.GetDouble(3),
                        Root = reader.GetDouble(4),
                        IterationsCount = reader.GetInt32(5),
                        Error = reader.GetDouble(6)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
            }
            return list;
        }

        public bool ClearAll()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM solutions", conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool TestConnection()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                return true;
            }
            catch { return false; }
        }
    }
}