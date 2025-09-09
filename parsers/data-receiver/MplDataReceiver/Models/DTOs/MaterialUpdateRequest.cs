namespace MplDataReceiver.Models.DTOs;

public record MaterialUpdate(
    int MaterialId,
    List<DateValue> DateValues
);
public record DateValue(
    string Date,
    List<PropertyValue> PropertyValues
);

public record PropertyValue(
    int PropertyId,
    string Value
);

