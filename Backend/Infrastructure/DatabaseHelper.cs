using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace HospitalNet.Backend.Infrastructure
{
    /// <summary>
    /// DatabaseHelper class handles all database connectivity and command execution.
    /// Uses ADO.NET (SqlConnection, SqlCommand, SqlDataReader).
    /// No ORM allowed - Pure SQL and Stored Procedures only.
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor - Initialize with connection string
        /// </summary>
        /// <param name="connectionString">MSSQL connection string</param>
        public DatabaseHelper(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty");

            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets a new SqlConnection instance
        /// </summary>
        /// <returns>New SqlConnection with the configured connection string</returns>
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Executes a stored procedure and returns the number of rows affected.
        /// Used for INSERT, UPDATE, DELETE operations.
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure (e.g., "sp_CreateAppointment")</param>
        /// <param name="parameters">Array of SqlParameters</param>
        /// <returns>Number of rows affected</returns>
        /// <exception cref="SqlException">Thrown if database operation fails</exception>
        public int ExecuteNonQuery(string storedProcedureName, params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedureName))
                throw new ArgumentNullException(nameof(storedProcedureName));

            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 30; // 30-second timeout

                    // Add parameters
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    try
                    {
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Database error executing stored procedure '{storedProcedureName}': {ex.Message}", ex);
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Executes a stored procedure and returns output parameters.
        /// Used for procedures that return values via OUTPUT parameters.
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure</param>
        /// <param name="parameters">Array of SqlParameters (can include OUTPUT parameters)</param>
        /// <returns>Dictionary of parameter names and their output values</returns>
        /// <exception cref="SqlException">Thrown if database operation fails</exception>
        public Dictionary<string, object> ExecuteNonQueryWithOutputs(string storedProcedureName, params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedureName))
                throw new ArgumentNullException(nameof(storedProcedureName));

            var outputValues = new Dictionary<string, object>();

            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 30;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        // Collect all OUTPUT parameters
                        foreach (SqlParameter param in command.Parameters)
                        {
                            if (param.Direction == ParameterDirection.Output || 
                                param.Direction == ParameterDirection.InputOutput)
                            {
                                outputValues[param.ParameterName] = param.Value ?? DBNull.Value;
                            }
                        }

                        return outputValues;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Database error executing stored procedure '{storedProcedureName}': {ex.Message}", ex);
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Executes a stored procedure and returns a scalar value (single value, first row/column).
        /// Used for COUNT, SUM, or other aggregate operations.
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure</param>
        /// <param name="parameters">Array of SqlParameters</param>
        /// <returns>The scalar value returned by the query</returns>
        /// <exception cref="SqlException">Thrown if database operation fails</exception>
        public object ExecuteScalar(string storedProcedureName, params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedureName))
                throw new ArgumentNullException(nameof(storedProcedureName));

            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 30;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    try
                    {
                        connection.Open();
                        object result = command.ExecuteScalar();
                        return result;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Database error executing stored procedure '{storedProcedureName}': {ex.Message}", ex);
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Executes a stored procedure and returns a DataTable with all results.
        /// Used for SELECT queries that return multiple rows.
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure</param>
        /// <param name="parameters">Array of SqlParameters</param>
        /// <returns>DataTable containing the query results</returns>
        /// <exception cref="SqlException">Thrown if database operation fails</exception>
        public DataTable ExecuteReader(string storedProcedureName, params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedureName))
                throw new ArgumentNullException(nameof(storedProcedureName));

            DataTable dataTable = new DataTable();

            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 30;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                        return dataTable;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Database error executing stored procedure '{storedProcedureName}': {ex.Message}", ex);
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Executes a stored procedure and applies a mapping function to each row.
        /// Generic method for strongly-typed result mapping.
        /// </summary>
        /// <typeparam name="T">Type to map result rows to</typeparam>
        /// <param name="storedProcedureName">Name of the stored procedure</param>
        /// <param name="mapFunction">Function to map SqlDataReader row to T</param>
        /// <param name="parameters">Array of SqlParameters</param>
        /// <returns>List of mapped objects</returns>
        /// <exception cref="SqlException">Thrown if database operation fails</exception>
        public List<T> ExecuteReader<T>(string storedProcedureName, Func<SqlDataReader, T> mapFunction, params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedureName))
                throw new ArgumentNullException(nameof(storedProcedureName));

            if (mapFunction == null)
                throw new ArgumentNullException(nameof(mapFunction), "Mapping function cannot be null");

            List<T> results = new List<T>();

            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 30;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(mapFunction(reader));
                            }
                        }
                        return results;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Database error executing stored procedure '{storedProcedureName}': {ex.Message}", ex);
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        /// <returns>True if connection successful, False otherwise</returns>
        public bool TestConnection()
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to create a SqlParameter for input parameters
        /// </summary>
        /// <param name="parameterName">Parameter name (e.g., "@PatientID")</param>
        /// <param name="value">Parameter value</param>
        /// <returns>SqlParameter ready to use</returns>
        public static SqlParameter CreateInputParameter(string parameterName, object value)
        {
            return new SqlParameter
            {
                ParameterName = parameterName,
                Value = value ?? DBNull.Value,
                Direction = ParameterDirection.Input
            };
        }

        /// <summary>
        /// Helper method to create a SqlParameter for output parameters
        /// </summary>
        /// <param name="parameterName">Parameter name (e.g., "@AppointmentID")</param>
        /// <param name="sqlType">SQL data type (e.g., SqlDbType.Int)</param>
        /// <returns>SqlParameter ready to use</returns>
        public static SqlParameter CreateOutputParameter(string parameterName, SqlDbType sqlType)
        {
            return new SqlParameter
            {
                ParameterName = parameterName,
                SqlDbType = sqlType,
                Direction = ParameterDirection.Output
            };
        }

        /// <summary>
        /// Helper method to create a SqlParameter for input/output parameters
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="sqlType">SQL data type</param>
        /// <param name="value">Initial value</param>
        /// <returns>SqlParameter ready to use</returns>
        public static SqlParameter CreateInputOutputParameter(string parameterName, SqlDbType sqlType, object value)
        {
            return new SqlParameter
            {
                ParameterName = parameterName,
                SqlDbType = sqlType,
                Value = value ?? DBNull.Value,
                Direction = ParameterDirection.InputOutput
            };
        }

        /// <summary>
        /// Helper method to safely get a string value from SqlDataReader, handling NULL
        /// </summary>
        /// <param name="reader">SqlDataReader instance</param>
        /// <param name="columnName">Column name</param>
        /// <returns>String value or empty string if NULL</returns>
        public static string GetStringValue(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        /// <summary>
        /// Helper method to safely get an int value from SqlDataReader, handling NULL
        /// </summary>
        /// <param name="reader">SqlDataReader instance</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Int value or 0 if NULL</returns>
        public static int GetIntValue(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }

        /// <summary>
        /// Helper method to safely get a DateTime value from SqlDataReader, handling NULL
        /// </summary>
        /// <param name="reader">SqlDataReader instance</param>
        /// <param name="columnName">Column name</param>
        /// <returns>DateTime value or DateTime.MinValue if NULL</returns>
        public static DateTime GetDateTimeValue(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }

        /// <summary>
        /// Helper method to safely get a boolean value from SqlDataReader, handling NULL
        /// </summary>
        /// <param name="reader">SqlDataReader instance</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Boolean value or false if NULL</returns>
        public static bool GetBoolValue(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? false : reader.GetBoolean(ordinal);
        }

        /// <summary>
        /// Helper method to safely get a date (Date) value from SqlDataReader, handling NULL
        /// </summary>
        /// <param name="reader">SqlDataReader instance</param>
        /// <param name="columnName">Column name</param>
        /// <returns>DateTime value (date only) or DateTime.MinValue if NULL</returns>
        public static DateTime GetDateValue(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return DateTime.MinValue;

            // Handle both DATE and DATETIME columns
            return reader.GetDateTime(ordinal).Date;
        }
    }
}
