import {random} from 'remotion';
import {ART_SIZE, ArtName, GRASS_CELL, GRASS_MAP} from './art.gen';

/**
 * Capy Island, grown up, laid out in world units the way the game measures them: the land art is
 * 10 px per unit (51.3 × 28 units), every other sprite 128 px per unit, at the game's own scales
 * (IslandBuilder.Catalog). x runs right and y runs down; (0, 0) is the middle of the land art.
 */

export const LAND = {w: 51.3, h: 28};
export const SPRITE_PPU = 128;

export type Kind = 'building' | 'tree' | 'garden' | 'capy' | 'king' | 'deco' | 'tower' | 'lantern';

export type Thing = {
	art: ArtName;
	x: number;
	/** Where it stands: the bottom of the sprite. */
	y: number;
	scale: number;
	flip?: boolean;
	kind: Kind;
};

export const size = (t: Pick<Thing, 'art' | 'scale'>): [number, number] => {
	const [w, h] = ART_SIZE[t.art];
	return [(w / SPRITE_PPU) * t.scale, (h / SPRITE_PPU) * t.scale];
};

/** The Steam Tower, with the hot spring in front of it (HotSpring.cs: 4.4 units across, tower grown 1.25×). */
export const TOWER = {x: 0, y: -1.5, scale: 2.4 * 1.25};
export const POOL = (() => {
	const [pw, ph] = ART_SIZE.HotSpring;
	const w = 4.4;
	const h = (w * ph) / pw;
	return {x: TOWER.x, y: TOWER.y + h * 0.3, w, h};
})();

const PLACED: Thing[] = [
	{art: 'Toro', x: -3.5, y: 0.4, scale: 0.85, kind: 'lantern'},
	{art: 'Toro', x: 3.5, y: 0.4, scale: 0.85, kind: 'lantern'},
	{art: 'CapyKing', x: 3.1, y: 1.7, scale: 1.3, kind: 'king'},
	{art: 'CapyMiso', x: -3.0, y: 2.1, scale: 1.25, flip: true, kind: 'capy'},

	{art: 'StoreGeneral', x: -8, y: -2.6, scale: 1.8, kind: 'building'},
	{art: 'StoreBronze', x: 7.8, y: -3.6, scale: 1.8, kind: 'building'},
	{art: 'StoreSilver', x: 12.2, y: 1.6, scale: 1.8, kind: 'building'},
	{art: 'CapyHouse', x: -10.5, y: 3.6, scale: 1.7, kind: 'building'},
	{art: 'CapyHouse', x: -14.8, y: -1.2, scale: 1.7, flip: true, kind: 'building'},
	{art: 'Sawmill', x: 15.4, y: -4.6, scale: 1.7, kind: 'building'},
	{art: 'Mine', x: -9.6, y: -6.9, scale: 1.7, kind: 'building'},
	{art: 'HillGray', x: -13.6, y: -6.4, scale: 1.6, kind: 'building'},
	{art: 'HillGray', x: -6.4, y: -8.2, scale: 1.2, kind: 'building'},
	{art: 'Fountain', x: -4.6, y: 6.0, scale: 1.9, kind: 'building'},

	{art: 'GardenTulip', x: 3.8, y: 5.6, scale: 1.6, kind: 'garden'},
	{art: 'GardenRose', x: 7.4, y: 6.4, scale: 1.6, kind: 'garden'},
	{art: 'GardenPosie', x: 10.8, y: 5.4, scale: 1.6, kind: 'garden'},
	{art: 'GardenTulip', x: -8.4, y: 7.6, scale: 1.6, kind: 'garden'},

	{art: 'TreePink', x: 4.4, y: -6.6, scale: 1.7, kind: 'tree'},
	{art: 'TreeWhite', x: -4.2, y: -4.4, scale: 1.7, kind: 'tree'},
	{art: 'TreePink', x: 0.6, y: 8.2, scale: 1.7, kind: 'tree'},

	{art: 'CapyIdle', x: 6.4, y: 2.6, scale: 1.2, flip: true, kind: 'capy'},
	{art: 'CapyIdle', x: -6.6, y: 1.0, scale: 1.2, kind: 'capy'},
	{art: 'CapyIdle', x: 13.6, y: 4.6, scale: 1.2, kind: 'capy'},
	{art: 'CapyIdle', x: -12.0, y: 6.0, scale: 1.2, flip: true, kind: 'capy'},
	{art: 'CapyIdle', x: 1.2, y: 4.4, scale: 1.2, kind: 'capy'},
];

/** Grass under this point, with a margin of `pad` units all round. */
const onGrass = (x: number, y: number, pad = 0.6) => {
	for (const dx of [-pad, 0, pad])
		for (const dy of [-pad, 0, pad]) {
			const col = Math.floor((x + dx + LAND.w / 2) / GRASS_CELL);
			const row = Math.floor((y + dy + LAND.h / 2) / GRASS_CELL);
			if (GRASS_MAP[row]?.[col] !== '1') return false;
		}
	return true;
};

/** How much room each kind keeps around itself. */
const ROOM: Record<Kind, number> = {building: 2.1, tower: 3, tree: 1.15, garden: 1.6, capy: 0.9, king: 0.9, deco: 0.5, lantern: 0.9};

const scatter = (): Thing[] => {
	const things = [...PLACED];
	// The spring and the foot of the tower stay clear.
	const bySpring = (x: number, y: number, room: number) =>
		Math.abs(x - TOWER.x) < 2.6 + room && y > TOWER.y - 2.5 && y < POOL.y + POOL.h / 2 + room;
	const free = (x: number, y: number, room: number) =>
		!bySpring(x, y, room) && things.every((t) => Math.hypot(t.x - x, (t.y - y) * 1.6) > Math.max(room, ROOM[t.kind]));

	// Woods: thickest towards the edge of the grass, with a few trees wandering inwards.
	const trees: ArtName[] = ['TreePine', 'TreePine', 'TreeApple', 'TreeBushy', 'TreeBushy', 'TreeMaple'];
	for (let i = 0, placed = 0; i < 4000 && placed < 34; i++) {
		const x = (random(`tx${i}`) - 0.5) * 42;
		const y = (random(`ty${i}`) - 0.5) * 22;
		const edge = Math.hypot(x / 20, y / 10);
		if (edge < 0.62 && random(`tk${i}`) > 0.12) continue;
		if (!onGrass(x, y - 0.4, 0.7) || !free(x, y, ROOM.tree)) continue;
		const art = edge > 0.62 ? trees[Math.floor(random(`ta${i}`) * trees.length)] : 'TreeBushy';
		things.push({art, x, y, scale: 1.55 + random(`ts${i}`) * 0.35, flip: random(`tf${i}`) < 0.5, kind: 'tree'});
		placed++;
	}
	for (let i = 0, placed = 0; i < 2000 && placed < 12; i++) {
		const x = (random(`bx${i}`) - 0.5) * 38;
		const y = (random(`by${i}`) - 0.5) * 20;
		if (!onGrass(x, y, 0.5) || !free(x, y, 0.9)) continue;
		things.push({art: 'TinyBush', x, y, scale: 0.9, kind: 'tree'});
		placed++;
	}
	// The little things in the grass, the way the game dresses its land.
	const bits: ArtName[] = ['Tuft', 'Tuft', 'Wildflowers', 'Wildflowers', 'Pebbles'];
	for (let i = 0, placed = 0; i < 4000 && placed < 120; i++) {
		const x = (random(`dx${i}`) - 0.5) * 40;
		const y = (random(`dy${i}`) - 0.5) * 21;
		if (!onGrass(x, y, 0.3) || !free(x, y, 0.45)) continue;
		const art = bits[Math.floor(random(`da${i}`) * bits.length)];
		things.push({art, x, y, scale: 0.6 + random(`ds${i}`) * 0.35, flip: random(`df${i}`) < 0.5, kind: 'deco'});
		placed++;
	}
	return things;
};

export const THINGS: Thing[] = scatter().sort((a, b) => a.y - b.y);

export const ALL_ART: ArtName[] = Object.keys(ART_SIZE) as ArtName[];
