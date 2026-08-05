using System;

namespace OutOfWhatApp.Models;

public record RatingEntry(DateTimeOffset Timestamp, int Numerator, int Denominator, string? Note);
