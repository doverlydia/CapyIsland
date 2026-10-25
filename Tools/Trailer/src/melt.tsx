import React, {useEffect, useState} from 'react';
import {continueRender, delayRender, interpolate, random, staticFile} from 'remotion';
import {noise3D} from '@remotion/noise';
import {loadFont} from '@remotion/fonts';
import {ALL_ART, POOL} from './layout';
import {art, Burst, WorldFilters} from './World';
import {blob} from './shapes';

/**
 * The Big Chill ending: a bubble of warm air pops on the frozen spring and the warmth rolls out
 * across the island. Shared by the snow-melt test and the trailer, each with its own timing.
 * Everything here is in the game's world units, except the snowfall, which is on the screen.
 */

export const clamp = {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'} as const;

/** The melt is round in the 3/4 view: squashed top to bottom like the island. */
const SQUASH = 1.8;
const SOURCE = {x: POOL.x, y: POOL.y};
const WOBBLE = 0.11;

export type Melt = ReturnType<typeof makeMelt>;

/** `pop`: the frame the bubble bursts. `spread`: frames until the warmth is off the edge of the picture. */
export const makeMelt = (pop: number, spread: number) => {
	/**
	 * How far the warmth has spread from the spring, in squashed world units: a steady roll across
	 * the island, so you can watch it go, then a quick sweep out over the sea.
	 */
	const front = (fr: number) => interpolate(fr, [pop, pop + 10, pop + Math.round(spread * 0.74), pop + spread], [0, 3, 25, 47], clamp);

	/** The frame the warmth reaches (x, y). */
	const meltFrame = (x: number, y: number) => {
		const d = Math.hypot(x - SOURCE.x, (y - SOURCE.y) * SQUASH) + 0.8;
		for (let fr = pop; fr <= pop + spread; fr++) if (front(fr) >= d) return fr;
		return Infinity;
	};

	/** The edge of the warmth: a lumpy ellipse, the same for the mask, the glow and the sparkles. */
	const shape = (fr: number) => blob(SOURCE.x, SOURCE.y, front(fr), front(fr) / SQUASH, 'front', WOBBLE, 56, fr * 0.02);
	const point = (fr: number, a: number): [number, number] => {
		const k = 1 + WOBBLE * noise3D('front', Math.cos(a) * 1.3, Math.sin(a) * 1.3, fr * 0.02);
		return [SOURCE.x + Math.cos(a) * front(fr) * k, SOURCE.y + (Math.sin(a) * front(fr) * k) / SQUASH];
	};

	/** 0 while frozen, 1 once the warmth has gone everywhere. */
	const warmth = (fr: number) => interpolate(fr, [pop, pop + spread], [0, 1], clamp);

	/** Hides the frozen island wherever the warmth has reached. Use as mask="url(#frozen)". */
	const Mask: React.FC<{fr: number}> = ({fr}) => (
		<mask id="frozen" maskUnits="userSpaceOnUse" x={-80} y={-60} width={160} height={120}>
			<rect x={-80} y={-60} width={160} height={120} fill="#FFFFFF" />
			{fr > pop && <path d={shape(fr)} fill="#000000" filter="url(#softEdge)" />}
		</mask>
	);

	/** The warm glow riding the edge of the melt, with the game's sparkles on it. */
	const Front: React.FC<{fr: number}> = ({fr}) => {
		if (fr < pop) return null;
		const glow = interpolate((fr - pop) / spread, [0, 0.06, 0.7, 1], [0, 1, 0.55, 0], clamp);
		if (glow <= 0) return null;
		const stars: React.ReactNode[] = [];
		for (let i = 0; i < 56; i++) {
			const a = (i / 56) * Math.PI * 2 + random(`sa${i}`) * 0.1;
			const [x, y] = point(fr, a);
			const twinkle = 0.5 + 0.5 * Math.sin(fr * 0.6 + i * 2.1);
			const s = 0.7 + random(`sr${i}`) * 0.7;
			stars.push(<image key={i} href={art('Sparkle')} x={x - s / 2} y={y - s / 2 - 0.2} width={s} height={s} opacity={glow * twinkle} />);
		}
		return (
			<g>
				<path d={shape(fr)} fill="none" stroke="#FFE9AE" strokeWidth={1.0} opacity={glow * 0.8} filter="url(#blurWide)" />
				<path d={shape(fr)} fill="none" stroke="#FFFBEA" strokeWidth={0.18} opacity={glow * 0.7} filter="url(#blurThin)" />
				{stars}
			</g>
		);
	};

	/** The bubble of warm air Old Yuzu sealed, sitting on the frozen spring, until it pops. */
	const Bubble: React.FC<{fr: number}> = ({fr}) => {
		const x = POOL.x;
		const y = POOL.y - 0.9;
		const r = 1.2;
		if (fr < pop) {
			const bob = Math.sin(fr * 0.13) * 0.08;
			const nerves = interpolate(fr, [pop - 20, pop], [0, 1], clamp);
			const wob = Math.sin(fr * 1.3) * 0.07 * nerves;
			const swell = interpolate(fr, [pop - 5, pop], [1, 1.14], clamp);
			const pulse = 0.55 + 0.25 * Math.sin(fr * 0.22) + 0.2 * nerves;
			return (
				<g transform={`translate(${x} ${y + bob}) scale(${(1 + wob) * swell} ${(1 - wob) * swell})`}>
					<circle r={r * 2} fill="url(#bubbleHalo)" opacity={pulse * 0.8} />
					<circle r={r * 0.62} fill="url(#bubbleWarm)" opacity={pulse} />
					<image href={art('Bubble')} x={-r} y={-r} width={r * 2} height={r * 2} />
				</g>
			);
		}
		const t = fr - pop;
		if (t > 30) return null;
		return (
			<g>
				<Burst x={x} y={y} r={r} t={t} seed="spring" />
				<circle cx={x} cy={y} r={3.4} fill="url(#flash)" opacity={interpolate(t, [0, 12], [1, 0], clamp)} />
			</g>
		);
	};

	return {pop, spread, front, meltFrame, warmth, Mask, Front, Bubble};
};

/** Snow coming down while it's cold; it thins out and stops as the warmth spreads. */
export const Snowfall: React.FC<{fr: number; melt: Melt}> = ({fr, melt}) => {
	const fade = interpolate(fr, [melt.pop, melt.pop + melt.spread * 0.55], [1, 0], clamp);
	if (fade <= 0) return null;
	const flakes: React.ReactNode[] = [];
	for (let i = 0; i < 170; i++) {
		const near = random(`fn${i}`) < 0.15;
		const speed = near ? 5 + random(`fv${i}`) * 2 : 1.5 + random(`fv${i}`) * 2;
		const size = near ? 6 + random(`fs${i}`) * 4 : 2 + random(`fs${i}`) * 3;
		const y = ((random(`fy${i}`) * 1200 + fr * speed) % 1200) - 60;
		const x = random(`fx${i}`) * 2100 - 90 + fr * speed * 0.35 + Math.sin(fr * 0.05 + i) * 14;
		flakes.push(<circle key={i} cx={x} cy={y} r={size} fill="#FFFFFF" opacity={(near ? 0.55 : 0.85) * fade} filter={near ? 'url(#blur4)' : undefined} />);
	}
	return (
		<svg width={1920} height={1080} style={{position: 'absolute'}}>
			<defs>
				<filter id="blur4" x="-50%" y="-50%" width="200%" height="200%">
					<feGaussianBlur stdDeviation={4} />
				</filter>
			</defs>
			{flakes}
		</svg>
	);
};

/** Filters and gradients the melt and the world use; put once inside the main <svg>. */
export const MeltDefs: React.FC = () => (
	<defs>
		<WorldFilters />
		<filter id="blurThin" filterUnits="userSpaceOnUse" x={-80} y={-60} width={160} height={120}>
			<feGaussianBlur stdDeviation={0.12} />
		</filter>
		<filter id="blurWide" filterUnits="userSpaceOnUse" x={-80} y={-60} width={160} height={120}>
			<feGaussianBlur stdDeviation={0.4} />
		</filter>
		<filter id="softEdge" filterUnits="userSpaceOnUse" x={-80} y={-60} width={160} height={120}>
			<feGaussianBlur stdDeviation={0.45} />
		</filter>
		<radialGradient id="bubbleWarm">
			<stop offset="0" stopColor="#FFF3C4" />
			<stop offset="0.5" stopColor="#FFC766" stopOpacity={0.8} />
			<stop offset="1" stopColor="#FFB24D" stopOpacity={0} />
		</radialGradient>
		<radialGradient id="bubbleHalo">
			<stop offset="0" stopColor="#FFE3A0" stopOpacity={0.55} />
			<stop offset="1" stopColor="#FFE3A0" stopOpacity={0} />
		</radialGradient>
		<radialGradient id="flash">
			<stop offset="0" stopColor="#FFFFFF" />
			<stop offset="0.4" stopColor="#FFF0C2" stopOpacity={0.8} />
			<stop offset="1" stopColor="#FFE3A0" stopOpacity={0} />
		</radialGradient>
		<radialGradient id="coldVignette" cx="0.5" cy="0.5" r="0.75">
			<stop offset="0.5" stopColor="#1C2B42" stopOpacity={0} />
			<stop offset="1" stopColor="#1C2B42" stopOpacity={0.55} />
		</radialGradient>
		<radialGradient id="sunWash" cx="0.15" cy="0.05" r="0.9">
			<stop offset="0" stopColor="#FFE9A8" stopOpacity={0.4} />
			<stop offset="1" stopColor="#FFE9A8" stopOpacity={0} />
		</radialGradient>
	</defs>
);

/** Holds the render until every sprite and font has loaded, so no frame is drawn with one missing. */
export const useAssets = () => {
	const [handle] = useState(() => delayRender('Loading the game sprites and fonts'));
	useEffect(() => {
		Promise.all([
			...ALL_ART.map((a) => {
				const img = new Image();
				img.src = art(a);
				return img.decode();
			}),
			loadFont({family: 'Fredoka', url: staticFile('fonts/Fredoka-SemiBold.ttf'), weight: '600'}),
			loadFont({family: 'Nunito', url: staticFile('fonts/Nunito-Bold.ttf'), weight: '700'}),
		]).then(() => continueRender(handle));
	}, [handle]);
};
