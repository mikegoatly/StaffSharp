using System.Diagnostics.CodeAnalysis;

// Multidimensional arrays are appropriate for ML/signal processing matrices
// They provide better memory locality and more natural syntax for 2D data like spectrograms and piano rolls
[assembly: SuppressMessage("Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "Multidimensional arrays are more appropriate for fixed-size matrix data in ML/DSP")]

// Array properties in records are immutable by design in this context
[assembly: SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Record properties with arrays are effectively immutable in this API design")]
