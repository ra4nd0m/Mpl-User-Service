type ProviderCurrency = string;
type InternalCurrency = string;

export const providerCodeToInternal: Record<ProviderCurrency, InternalCurrency> = {
    '₽/т': "RUB",
    '$/т': "USD",
    '¥/т': "CNY"
}

export const availableCurrencies = [
    "",
    "RUB",
    "USD",
    "CNY"
]
