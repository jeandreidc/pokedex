const TYPE_COLORS: Record<string, string> = {
  normal: '#A8A878',
  fire: '#F08030',
  water: '#6890F0',
  electric: '#F8D030',
  grass: '#78C850',
  ice: '#98D8D8',
  fighting: '#C03028',
  poison: '#A040A0',
  ground: '#E0C068',
  flying: '#A890F0',
  psychic: '#F85888',
  bug: '#A8B820',
  rock: '#B8A038',
  ghost: '#705898',
  dragon: '#7038F8',
  dark: '#705848',
  steel: '#B8B8D0',
  fairy: '#EE99AC'
};

export function formatPokemonName(name: string): string {
  return name
    .split('-')
    .map(part => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
}

export function typeColor(type: string): string {
  return TYPE_COLORS[type.toLowerCase()] ?? '#888';
}

export function formatGenerationRoman(generation?: string | null): string | null {
  if (!generation) return null;
  const trimmed = generation.trim();
  if (/^[IVX]+$/i.test(trimmed)) return trimmed.toUpperCase();
  const match = trimmed.match(/generation[-\s]*([IVX]+)/i);
  return match ? match[1].toUpperCase() : trimmed.toUpperCase();
}

export function formatGenLabel(generation?: string | null): string | null {
  const roman = formatGenerationRoman(generation);
  return roman ? `GEN ${roman}` : null;
}

export function computePageSize(width: number, height: number): number {
  const cardWidth = 268;
  const cardHeight = 96;
  const cols = Math.max(1, Math.floor(width / cardWidth));
  const rows = Math.max(1, Math.floor(height / cardHeight));
  return Math.min(100, Math.max(6, cols * rows));
}
