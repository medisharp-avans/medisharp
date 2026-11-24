/**
 * Zet een numerieke TypeScript-enum om naar opties voor SelectField.
 * Een numerieke enum bevat ook de omgekeerde koppeling (getal -> naam);
 * die wordt hier weggefilterd.
 */
export function enumOptions(enumObject: Record<string, string | number>) {
  return Object.entries(enumObject)
    .filter((entry): entry is [string, number] => typeof entry[1] === 'number')
    .map(([label, value]) => ({ value, label }))
}
