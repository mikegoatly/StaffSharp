namespace StaffSharp.MachineLearning.ML.Models;

using System;

/// <summary>
/// Provides a helper around loading a model from file, caching the model bytes.
/// Only the last model is cached to limit memory usage.
/// </summary>
public static class ModelLoader
{
    private static (string lastModelPath, byte[] lastModelData)? lastModel;
    /// <summary>
    /// Loads the model from the specified path, caching the bytes if it's the same as the last loaded model.
    /// </summary>
    /// <param name="modelPath">The file path to the ONNX model.</param>
    /// <returns>The model bytes.</returns>
    public static byte[] LoadModel(string modelPath)
    {
        if (lastModel is var (lastModelPath, lastModelData) && lastModelPath == modelPath)
        {
            return lastModelData;
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"ONNX model not found at: {modelPath}", modelPath);
        }

        var modelData = File.ReadAllBytes(modelPath);

        if (IsZipped(modelData))
        {
            modelData = UnzipModel(modelData);
        }

        lastModel = (modelPath, modelData);
        return modelData;
    }

    private static bool IsZipped(byte[] data)
    {
        // Check for ZIP file signature (first 4 bytes: 50 4B 03 04)
        return data is [0x50, 0x4B, 0x03, 0x04, ..];
    }

    private static byte[] UnzipModel(byte[] zippedData)
    {
        using var compressedStream = new MemoryStream(zippedData);
        using var zipArchive = new System.IO.Compression.ZipArchive(compressedStream, System.IO.Compression.ZipArchiveMode.Read);
        var entry = zipArchive.Entries.FirstOrDefault()
            ?? throw new InvalidOperationException("No entries found in the zipped model.");

        using var entryStream = entry.Open();
        using var memoryStream = new MemoryStream();
        entryStream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
