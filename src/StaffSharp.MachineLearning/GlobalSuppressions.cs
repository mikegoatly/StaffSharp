using System.Diagnostics.CodeAnalysis;

// Multidimensional arrays are appropriate for ML/signal processing matrices
// They provide better memory locality and more natural syntax for 2D data like spectrograms and piano rolls
[assembly: SuppressMessage("Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "Multidimensional arrays are more appropriate for fixed-size matrix data in ML/DSP")]

// Array properties in records are immutable by design in this context
[assembly: SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Record properties with arrays are effectively immutable in this API design")]

// SessionOptions is owned by InferenceSession after construction and is disposed with the session
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "~M:StaffSharp.MachineLearning.ML.Models.OnnxTranscriber.#ctor(System.String,StaffSharp.MachineLearning.Options.PolyphonicTranscriptionOptions)", Justification = "SessionOptions is owned by InferenceSession and disposed with it")]
