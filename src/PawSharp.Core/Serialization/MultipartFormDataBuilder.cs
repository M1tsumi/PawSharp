#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PawSharp.Core.Models;

namespace PawSharp.Core.Serialization;

/// <summary>
/// Builder for constructing multipart/form-data requests for Discord API file uploads.
/// </summary>
public class MultipartFormDataBuilder
{
    private readonly List<MultipartField> _fields = new();
    private readonly List<FileAttachment> _files = new();
    private readonly string _boundary;
    
    /// <summary>
    /// Creates a new MultipartFormDataBuilder with a random boundary.
    /// </summary>
    public MultipartFormDataBuilder() : this(Guid.NewGuid().ToString())
    {
    }
    
    /// <summary>
    /// Creates a new MultipartFormDataBuilder with a specific boundary.
    /// </summary>
    /// <param name="boundary">The boundary string to use.</param>
    public MultipartFormDataBuilder(string boundary)
    {
        _boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
    }
    
    /// <summary>
    /// Adds a text field to the multipart form data.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="value">The field value.</param>
    /// <returns>The builder for method chaining.</returns>
    public MultipartFormDataBuilder AddField(string name, string value)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Field name cannot be null or empty.", nameof(name));
        
        _fields.Add(new MultipartField(name, value ?? string.Empty));
        return this;
    }
    
    /// <summary>
    /// Adds a JSON field to the multipart form data.
    /// </summary>
    /// <param name="name">The field name (typically "payload_json").</param>
    /// <param name="json">The JSON string.</param>
    /// <returns>The builder for method chaining.</returns>
    public MultipartFormDataBuilder AddJson(string name, string json)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Field name cannot be null or empty.", nameof(name));
        
        _fields.Add(new MultipartField(name, json ?? string.Empty, "application/json"));
        return this;
    }
    
    /// <summary>
    /// Adds a file attachment to the multipart form data.
    /// </summary>
    /// <param name="file">The file attachment.</param>
    /// <returns>The builder for method chaining.</returns>
    public MultipartFormDataBuilder AddFile(FileAttachment file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        
        _files.Add(file);
        return this;
    }
    
    /// <summary>
    /// Adds a file attachment to the multipart form data.
    /// </summary>
    /// <param name="data">The file data.</param>
    /// <param name="fileName">The filename.</param>
    /// <param name="key">The field key.</param>
    /// <param name="contentType">Optional content type.</param>
    /// <returns>The builder for method chaining.</returns>
    public MultipartFormDataBuilder AddFile(byte[] data, string fileName, string key = "file", string? contentType = null)
    {
        var attachment = new FileAttachment(data, fileName)
        {
            Key = key,
            ContentType = contentType
        };
        _files.Add(attachment);
        return this;
    }
    
    /// <summary>
    /// Builds the multipart form data as a byte array.
    /// </summary>
    /// <returns>The multipart form data as a byte array.</returns>
    public byte[] Build()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        
        // Add text fields
        foreach (var field in _fields)
        {
            writer.WriteLine($"--{_boundary}");
            writer.WriteLine($"Content-Disposition: form-data; name=\"{field.Name}\"");
            
            if (!string.IsNullOrEmpty(field.ContentType))
            {
                writer.WriteLine($"Content-Type: {field.ContentType}");
            }
            
            writer.WriteLine();
            writer.WriteLine(field.Value);
        }
        
        // Add file attachments
        foreach (var file in _files)
        {
            writer.WriteLine($"--{_boundary}");
            writer.Write($"Content-Disposition: form-data; name=\"{file.Key}\"; filename=\"{file.FileName}\"");
            
            if (!string.IsNullOrEmpty(file.Description))
            {
                writer.Write($"; description=\"{file.Description}\"");
            }
            
            writer.WriteLine();
            
            if (!string.IsNullOrEmpty(file.ContentType))
            {
                writer.WriteLine($"Content-Type: {file.ContentType}");
            }
            else
            {
                // Try to infer content type from filename
                var inferredType = GetContentType(file.FileName);
                if (!string.IsNullOrEmpty(inferredType))
                {
                    writer.WriteLine($"Content-Type: {inferredType}");
                }
            }
            
            writer.WriteLine();
            writer.Flush();
            
            // Write file data
            memoryStream.Write(file.Data, 0, file.Data.Length);
            writer.WriteLine();
        }
        
        // Write boundary end
        writer.WriteLine($"--{_boundary}--");
        writer.Flush();
        
        return memoryStream.ToArray();
    }
    
    /// <summary>
    /// Gets the Content-Type header value for the multipart request.
    /// </summary>
    /// <returns>The Content-Type header value.</returns>
    public string GetContentType()
    {
        return $"multipart/form-data; boundary={_boundary}";
    }
    
    /// <summary>
    /// Gets the boundary string.
    /// </summary>
    /// <returns>The boundary string.</returns>
    public string GetBoundary()
    {
        return _boundary;
    }
    
    /// <summary>
    /// Clears all fields and files from the builder.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public MultipartFormDataBuilder Clear()
    {
        _fields.Clear();
        _files.Clear();
        return this;
    }
    
    /// <summary>
    /// Infers the content type from the file extension.
    /// </summary>
    /// <param name="fileName">The filename.</param>
    /// <returns>The inferred content type, or null if unknown.</returns>
    private static string? GetContentType(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".json" => "application/json",
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".mp3" => "audio/mpeg",
            ".mp4" => "video/mp4",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".aac" => "audio/aac",
            _ => null
        };
    }
    
    /// <summary>
    /// Represents a field in multipart form data.
    /// </summary>
    private class MultipartField
    {
        public string Name { get; }
        public string Value { get; }
        public string? ContentType { get; }
        
        public MultipartField(string name, string value, string? contentType = null)
        {
            Name = name;
            Value = value;
            ContentType = contentType;
        }
    }
}
