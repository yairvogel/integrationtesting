namespace application;

public record Page<T>(IReadOnlyList<T> data, string next);
