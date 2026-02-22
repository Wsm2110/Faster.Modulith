
namespace Module.Kitchen.Infrastructure
{
    /// <summary>
    /// Represents configuration options for the kitchen module, including database connection settings and in-memory
    /// storage preferences.
    /// </summary>
    /// <remarks>Use this class to specify how the kitchen module connects to its data source. The options
    /// allow for either a persistent database connection via a connection string or the use of in-memory storage for
    /// testing or lightweight scenarios. The default value for the connection string is an empty string. Set the
    /// UseInMemory property to <see langword="true"/> to enable in-memory storage.</remarks>
    public partial class KitchenOptions // Note this is a partial which extends the generated kitchen options 
    {
        /// <summary>
        /// 
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        public bool UseInMemory { get; set; }

    }
}
