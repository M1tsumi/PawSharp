using System;
using System.Diagnostics;

namespace PawSharp.Core.Metrics;

/// <summary>
/// Tracks memory usage of the PawSharp application.
/// </summary>
public interface IMemoryMetrics
{
    /// <summary>
    /// Gets the current memory usage in bytes.
    /// </summary>
    long GetCurrentMemoryBytes();

    /// <summary>
    /// Gets the peak memory usage in bytes since creation.
    /// </summary>
    long GetPeakMemoryBytes();

    /// <summary>
    /// Gets memory usage summary.
    /// </summary>
    MemorySummary GetSummary();

    /// <summary>
    /// Resets peak memory tracking.
    /// </summary>
    void ResetPeak();
}

/// <summary>
/// Default implementation of memory metrics tracking.
/// </summary>
public class MemoryMetrics : IMemoryMetrics
{
    private readonly Process _currentProcess = Process.GetCurrentProcess();
    private long _peakMemoryBytes;
    private DateTimeOffset _startTime = DateTimeOffset.UtcNow;

    public MemoryMetrics()
    {
        _peakMemoryBytes = _currentProcess.WorkingSet64;
    }

    public long GetCurrentMemoryBytes()
    {
        return _currentProcess.WorkingSet64;
    }

    public long GetPeakMemoryBytes()
    {
        return _peakMemoryBytes;
    }

    public MemorySummary GetSummary()
    {
        long currentMemory = GetCurrentMemoryBytes();
        
        // Update peak if current exceeds it
        if (currentMemory > _peakMemoryBytes)
            _peakMemoryBytes = currentMemory;

        return new MemorySummary
        {
            CurrentMemoryBytes = currentMemory,
            CurrentMemoryMB = currentMemory / (1024.0 * 1024.0),
            PeakMemoryBytes = _peakMemoryBytes,
            PeakMemoryMB = _peakMemoryBytes / (1024.0 * 1024.0),
            TotalProcessorTime = _currentProcess.TotalProcessorTime.TotalSeconds,
            UserProcessorTime = _currentProcess.UserProcessorTime.TotalSeconds,
            Handles = _currentProcess.HandleCount,
            Threads = _currentProcess.Threads.Count,
            UptimeSeconds = (long)(DateTimeOffset.UtcNow - _startTime).TotalSeconds
        };
    }

    public void ResetPeak()
    {
        _peakMemoryBytes = GetCurrentMemoryBytes();
    }
}

/// <summary>
/// Summary of memory and process metrics.
/// </summary>
public class MemorySummary
{
    public long CurrentMemoryBytes { get; set; }
    public double CurrentMemoryMB { get; set; }
    
    public long PeakMemoryBytes { get; set; }
    public double PeakMemoryMB { get; set; }
    
    public double TotalProcessorTime { get; set; }
    public double UserProcessorTime { get; set; }
    
    public int Handles { get; set; }
    public int Threads { get; set; }
    
    public long UptimeSeconds { get; set; }

    public override string ToString() =>
        $"Memory: {CurrentMemoryMB:F2}MB (Peak: {PeakMemoryMB:F2}MB) | " +
        $"Handles: {Handles} | Threads: {Threads} | " +
        $"Uptime: {UptimeSeconds}s";
}
