import { ProductResponse } from '../models/product-response';

const PALETTES: ReadonlyArray<readonly [string, string]> = [
  ['#17665e', '#0d3a36'],
  ['#1f7a70', '#124f49'],
  ['#b86a14', '#7c450c'],
  ['#0f766e', '#134e4a'],
  ['#c2410c', '#7c2d12'],
  ['#0e7490', '#155e75'],
  ['#365314', '#1a2e05'],
  ['#334155', '#0f172a'],
];

function hashSeed(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash * 31 + value.charCodeAt(i)) | 0;
  }
  return Math.abs(hash);
}

export function productMediaStyle(product: ProductResponse): Record<string, string> {
  const seed = hashSeed(`${product.category}:${product.productID}`);
  const [toneA, toneB] = PALETTES[seed % PALETTES.length];
  return {
    '--tone-a': toneA,
    '--tone-b': toneB,
  };
}

export function productInitial(product: ProductResponse): string {
  const name = (product.productName ?? '').trim();
  return name ? name.charAt(0).toUpperCase() : '?';
}
