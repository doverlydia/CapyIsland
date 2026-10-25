// Copies the game's own assets that the trailer uses into public/ (sprites into public/art,
// music and sounds into public/audio, fonts into public/fonts), and writes src/art.gen.ts: each
// sprite's pixel size, plus a coarse map of where Capy Island's grass is, so trees and flowers
// are only ever placed on land.
//
// Usage (from Tools/Trailer): node scripts/import-assets.mjs

import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {decode} from '../../ChatGPT/pngio.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const trailer = path.resolve(here, '..');
const repo = path.resolve(trailer, '../..');
const out = path.join(trailer, 'public/art');

const ART = {
	Shore: 'Assets/Art/Terrain/Land/Land_00_CapyIsland_Shore.png',
	Sand: 'Assets/Art/Terrain/Land/Land_00_CapyIsland_Sand.png',
	Grass: 'Assets/Art/Terrain/Land/Land_00_CapyIsland_Grass.png',
	HotSpring: 'Assets/Resources/World/HotSpring.png',
	Tower: 'Assets/Art/Buildings/Build_Tower.png',
	TowerCold: 'Assets/Resources/World/Build_Tower_Cold.png',
	Toro: 'Assets/Resources/World/Toro.png',
	Bubble: 'Assets/Resources/World/Bubble.png',
	Sparkle: 'Assets/Art/FX/FX_Sparkle.png',
	Petal: 'Assets/Art/FX/FX_Petal.png',
	StoreGeneral: 'Assets/Art/Buildings/Store_General.png',
	StoreBronze: 'Assets/Art/Buildings/Store_Bronze.png',
	StoreSilver: 'Assets/Art/Buildings/Store_Silver.png',
	CapyHouse: 'Assets/Art/Buildings/Build_CapyHouse.png',
	Sawmill: 'Assets/Art/Buildings/Build_Sawmill.png',
	Mine: 'Assets/Art/Buildings/Build_Mine.png',
	Fountain: 'Assets/Art/Props/Deco_Fountain.png',
	TreeMaple: 'Assets/Art/Terrain/Tree_Maple.png',
	TreePine: 'Assets/Art/Terrain/Tree_Pine.png',
	TreeApple: 'Assets/Art/Terrain/Tree_Apple.png',
	TreePink: 'Assets/Art/Props/Deco_PinkFlowerTree.png',
	TreeWhite: 'Assets/Art/Props/Deco_WhiteFlowerTree.png',
	TreeBushy: 'Assets/Art/Props/Deco_BushyTree.png',
	TinyBush: 'Assets/Art/Props/Deco_TinyBush.png',
	HillGray: 'Assets/Art/Terrain/Hill_Gray.png',
	GardenTulip: 'Assets/Art/Terrain/Garden_Tulip.png',
	GardenRose: 'Assets/Art/Terrain/Garden_Rose.png',
	GardenPosie: 'Assets/Art/Terrain/Garden_Posie.png',
	CapyIdle: 'Assets/Art/Capybaras/Capy_Idle.png',
	CapyKing: 'Assets/Art/Capybaras/Capy_King.png',
	CapyMiso: 'Assets/Art/Capybaras/Capy_Coriander.png',
	Tuft: 'Assets/Art/Terrain/Deco_GrassTuft.png',
	Wildflowers: 'Assets/Art/Terrain/Deco_Wildflowers.png',
	Pebbles: 'Assets/Art/Terrain/Deco_Pebbles.png',

	Icon: 'Assets/Art/Icon/Icon_Full.png',
	VigFishing: 'Assets/Resources/Hobbies/Vignette_Fishing.png',
	VigCooking: 'Assets/Resources/Hobbies/Vignette_Cooking.png',
	VigCats: 'Assets/Resources/Hobbies/Vignette_Cats.png',
	VigSewing: 'Assets/Resources/Hobbies/Vignette_Sewing.png',
	FishSalmon: 'Assets/Resources/Hobbies/Fish_salmon.png',
	FishClown: 'Assets/Resources/Hobbies/Fish_clownfish.png',
	FishPuffer: 'Assets/Resources/Hobbies/Fish_puffer.png',
	FishRay: 'Assets/Resources/Hobbies/Fish_yuzuray.png',
	DishOnigiri: 'Assets/Resources/Hobbies/Dish_Onigiri.png',
	DishPancakes: 'Assets/Resources/Hobbies/Dish_Pancakes.png',
	DishHotPot: 'Assets/Resources/Hobbies/Dish_HotPot.png',
	DishMochi: 'Assets/Resources/Hobbies/Dish_StrawberryMochi.png',
	CatGinger: 'Assets/Resources/Hobbies/Cat_Ginger.png',
	CatCalico: 'Assets/Resources/Hobbies/Cat_Calico.png',
	CatBlack: 'Assets/Resources/Hobbies/Cat_Black.png',
	CatWhite: 'Assets/Resources/Hobbies/Cat_White.png',
	SewingMachine: 'Assets/Resources/Hobbies/SewingMachine.png',
	Mannequin: 'Assets/Resources/Hobbies/Sew_Mannequin.png',
	MisoDress: 'Assets/Resources/Hobbies/Miso_Dress.png',
	Hand: 'Assets/Art/Common/UI_PointerHand.png',
};

/** Music from Capy Music Studio, and our generated sounds (Tools/fx/sfx.mjs). */
const AUDIO = {
	'music_day.wav': 'Assets/Resources/Music/music_day.wav',
	'intro_chill.wav': 'Assets/Resources/Music/Intro/intro_chill.wav',
	'Intro_Bubble.wav': 'Assets/Resources/Sfx/Intro_Bubble.wav',
	'Intro_Magic.wav': 'Assets/Resources/Sfx/Intro_Magic.wav',
	'Intro_Sparkle.wav': 'Assets/Resources/Sfx/Intro_Sparkle.wav',
	'Intro_Crown.wav': 'Assets/Resources/Sfx/Intro_Crown.wav',
	'Amb_Chirp1.wav': 'Assets/Resources/Sfx/Amb_Chirp1.wav',
	'Amb_Chirp3.wav': 'Assets/Resources/Sfx/Amb_Chirp3.wav',
	'Sfx_Meow.wav': 'Assets/Resources/Sfx/Sfx_Meow.wav',
	'Sfx_Stir.wav': 'Assets/Resources/Sfx/Sfx_Stir.wav',
	'Sfx_Slide.wav': 'Assets/Resources/Sfx/Sfx_Slide.wav',
};

const FONTS = {
	'Fredoka-SemiBold.ttf': 'Assets/Resources/Fonts/Fredoka-SemiBold.ttf',
	'Nunito-Bold.ttf': 'Assets/Resources/Fonts/Nunito-Bold.ttf',
};

const copyAll = (files, dir) => {
	fs.mkdirSync(path.join(trailer, 'public', dir), {recursive: true});
	for (const [name, src] of Object.entries(files)) fs.copyFileSync(path.join(repo, src), path.join(trailer, 'public', dir, name));
};
copyAll(AUDIO, 'audio');
copyAll(FONTS, 'fonts');

fs.mkdirSync(out, {recursive: true});
const sizes = {};
for (const [name, src] of Object.entries(ART)) {
	const buf = fs.readFileSync(path.join(repo, src));
	fs.writeFileSync(path.join(out, `${name}.png`), buf);
	sizes[name] = [buf.readUInt32BE(16), buf.readUInt32BE(20)];
}

// The grass, sampled every half world unit (the land art is 10 px per unit): 1 where a cell is
// solidly grass all the way through.
const grass = decode(fs.readFileSync(path.join(repo, ART.Grass)));
const CELL = 5;
const rows = [];
for (let cy = 0; cy + CELL <= grass.h; cy += CELL) {
	let row = '';
	for (let cx = 0; cx + CELL <= grass.w; cx += CELL) {
		let solid = true;
		for (let y = cy; y < cy + CELL && solid; y++)
			for (let x = cx; x < cx + CELL && solid; x++) if (grass.data[(y * grass.w + x) * 4 + 3] < 230) solid = false;
		row += solid ? '1' : '0';
	}
	rows.push(row);
}

const ts = `// Generated by scripts/import-art.mjs. Do not edit.

export type ArtName = ${Object.keys(ART).map((n) => `'${n}'`).join(' | ')};

/** Each sprite's size in pixels. */
export const ART_SIZE: Record<ArtName, [number, number]> = ${JSON.stringify(sizes, null, '\t')};

/** Where the grass is: one character per half world unit, top row first ('1' = grass). */
export const GRASS_CELL = 0.5;
export const GRASS_MAP: string[] = ${JSON.stringify(rows, null, '\t')};
`;
fs.writeFileSync(path.join(trailer, 'src/art.gen.ts'), ts);
console.log(`Copied ${Object.keys(ART).length} sprites, ${Object.keys(AUDIO).length} sounds, ${Object.keys(FONTS).length} fonts; grass map ${rows[0].length}x${rows.length}.`);
