/**
 * Converts a numeric TypeScript enum into options suitable for a SelectField component.
 *
 * TypeScript numeric enums generate reverse mappings (numeric value to string name)
 * at runtime. This function filters those reverse mappings out so only the standard
 * name-to-number pairs are mapped into the options array.
 *
 * @param enumObject - The numeric TypeScript enum object to convert.
 * @returns An array of objects containing `label` (string) and `value` (number) properties.
 */
export function enumOptions(enumObject: Record<string, string | number>) {
  return Object.entries(enumObject)
    .filter((entry): entry is [string, number] => typeof entry[1] === 'number')
    .map(([label, value]) => ({ value, label }))
}
