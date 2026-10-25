import React from 'react';
import {AbsoluteFill, Img, interpolate, random, spring} from 'remotion';
import {ArtName} from './art.gen';
import {Caption} from './Caption';
import {art} from './World';

/**
 * Quick cuts of the hobbies, one beat-pair each: the game's own vignette of a capybara at it, the
 * hobby's things popping in around it, and a word or two to say what it is.
 */

type Hobby = {
	vignette: ArtName;
	label: string;
	/** Inner and outer colour of the backdrop. */
	bg: [string, string];
	props: ArtName[];
	/** Fish jump in arcs; everything else pops into place. */
	leap?: boolean;
};

export const HOBBIES: Hobby[] = [
	{vignette: 'VigFishing', label: 'Go fishing', bg: ['#D8F3F8', '#6FC0D8'], props: ['FishSalmon', 'FishClown', 'FishPuffer', 'FishRay'], leap: true},
	{vignette: 'VigCooking', label: 'Cook tasty dishes', bg: ['#FFF0D2', '#F4AE62'], props: ['DishOnigiri', 'DishPancakes', 'DishHotPot', 'DishMochi']},
	{vignette: 'VigCats', label: 'Adopt cats', bg: ['#FFEDE4', '#EF9E8E'], props: ['CatGinger', 'CatCalico', 'CatBlack', 'CatWhite']},
	{vignette: 'VigSewing', label: 'Design your own clothes', bg: ['#F3EAFB', '#B99ADF'], props: ['SewingMachine', 'MisoDress', 'Mannequin']},
];

/** Where the props sit around the vignette (centre of each), and how big. */
const SLOTS: [number, number, number][] = [
	[420, 330, 250],
	[1500, 330, 250],
	[540, 690, 220],
	[1380, 690, 220],
];

/** Soft sun rays turning slowly behind everything. */
const Rays: React.FC<{t: number}> = ({t}) => (
	<svg width={1920} height={1080} style={{position: 'absolute'}}>
		<g transform={`translate(960 460) rotate(${t * 0.6})`}>
			{Array.from({length: 14}, (_, i) => (
				<path key={i} d="M0,0 L-120,-1400 L120,-1400 Z" fill="#FFFFFF" opacity={0.13} transform={`rotate(${(i * 360) / 14})`} />
			))}
		</g>
	</svg>
);

const Prop: React.FC<{a: ArtName; slot: number; t: number; leap?: boolean}> = ({a, slot, t, leap}) => {
	const [cx, cy, size] = SLOTS[slot % SLOTS.length];
	if (leap) {
		// up out of the water and back down, crossing in front of the camera
		const start = slot * 4;
		const p = (t - start) / 30;
		if (p < 0 || p > 1.2) return null;
		const fromLeft = slot % 2 === 0;
		const x = fromLeft ? interpolate(p, [0, 1], [260, 860]) : interpolate(p, [0, 1], [1660, 1060]);
		const y = 1100 - Math.sin(Math.min(1, p) * Math.PI) * (700 - (slot >> 1) * 180) + (p > 1 ? (p - 1) * 900 : 0);
		const rot = (fromLeft ? 1 : -1) * interpolate(p, [0, 1], [-50, 60]);
		return (
			<Img
				src={art(a)}
				style={{position: 'absolute', left: x - size / 2, top: y - size / 2, width: size, height: size, transform: `rotate(${rot}deg) scaleX(${fromLeft ? -1 : 1})`}}
			/>
		);
	}
	const pop = spring({frame: t - 2 - slot * 3, fps: 30, config: {damping: 9, stiffness: 190}});
	const bob = Math.sin(t * 0.18 + slot * 1.7) * 8;
	const tilt = Math.sin(t * 0.12 + slot) * 5;
	return (
		<Img
			src={art(a)}
			style={{
				position: 'absolute',
				left: cx - size / 2,
				top: cy - size / 2 + bob,
				width: size,
				height: size,
				transform: `scale(${pop}) rotate(${tilt}deg)`,
				filter: 'drop-shadow(0 10px 14px rgba(60, 40, 20, 0.25))',
			}}
		/>
	);
};

/** One hobby's cut. `t` counts from the cut's first frame. It opens with an iris over the shot before. */
export const HobbyCut: React.FC<{hobby: Hobby; t: number}> = ({hobby, t}) => {
	const iris = interpolate(t, [0, 7], [0, 1300], {extrapolateRight: 'clamp'});
	const pop = spring({frame: t, fps: 30, config: {damping: 12, stiffness: 160}});
	const push = 1 + t * 0.0015;
	return (
		<AbsoluteFill style={{clipPath: `circle(${iris}px at 50% 45%)`, background: `radial-gradient(circle at 50% 42%, ${hobby.bg[0]} 0%, ${hobby.bg[1]} 85%)`}}>
			<Rays t={t} />
			{hobby.props.map((a, i) => (
				<Prop key={i} a={a} slot={i} t={t} leap={hobby.leap} />
			))}
			<Img
				src={art(hobby.vignette)}
				style={{
					position: 'absolute',
					left: 960 - 330,
					top: 440 - 330,
					width: 660,
					height: 660,
					transform: `scale(${(0.7 + 0.3 * pop) * push}) rotate(${(1 - pop) * -6 + Math.sin(t * 0.2) * 1.2}deg)`,
					filter: 'drop-shadow(0 16px 20px rgba(60, 40, 20, 0.3))',
				}}
			/>
			<Caption text={hobby.label} fr={t} from={2} to={1000} size={84} y={952} stagger={1.5} />
		</AbsoluteFill>
	);
};

/** Little sparkles for a card, using the game's sparkle sprite. */
export const Twinkles: React.FC<{t: number; count: number; seed: string}> = ({t, count, seed}) => (
	<>
		{Array.from({length: count}, (_, i) => {
			const x = random(`${seed}x${i}`) * 1920;
			const y = random(`${seed}y${i}`) * 1080;
			const s = 30 + random(`${seed}s${i}`) * 40;
			const o = Math.max(0, Math.sin(t * 0.25 + i * 2.3));
			return <Img key={i} src={art('Sparkle')} style={{position: 'absolute', left: x - s / 2, top: y - s / 2, width: s, height: s, opacity: o}} />;
		})}
	</>
);
