import React from 'react';
import {interpolate, random, spring, staticFile} from 'remotion';
import {noise3D} from '@remotion/noise';
import {ArtName} from './art.gen';
import {LAND, POOL, THINGS, TOWER, Thing, size} from './layout';

/**
 * Capy Island from the game's own sprites, drawn twice: warm, and frozen under the Big Chill.
 * The frozen one is the same art run through the filters in WorldFilters (cooler colours, snow
 * settling on every top edge), plus the tower's own cold sprite. The movie lays the frozen island
 * over the warm one and melts a hole in it.
 */

export const art = (a: ArtName) => staticFile(`art/${a}.png`);

const clamp = {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'} as const;

type Life = {
	cold: boolean;
	frame: number;
	/** Frames since the warmth reached (x, y); negative before it does. */
	since: (x: number, y: number) => number;
	/**
	 * When given, the island starts out the way the game starts you: buildings, gardens and
	 * capybaras each sealed in a bubble. Frames since thing `i`'s bubble popped; negative before.
	 */
	popped?: (i: number) => number;
};

/** What waits in a bubble on a new island (the hills and the woods are just there). */
export const inBubble = (t: Thing) => (t.kind === 'building' && t.art !== 'HillGray') || t.kind === 'garden' || t.kind === 'capy';

/** The game's bubble: 2.4 units across, floating a little above where its contents will stand. */
const BUBBLE_R = 1.2;
const bubbleAt = (t: Thing) => ({x: t.x, y: t.y - 1.45});

/** A little bounce when the warmth arrives, as if shaking off the cold. */
const boing = (t: number) => (t < 0 ? 1 : 1 + 0.06 * Math.sin(t * 0.55) * Math.exp(-t * 0.07));

const Sprite: React.FC<{a: ArtName; x: number; y: number; w: number; h: number; flip?: boolean; filter?: string}> = ({
	a,
	x,
	y,
	w,
	h,
	flip,
	filter,
}) => (
	<image
		href={art(a)}
		x={x - w / 2}
		y={y - h}
		width={w}
		height={h}
		preserveAspectRatio="none"
		filter={filter}
		transform={flip ? `translate(${2 * x} 0) scale(-1 1)` : undefined}
	/>
);

const about = (x: number, y: number, sx: number, sy: number) => `translate(${x} ${y}) scale(${sx} ${sy}) translate(${-x} ${-y})`;

const ThingView: React.FC<{t: Thing; i: number; life: Life}> = ({t, i, life}) => {
	const [w, h] = size(t);
	const {cold, frame, since, popped} = life;
	if (popped && inBubble(t)) {
		// Out of its bubble with a bounce, the way Bubble.cs lets a character out.
		const s = popped(i);
		if (cold || s < 0) return null;
		const pop = spring({frame: s, fps: 30, config: {damping: 9, stiffness: 170}});
		const k = 0.55 + 0.45 * pop;
		const hop = t.kind === 'capy' ? -Math.abs(Math.sin(frame * 0.16 + i)) * 0.18 : 0;
		return (
			<g transform={about(t.x, t.y, k, k)} opacity={Math.min(1, pop * 2)}>
				<Sprite a={t.art} x={t.x} y={t.y + hop} w={w} h={h} flip={t.flip} />
			</g>
		);
	}
	if (cold) {
		if (t.kind === 'capy' || t.kind === 'deco') return null;
		const filter = t.kind === 'king' ? 'url(#coldGrade)' : 'url(#snowify)';
		return <Sprite a={t.art} x={t.x} y={t.y} w={w} h={h} flip={t.flip} filter={filter} />;
	}
	const s = since(t.x, t.y);
	if (t.kind === 'capy' || t.kind === 'deco') {
		// They come back once it's warm: a pop, and the capybaras keep hopping about.
		const pop = spring({frame: s - 4 - (i % 5) * 2, fps: 30, config: {damping: 10, stiffness: 160}});
		if (pop < 0.01) return null;
		const hop = t.kind === 'capy' ? -Math.abs(Math.sin(frame * 0.16 + i)) * 0.18 : 0;
		return (
			<g transform={about(t.x, t.y, pop, pop)}>
				<Sprite a={t.art} x={t.x} y={t.y + hop} w={w} h={h} flip={t.flip} />
			</g>
		);
	}
	const b = boing(s);
	return (
		<g transform={about(t.x, t.y, 2 - b, b)}>
			<Sprite a={t.art} x={t.x} y={t.y} w={w} h={h} flip={t.flip} />
		</g>
	);
};

/**
 * A bubble on the island with its contents drawn inside the glass, as in Bubble.cs. Frosted over
 * while it's cold; bursts when its time comes.
 */
const BubbleView: React.FC<{t: Thing; i: number; life: Life}> = ({t, i, life}) => {
	const s = life.popped ? life.popped(i) : -1;
	const {x, y} = bubbleAt(t);
	if (!life.cold && s >= 0) return <Burst x={x} y={y} r={BUBBLE_R} t={s} seed={`b${i}`} grow={0.09} />;
	const [w, h] = size(t);
	const fit = 1.6 / Math.max(w, h);
	const bob = life.cold ? 0 : Math.sin(life.frame * 0.1 + i * 1.3) * 0.09;
	const wobble = !life.cold && s > -8 ? Math.sin(life.frame * 1.4) * 0.06 : 0;
	return (
		<g transform={`translate(${x} ${y + bob}) scale(${1 + wobble} ${1 - wobble})`} filter={life.cold ? 'url(#snowify)' : undefined}>
			<image href={art(t.art)} x={(-w * fit) / 2} y={(-h * fit) / 2} width={w * fit} height={h * fit} preserveAspectRatio="none" />
			<image href={art('Bubble')} x={-BUBBLE_R} y={-BUBBLE_R} width={BUBBLE_R * 2} height={BUBBLE_R * 2} />
		</g>
	);
};

/** A bubble bursting: a ring flying out and droplets falling away. */
/** `grow`: how fast the ring flies out, in units per frame. */
export const Burst: React.FC<{x: number; y: number; r: number; t: number; seed: string; grow?: number}> = ({x, y, r, t, seed, grow = 0.45}) => {
	if (t < 0 || t > 24) return null;
	const k = r / 1.2;
	const drops: React.ReactNode[] = [];
	for (let i = 0; i < 16; i++) {
		const a = (i / 16) * Math.PI * 2 + random(`${seed}a${i}`) * 0.3;
		const v = (0.14 + random(`${seed}v${i}`) * 0.14) * k;
		const dx = Math.cos(a) * v * t;
		const dy = Math.sin(a) * v * t * 0.8 + 0.007 * k * t * t;
		drops.push(
			<circle
				key={i}
				cx={x + dx}
				cy={y + dy}
				r={Math.max(0.02, (0.12 - t * 0.004) * k)}
				fill="#EFFCFF"
				stroke="#BDE6F2"
				strokeWidth={0.03}
				opacity={interpolate(t, [0, 22], [1, 0], clamp)}
			/>,
		);
	}
	return (
		<g>
			<circle cx={x} cy={y} r={r + t * grow * k} fill="none" stroke="#FFFFFF" strokeWidth={Math.max(0, (0.18 - t * 0.012) * k)} opacity={interpolate(t, [0, 14], [1, 0], clamp)} />
			{drops}
		</g>
	);
};

/** The Steam Tower and the hot spring at its foot. */
const Spring: React.FC<{life: Life}> = ({life}) => {
	const {cold, since} = life;
	const tw = 2 * TOWER.scale;
	const b = cold ? 1 : boing(since(TOWER.x, TOWER.y));
	return (
		<g>
			<g transform={about(TOWER.x, TOWER.y, 2 - b, b)}>
				<Sprite a={cold ? 'TowerCold' : 'Tower'} x={TOWER.x} y={TOWER.y} w={tw} h={tw} filter={cold ? 'url(#coldGrade)' : undefined} />
			</g>
			<image
				href={art('HotSpring')}
				x={POOL.x - POOL.w / 2}
				y={POOL.y - POOL.h / 2}
				width={POOL.w}
				height={POOL.h}
				preserveAspectRatio="none"
				filter={cold ? 'url(#snowify)' : undefined}
			/>
			{cold && (
				// the water frozen over
				<ellipse cx={POOL.x - 0.05} cy={POOL.y + 0.12} rx={POOL.w * 0.33} ry={POOL.h * 0.24} fill="#DCEAF2" opacity={0.85} />
			)}
		</g>
	);
};

/** Steam off the spring, once it's warm again. */
const Steam: React.FC<{life: Life}> = ({life}) => {
	const {frame, since} = life;
	const start = frame - since(POOL.x, POOL.y) + 6;
	const every = 5;
	const lifeSpan = 70;
	const puffs: React.ReactNode[] = [];
	for (let born = start; born <= frame; born += every) {
		const age = frame - born;
		if (age > lifeSpan) continue;
		const n = born * 0.37;
		const x = POOL.x + noise3D('steam', n, 0, 0) * POOL.w * 0.32 + Math.sin(age * 0.07 + n) * 0.2;
		const y = POOL.y - 0.1 - age * 0.045;
		const r = 0.26 + age * 0.009;
		puffs.push(<circle key={born} cx={x} cy={y} r={r} fill="#FFFFFF" opacity={Math.sin((Math.PI * age) / lifeSpan) * 0.32} />);
	}
	return <g filter="url(#steamBlur)">{puffs}</g>;
};

const Sea: React.FC<{life: Life}> = ({life}) => {
	const {cold, frame} = life;
	const waves: React.ReactNode[] = [];
	for (let i = 0; i < 90; i++) {
		const x = (random(`wx${i}`) - 0.5) * 70;
		const y = (random(`wy${i}`) - 0.5) * 40;
		if ((x / 27) ** 2 + (y / 15) ** 2 < 1) continue;
		const X = x + (cold ? 0 : Math.sin(frame * 0.05 + i) * 0.25);
		const Y = y + (cold ? 0 : Math.sin(frame * 0.08 + i * 1.7) * 0.08);
		waves.push(
			<path
				key={i}
				d={`M${X - 0.6},${Y} q0.3,-0.25 0.6,0 q0.3,-0.25 0.6,0`}
				fill="none"
				stroke={cold ? '#90A6B9' : '#BFE7F1'}
				strokeWidth={0.12}
				strokeLinecap="round"
				opacity={cold ? 0.4 : 0.7}
			/>,
		);
	}
	return (
		<>
			<rect x={-60} y={-40} width={120} height={80} fill={cold ? '#5F7B93' : '#8FCCE0'} />
			{waves}
		</>
	);
};

const Land: React.FC<{life: Life}> = ({life}) => {
	const {cold} = life;
	const box = {x: -LAND.w / 2, y: -LAND.h / 2, width: LAND.w, height: LAND.h, preserveAspectRatio: 'none'};
	return (
		<>
			{cold && (
				// the sea frozen in a shelf of ice around the shore
				<g transform={about(0, 0.3, 1.13, 1.18)}>
					<image href={art('Sand')} {...box} filter="url(#iceShelf)" />
					<image href={art('Shore')} {...box} />
				</g>
			)}
			<image href={art('Shore')} {...box} />
			<image href={art('Sand')} {...box} filter={cold ? 'url(#snowSand)' : undefined} />
			<image href={art('Grass')} {...box} filter={cold ? 'url(#snowGround)' : undefined} />
		</>
	);
};

export const World: React.FC<Life> = (life) => {
	const items: {y: number; el: React.ReactNode}[] = THINGS.map((t, i) => ({y: t.y, el: <ThingView key={i} t={t} i={i} life={life} />}));
	items.push({y: TOWER.y, el: <Spring key="spring" life={life} />});
	items.sort((a, b) => a.y - b.y);
	return (
		<g>
			<Sea life={life} />
			<Land life={life} />
			{items.map((it) => it.el)}
			{!life.cold && <Steam life={life} />}
			{/* bubbles float over everything, nearer ones in front (Bubble.cs) */}
			{life.popped && THINGS.map((t, i) => (inBubble(t) ? <BubbleView key={`bubble${i}`} t={t} i={i} life={life} /> : null))}
		</g>
	);
};

/**
 * How winter is drawn over the game's art. Lengths are in world units (the filters work in the
 * space of the sprite using them), so snow is equally deep on everything.
 */
export const WorldFilters: React.FC = () => (
	<>
		<filter id="coldGrade" colorInterpolationFilters="sRGB">
			<feColorMatrix type="matrix" values="0.476 0.290 0.054 0 0.06  0.162 0.679 0.059 0 0.07  0.189 0.372 0.489 0 0.10  0 0 0 1 0" />
		</filter>
		<filter id="snowify" x="-10%" y="-10%" width="120%" height="120%" colorInterpolationFilters="sRGB">
			<feColorMatrix
				in="SourceGraphic"
				type="matrix"
				values="0.476 0.290 0.054 0 0.06  0.162 0.679 0.059 0 0.07  0.189 0.372 0.489 0 0.10  0 0 0 1 0"
				result="cold"
			/>
			{/* snow settles wherever there's nothing just above: roofs, treetops, rims */}
			<feOffset in="SourceAlpha" dy={0.3} result="below" />
			<feComposite in="SourceAlpha" in2="below" operator="out" result="tops" />
			<feTurbulence type="fractalNoise" baseFrequency={2.4} numOctaves={2} seed={7} result="noise" />
			<feDisplacementMap in="tops" in2="noise" scale={0.32} xChannelSelector="R" yChannelSelector="G" result="lumpy" />
			<feGaussianBlur in="lumpy" stdDeviation={0.035} result="soft" />
			<feComponentTransfer in="soft" result="firm">
				<feFuncA type="linear" slope={2.6} intercept={-0.45} />
			</feComponentTransfer>
			<feComposite in="firm" in2="SourceAlpha" operator="in" result="snowMask" />
			<feOffset in="snowMask" dy={0.07} result="shadeMask0" />
			<feComposite in="shadeMask0" in2="SourceAlpha" operator="in" result="shadeMask" />
			<feFlood floodColor="#A9BFD3" result="blue" />
			<feComposite in="blue" in2="shadeMask" operator="in" result="shade" />
			<feFlood floodColor="#F7FBFF" result="white" />
			<feComposite in="white" in2="snowMask" operator="in" result="snow" />
			<feMerge>
				<feMergeNode in="cold" />
				<feMergeNode in="shade" />
				<feMergeNode in="snow" />
			</feMerge>
		</filter>
		<filter id="snowGround" colorInterpolationFilters="sRGB">
			<feColorMatrix type="matrix" values="0.07 0.13 0.02 0 0.76  0.07 0.13 0.02 0 0.80  0.07 0.13 0.02 0 0.86  0 0 0 1 0" />
		</filter>
		<filter id="snowSand" colorInterpolationFilters="sRGB">
			<feColorMatrix type="matrix" values="0.07 0.13 0.02 0 0.60  0.07 0.13 0.02 0 0.66  0.07 0.13 0.02 0 0.75  0 0 0 1 0" />
		</filter>
		<filter id="iceShelf" colorInterpolationFilters="sRGB">
			<feFlood floodColor="#CFDDE8" result="ice" />
			<feComposite in="ice" in2="SourceAlpha" operator="in" />
		</filter>
		{/* the game's petal is white, for tinting */}
		<filter id="petalPink" colorInterpolationFilters="sRGB">
			<feColorMatrix type="matrix" values="0.97 0 0 0 0  0 0.66 0 0 0  0 0 0.78 0 0  0 0 0 1 0" />
		</filter>
		<filter id="steamBlur" x="-50%" y="-50%" width="200%" height="200%">
			<feGaussianBlur stdDeviation={0.12} />
		</filter>
	</>
);
