using System.Threading.Tasks;

namespace TowerDefense.Streaming.Interfaces
{
    /// <summary>
    /// Interface for managing authentication tokens.
    /// </summary>
    public interface IAuthManager
    {
        /// <summary>
        /// Asynchronously retrieves a valid authentication token.
        /// </summary>
        /// <returns>A valid authentication token string.</returns>
        Task<string> GetAuthTokenAsync();
        
        /// <summary>
        /// Asynchronously refreshes the current authentication token.
        /// </summary>
        /// <returns>True if refresh was successful, false otherwise.</returns>
        Task<bool> RefreshTokenAsync();
        
        /// <summary>
        /// Checks if the current token is valid and not expired.
        /// </summary>
        /// <returns>True if token is valid, false otherwise.</returns>
        bool IsTokenValid();
    }
}
