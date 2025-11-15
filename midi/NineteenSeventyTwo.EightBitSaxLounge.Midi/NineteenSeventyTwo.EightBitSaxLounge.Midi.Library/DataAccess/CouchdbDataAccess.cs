using System.Collections;
using System.Reflection;
using CouchDB.Driver;
using CouchDB.Driver.Types;
using CouchDB.Driver.Views;
using Microsoft.Extensions.Configuration;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public class CouchdbDataAccess : IDataAccess
{
    private readonly IConfiguration _config;
    
    public CouchdbDataAccess(IConfiguration config)
    {
        this._config = config;
    }
    
    public async Task<List<T>> LoadDataAsync<T, TU>(string couchdbView, TU parameters, string connectionStringName)
    {
        string? connectionString = _config.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        var dbName = ExtractDbName(parameters) ?? throw new ArgumentException("Database name not found in parameters.", nameof(parameters));

        var viewSpec = couchdbView?.StartsWith("view:", StringComparison.OrdinalIgnoreCase) == true
            ? couchdbView.Substring(5)
            : couchdbView ?? throw new ArgumentException("couchdbView is null", nameof(couchdbView));

        var parts = viewSpec.Split(new[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) throw new ArgumentException(
            "couchdbView must be in the form 'designDoc/viewName' (optionally prefixed with 'view:').", nameof(couchdbView));

        var designDoc = parts[0];
        var viewName = parts[1];

        // locate ViewOptions on TU (case-insensitive)
        object? optionsObj = null;
        if (parameters != null)
        {
            var paramType = parameters.GetType();
            var optProp = paramType.GetProperty(
                "ViewOptions", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (optProp != null)
            {
                optionsObj = optProp.GetValue(parameters);
            }
        }

        if (optionsObj == null)
            throw new ArgumentException("Parameters must contain a non-null ViewOptions property of type CouchViewOptions<TKey>.", nameof(parameters));

        var optType = optionsObj.GetType();
        if (!optType.IsGenericType || !optType.GetGenericTypeDefinition().Name.StartsWith("CouchViewOptions", StringComparison.Ordinal))
            throw new ArgumentException("ViewOptions must be a CouchViewOptions<TKey> instance.", nameof(parameters));

        await using var client = new CouchClient(connectionString);
        var couchDb = client.GetDatabase<CouchDocument>(dbName);
        
        // use dynamic to preserve TKey generic and call the correct generic method
        dynamic dbDyn = couchDb;
        dynamic optDyn = optionsObj;
        dynamic detailedDynamic = await dbDyn.GetDetailedViewAsync(designDoc, viewName, optDyn);

        var result = new List<T>();

        if (detailedDynamic is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                // items are expected to be the detailed view result elements (cast to T)
                result.Add((T)item);
            }
            return result;
        }

        // If runtime shape is unexpected, throw
        throw new InvalidOperationException("Unexpected view result shape; expected an enumerable of detailed view items.");
    }

    public Task SaveDataAsync<T>(string couchdbView, T parameters, string connectionStringName)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Extracts the database name from the parameters object using reflection.
    /// </summary>
    /// <param name="parameters">
    /// The parameters object which may contain a property named "DatabaseName".
    /// </param>
    /// <returns>
    /// The database name if found; otherwise, null.
    /// </returns>
    private static string? ExtractDbName(object? parameters)
    {
        if (parameters == null) return null;
        
        string? dbName = null;

        // reflection: common property names
        var propNames = new[] { "DatabaseName" };
        var type = parameters.GetType();
        foreach (var name in propNames)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.PropertyType == typeof(string))
            {
                dbName = prop.GetValue(parameters) as string;
            }
        }

        return dbName;
    }
}