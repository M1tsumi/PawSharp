#nullable enable
using System;

namespace PawSharp.Core.Models;

/// <summary>
/// Represents a file attachment for multipart/form-data uploads to Discord.
/// </summary>
public class FileAttachment
{
    /// <summary>
    /// The file data as a byte array.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// The filename to use for the upload.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// The MIME content type of the file (e.g., "image/png", "application/json").
    /// If null, Discord will attempt to infer the content type from the filename.
    /// </summary>
    public string? ContentType { get; set; }
    
    /// <summary>
    /// The key/field name for the file in the multipart form data.
    /// For message attachments, this is typically "files[{index}]".
    /// For stickers, this is "file".
    /// For avatars/banners, this is "file" or "image".
    /// </summary>
    public string Key { get; set; } = "file";
    
    /// <summary>
    /// Optional description/alt text for the file (used for images).
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Creates a new FileAttachment.
    /// </summary>
    public FileAttachment() { }
    
    /// <summary>
    /// Creates a new FileAttachment with the specified data and filename.
    /// </summary>
    /// <param name="data">The file data.</param>
    /// <param name="fileName">The filename.</param>
    public FileAttachment(byte[] data, string fileName)
    {
        Data = data ?? Array.Empty<byte>();
        FileName = fileName ?? string.Empty;
    }
    
    /// <summary>
    /// Creates a new FileAttachment with the specified data, filename, and content type.
    /// </summary>
    /// <param name="data">The file data.</param>
    /// <param name="fileName">The filename.</param>
    /// <param name="contentType">The MIME content type.</param>
    public FileAttachment(byte[] data, string fileName, string contentType)
    {
        Data = data ?? Array.Empty<byte>();
        FileName = fileName ?? string.Empty;
        ContentType = contentType;
    }
    
    /// <summary>
    /// Creates a FileAttachment for a message attachment.
    /// </summary>
    /// <param name="data">The file data.</param>
    /// <param name="fileName">The filename.</param>
    /// <param name="index">The attachment index (0-9).</param>
    /// <param name="description">Optional alt text.</param>
    /// <returns>A FileAttachment configured for message upload.</returns>
    public static FileAttachment ForMessage(byte[] data, string fileName, int index = 0, string? description = null)
    {
        return new FileAttachment(data, fileName)
        {
            Key = $"files[{index}]",
            Description = description
        };
    }
    
    /// <summary>
    /// Creates a FileAttachment for a sticker upload.
    /// </summary>
    /// <param name="data">The file data.</param>
    /// <param name="fileName">The filename.</param>
    /// <returns>A FileAttachment configured for sticker upload.</returns>
    public static FileAttachment ForSticker(byte[] data, string fileName)
    {
        return new FileAttachment(data, fileName)
        {
            Key = "file"
        };
    }
    
    /// <summary>
    /// Creates a FileAttachment for an avatar or banner upload.
    /// </summary>
    /// <param name="data">The file data.</param>
    /// <param name="fileName">The filename.</param>
    /// <returns>A FileAttachment configured for avatar/banner upload.</returns>
    public static FileAttachment ForAvatar(byte[] data, string fileName)
    {
        return new FileAttachment(data, fileName)
        {
            Key = "file"
        };
    }
    
    /// <summary>
    /// Creates a FileAttachment for an emoji upload.
    /// </summary>
    /// <param name="data">The file data.</param>
    /// <param name="fileName">The filename.</param>
    /// <returns>A FileAttachment configured for emoji upload.</returns>
    public static FileAttachment ForEmoji(byte[] data, string fileName)
    {
        return new FileAttachment(data, fileName)
        {
            Key = "file"
        };
    }
}
