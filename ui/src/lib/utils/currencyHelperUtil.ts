type ProviderCurrency = string;
type InternalCurrency = string;
type CurrencyRates = Record<string, number>

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

function toInternalCurrencyCode(code: string): InternalCurrency {
    const trimmed = code.trim()
    return providerCodeToInternal[trimmed] ?? trimmed.toUpperCase()
}

/**
 * Converts value in two steps:
 * 1) source currency -> RUB
 * 2) RUB -> requested currency
 */
export function convertCurrencyValue(
    internalCode: string,
    value: number,
    requestedCurCode: string,
    curValues: CurrencyRates
): number {
    if (!Number.isFinite(value)) return value

    const fromCode = toInternalCurrencyCode(internalCode)
    const toCode = toInternalCurrencyCode(requestedCurCode)

    // Empty requested code means "keep original value".
    if (!toCode) return value

    const fromRate = fromCode === 'RUB' ? 1 : curValues[fromCode]
    const toRate = toCode === 'RUB' ? 1 : curValues[toCode]

    // If we cannot convert reliably, keep the original value.
    if (!fromRate || !toRate) return value

    const valueInRub = value * fromRate
    return valueInRub / toRate
}
