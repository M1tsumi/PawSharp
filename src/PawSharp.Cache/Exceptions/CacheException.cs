#nullable enable
using System;

namespace PawSharp.Cache.Exceptions
{
    /// <summary>
    /// Base exception for cache-related errors.
    /// </summary>
    public class CacheException : Exception
    {
        /// <summary>
        /// The cache provider that threw the exception.
        /// </summary>
        public string? ProviderName { get; }

        /// <summary>
        /// The cache operation that failed.
        /// </summary>
        public string? Operation { get; }

        public CacheException(string message) : base(message) { }

        public CacheException(string message, Exception innerException) : base(message, innerException) { }

        public CacheException(string message, string? providerName, string? operation) 
            : base(message)
        {
            ProviderName = providerName;
            Operation = operation;
        }

        public CacheException(string message, string? providerName, string? operation, Exception innerException) 
            : base(message, innerException)
        {
            ProviderName = providerName;
            Operation = operation;
        }
    }

    /// <summary>
    /// Thrown when a cache provider is not available or healthy.
    /// </summary>
    public class CacheProviderUnavailableException : CacheException
    {
        public CacheProviderUnavailableException(string providerName) 
            : base($"Cache provider '{providerName}' is not available or unhealthy.", providerName, null) { }

        public CacheProviderUnavailableException(string providerName, Exception innerException) 
            : base($"Cache provider '{providerName}' is not available or unhealthy.", providerName, null, innerException) { }
    }

    /// <summary>
    /// Thrown when cache swapping fails.
    /// </summary>
    public class CacheSwapException : CacheException
    {
        public CacheSwapException(string message) : base(message) { }

        public CacheSwapException(string message, Exception innerException) : base(message, innerException) { }

        public CacheSwapException(string message, string fromProvider, string toProvider) 
            : base($"Failed to swap cache from '{fromProvider}' to '{toProvider}': {message}", toProvider, "Swap") { }
    }

    /// <summary>
    /// Thrown when cache distribution fails.
    /// </summary>
    public class CacheDistributionException : CacheException
    {
        public CacheDistributionException(string message) : base(message) { }

        public CacheDistributionException(string message, Exception innerException) : base(message, innerException) { }

        public CacheDistributionException(string message, string operation) 
            : base($"Cache distribution failed during '{operation}': {message}", null, operation) { }
    }

    /// <summary>
    /// Thrown when a cache provider is not registered.
    /// </summary>
    public class CacheProviderNotRegisteredException : CacheException
    {
        public CacheProviderNotRegisteredException(string providerName) 
            : base($"Cache provider '{providerName}' is not registered.", providerName, null) { }
    }

    /// <summary>
    /// Thrown when a cache provider operation times out.
    /// </summary>
    public class CacheTimeoutException : CacheException
    {
        public TimeSpan Timeout { get; }

        public CacheTimeoutException(string providerName, string operation, TimeSpan timeout) 
            : base($"Cache operation '{operation}' on provider '{providerName}' timed out after {timeout.TotalSeconds:F2} seconds.", providerName, operation)
        {
            Timeout = timeout;
        }

        public CacheTimeoutException(string providerName, string operation, TimeSpan timeout, Exception innerException) 
            : base($"Cache operation '{operation}' on provider '{providerName}' timed out after {timeout.TotalSeconds:F2} seconds.", providerName, operation, innerException)
        {
            Timeout = timeout;
        }
    }
}
