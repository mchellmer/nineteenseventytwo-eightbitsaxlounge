namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

/// <summary>
/// Defines db agnostic methods for data access operations.
/// </summary>
public interface IDataAccess
{
    /// <summary>
    /// Loads data from the database asynchronously.
    /// </summary>
    /// <param name="dataFunction">
    /// The name of the data function e.g. stored procedure/view/service to execute.
    /// </param>
    /// <param name="parameters">
    /// The parameters to pass to the data function.
    /// </param>
    /// <param name="connectionStringName">
    /// The name of the connection string to use.
    /// </param>
    /// <typeparam name="T">
    /// The type of data to load, maps a list of returned data to typed objects.
    /// </typeparam>
    /// <typeparam name="TU">
    /// The type of the parameters object used to execute the function.
    /// </typeparam>
    /// <returns>
    /// A list of data of type T.
    /// </returns>
    Task<List<T>> LoadDataAsync<T, TU>(
        string dataFunction,
        TU parameters,
        string connectionStringName);

    /// <summary>
    /// Saves data to the database asynchronously.
    /// </summary>
    /// <param name="databaseFunction">
    /// The name of the database function e.g. stored procedure/view to execute.
    /// </param>
    /// <param name="parameters">
    /// The parameters to pass to the database function.
    /// </param>
    /// <param name="connectionStringName">
    /// The name of the connection string to use.
    /// </param>
    /// <typeparam name="T">
    /// The type of the parameters object used to execute the function in the database.
    /// </typeparam>
    /// <returns></returns>
    Task SaveDataAsync<T>(
        string databaseFunction,
        T parameters,
        string connectionStringName);
}