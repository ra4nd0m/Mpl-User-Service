namespace MplDataReceiver.Models.DTOs;

public record Payload<T>(
    T Data
);