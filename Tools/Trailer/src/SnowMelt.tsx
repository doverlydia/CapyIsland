import React from 'react';
import {AbsoluteFill, Audio, Easing, interpolate, random, Sequence, staticFile, useCurrentFrame} from 'remotion';
import {clamp, makeMelt, MeltDefs, Snowfall, useAssets} from './melt';
import {POOL} from './layout';
import {art, World} from './World';
import {clamp01} from './shapes';

/**
 * Trailer test: Capy Island under the Big Chill, a bubble of warm air on the frozen spring, and —
 * pop — the warmth rolling out across the island until the snow is gone. Built from the game's
 * own sprites; everything is measured in the game's world units.
 */

export const SNOW_MELT_FRAMES = 150;

const melt = makeMelt(36, 95);
const {pop: POP, spread: SPREAD} = melt;

/** Starts close on the bubble, then pulls back to show the whole island as it warms. */
const camera = (fr: number) => {
	const out = interpolate(fr, [POP + 4, POP + 92], [0, 1], {...clamp, easing: Easing.inOut(Easing.cubic)});
	// how many world units fit across the screen
	const across = fr < POP ? interpolate(fr, [0, POP], [19, 17.5]) : interpolate(out, [0, 1], [17.5, 55]);
	const settle = interpolate(fr, [POP + 92, SNOW_MELT_FRAMES], [0, 1.2], clamp);
	const scale = 1920 / (across - settle);
	const cx = interpolate(out, [0, 1], [POOL.x, 0]);
	const cy = interpolate(out, [0, 1], [POOL.y - 1.6, 0.4]);
	const kick = fr >= POP && fr < POP + 10 ? (1 - (fr - POP) / 10) * 7 : 0;
	return {
		transform: `translate(${960 + Math.sin(fr * 3.1) * kick} ${540 + Math.cos(fr * 2.3) * kick}) scale(${scale}) translate(${-cx} ${-cy})`,
	};
};

/** Once it's warm: two little birds, and petals on the breeze (the game's petal sprite). */
export const SpringAir: React.FC<{t: number}> = ({t}) => {
	if (t < 0) return null;
	return (
		<svg width={1920} height={1080} style={{position: 'absolute'}}>
			{[0, 1].map((k) => {
				const x = -60 + t * (12 - k * 2) - k * 70;
				const y = 230 + k * 46 - t * 1.2 + Math.sin(t * 0.2 + k) * 6;
				const flap = Math.sin(t * 0.7 + k * 2) * 10;
				return (
					<path
						key={k}
						d={`M${x - 18},${y - flap} Q${x - 8},${y - 6} ${x},${y} Q${x + 8},${y - 6} ${x + 18},${y - flap}`}
						fill="none"
						stroke="#5A3A22"
						strokeWidth={4}
						strokeLinecap="round"
					/>
				);
			})}
			{Array.from({length: 9}, (_, k) => {
				const x = 1980 - (t + k * 9) * (7 + random(`pv${k}`) * 4);
				const y = 80 + random(`py${k}`) * 700 + (t + k * 9) * 2.2 + Math.sin((t + k * 9) * 0.12) * 18;
				const s = 30 + random(`ps${k}`) * 16;
				return (
					<image
						key={k}
						href={art('Petal')}
						filter="url(#petalPink)"
						x={x}
						y={y}
						width={s}
						height={s}
						opacity={interpolate(t, [k * 3, k * 3 + 10], [0, 0.9], clamp)}
						transform={`rotate(${(t + k * 40) * 4} ${x + s / 2} ${y + s / 2})`}
					/>
				);
			})}
		</svg>
	);
};

export const SnowMelt: React.FC = () => {
	useAssets();
	const fr = useCurrentFrame();
	const since = (x: number, y: number) => fr - melt.meltFrame(x, y);
	const never = () => -1;
	const warmth = melt.warmth(fr);
	return (
		<AbsoluteFill style={{backgroundColor: '#5F7B93'}}>
			<svg width={1920} height={1080} viewBox="0 0 1920 1080">
				<MeltDefs />
				<melt.Mask fr={fr} />
				<g {...camera(fr)}>
					<World cold={false} frame={fr} since={since} />
					<g mask="url(#frozen)">
						<World cold frame={fr} since={never} />
					</g>
					<melt.Front fr={fr} />
					<melt.Bubble fr={fr} />
				</g>
				<rect width={1920} height={1080} fill="url(#coldVignette)" opacity={1 - warmth} />
				<rect width={1920} height={1080} fill="url(#sunWash)" opacity={clamp01(warmth * 1.2)} />
			</svg>
			<Snowfall fr={fr} melt={melt} />
			<SpringAir t={fr - (POP + 58)} />

			{/* Our own generated sounds, from Tools/fx/sfx.mjs. */}
			<Sequence from={POP - 2}>
				<Audio src={staticFile('audio/Intro_Bubble.wav')} volume={0.9} />
			</Sequence>
			<Sequence from={POP}>
				<Audio src={staticFile('audio/Intro_Magic.wav')} volume={0.7} />
			</Sequence>
			<Sequence from={POP + 14}>
				<Audio src={staticFile('audio/Intro_Sparkle.wav')} volume={0.6} />
			</Sequence>
				<Sequence from={POP + 62}>
				<Audio src={staticFile('audio/Amb_Chirp1.wav')} volume={0.6} />
			</Sequence>
			<Sequence from={POP + 78}>
				<Audio src={staticFile('audio/Amb_Chirp3.wav')} volume={0.5} />
			</Sequence>
		</AbsoluteFill>
	);
};
