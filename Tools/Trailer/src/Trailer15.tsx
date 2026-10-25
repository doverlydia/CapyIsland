import React from 'react';
import {AbsoluteFill, Audio, Easing, interpolate, random, Sequence, staticFile, useCurrentFrame} from 'remotion';
import {Caption} from './Caption';
import {EndCard} from './EndCard';
import {HOBBIES, HobbyCut} from './Hobbies';
import {POOL, THINGS} from './layout';
import {clamp, makeMelt, MeltDefs, Snowfall, useAssets} from './melt';
import {SpringAir} from './SnowMelt';
import {clamp01} from './shapes';
import {inBubble, World} from './World';

/**
 * The 15-second trailer: the Big Chill, the bubble that pops, the island coming back to life out
 * of its bubbles, four hobbies, and the name. Cut to "Morning on the Island" (Capy Music Studio,
 * 96 bpm): it comes in on the pop, from bar 9 where the ocarina joins, and every cut after that
 * lands on its beat.
 */

export const TRAILER15_FRAMES = 450;

/** Words on screen. English for now; the Hebrew version swaps these. */
const TEXT = {
	chill: 'The Big Chill froze Capy Island…',
	wake: 'Bring it back to life!',
	title: 'Capy Island',
	subtitle: 'The Big Chill',
	callToAction: 'Coming soon',
};

export const OPENING_POP = 60;
const POP = OPENING_POP;
const melt = makeMelt(POP, 90);

/** One beat of the music, in frames (96 bpm at 30 fps). */
const BEAT = (30 * 60) / 96;
const BAR = BEAT * 4;
/** Bar 9 of the song, in frames from its start: the music joins here, on the pop. */
const MUSIC_FROM = Math.round(8 * BAR);

const HOBBIES_AT = POP + 2 * BAR;
const HOBBY_LEN = 2 * BEAT;
const END_AT = Math.round(HOBBIES_AT + HOBBIES.length * HOBBY_LEN);

/**
 * When each bubble pops: nearest the spring first, one every sixteenth note, and never before the
 * warmth has reached it.
 */
const POP_AT: Record<number, number> = (() => {
	const order = THINGS.map((t, i) => ({t, i}))
		.filter(({t}) => inBubble(t))
		.sort((a, b) => Math.hypot(a.t.x - POOL.x, a.t.y - POOL.y) - Math.hypot(b.t.x - POOL.x, b.t.y - POOL.y));
	const at: Record<number, number> = {};
	order.forEach(({t, i}, k) => {
		const step = BEAT / 4;
		let when = POP + BEAT * 2 + k * step;
		const warm = melt.meltFrame(t.x, t.y) + 6;
		while (when < warm) when += step;
		at[i] = Math.round(when);
	});
	return at;
})();

/** Close on the bubble; after the pop, pull back to the whole island as it fills up. */
const camera = (fr: number) => {
	const out = interpolate(fr, [POP + 4, POP + 110], [0, 1], {...clamp, easing: Easing.inOut(Easing.cubic)});
	const across = fr < POP ? interpolate(fr, [0, POP], [18, 16.5]) : interpolate(out, [0, 1], [16.5, 55]);
	const settle = interpolate(fr, [POP + 110, HOBBIES_AT + 10], [0, 2], clamp);
	const scale = 1920 / (across - settle);
	const cx = interpolate(out, [0, 1], [POOL.x, 0]);
	const cy = interpolate(out, [0, 1], [POOL.y - 1.6, 0.6]);
	const kick = fr >= POP && fr < POP + 10 ? (1 - (fr - POP) / 10) * 7 : 0;
	return {
		transform: `translate(${960 + Math.sin(fr * 3.1) * kick} ${540 + Math.cos(fr * 2.3) * kick}) scale(${scale}) translate(${-cx} ${-cy})`,
	};
};

/** The opening: the frozen island, the pop, and the warmth rolling out. `wake` adds the second line. */
export const OpeningIsland: React.FC<{fr: number; wake?: boolean}> = ({fr, wake = true}) => {
	const since = (x: number, y: number) => fr - melt.meltFrame(x, y);
	const popped = (i: number) => fr - (POP_AT[i] ?? Infinity);
	const never = () => -1;
	const warmth = melt.warmth(fr);
	return (
		<AbsoluteFill style={{backgroundColor: '#5F7B93'}}>
			<svg width={1920} height={1080} viewBox="0 0 1920 1080">
				<MeltDefs />
				<melt.Mask fr={fr} />
				<g {...camera(fr)}>
					<World cold={false} frame={fr} since={since} popped={popped} />
					<g mask="url(#frozen)">
						<World cold frame={fr} since={never} popped={popped} />
					</g>
					<melt.Front fr={fr} />
					<melt.Bubble fr={fr} />
				</g>
				<rect width={1920} height={1080} fill="url(#coldVignette)" opacity={1 - warmth} />
				<rect width={1920} height={1080} fill="url(#sunWash)" opacity={clamp01(warmth * 1.2)} />
			</svg>
			<Snowfall fr={fr} melt={melt} />
			<SpringAir t={fr - (POP + 70)} />
			<Caption text={TEXT.chill} fr={fr} from={6} to={POP - 2} size={84} />
			{wake && <Caption text={TEXT.wake} fr={fr} from={POP + 40} to={HOBBIES_AT + 6} size={104} />}
		</AbsoluteFill>
	);
};

const Sound: React.FC = () => (
	<>
		{/* music: "The Big Chill" under the cold, "Morning on the Island" from the pop */}
		<Audio src={staticFile('audio/intro_chill.wav')} volume={(f) => interpolate(f, [0, 6, POP - 2, POP + 16], [0, 0.8, 0.8, 0], clamp)} />
		<Sequence from={POP}>
			<Audio
				src={staticFile('audio/music_day.wav')}
				trimBefore={MUSIC_FROM}
				volume={(f) => interpolate(f, [0, 3, TRAILER15_FRAMES - POP - 24, TRAILER15_FRAMES - POP], [0, 0.85, 0.85, 0], clamp)}
			/>
		</Sequence>

		{/* the cold, the pop and the warmth */}
		<Sequence from={POP - 2}>
			<Audio src={staticFile('audio/Intro_Bubble.wav')} volume={0.9} />
		</Sequence>
		<Sequence from={POP}>
			<Audio src={staticFile('audio/Intro_Magic.wav')} volume={0.55} />
		</Sequence>
		<Sequence from={POP + 12}>
			<Audio src={staticFile('audio/Intro_Sparkle.wav')} volume={0.5} />
		</Sequence>
		<Sequence from={POP + 75}>
			<Audio src={staticFile('audio/Amb_Chirp1.wav')} volume={0.45} />
		</Sequence>

		{/* every bubble on the island popping, each a little different */}
		{Object.entries(POP_AT).map(([i, at]) => (
			<Sequence key={i} from={at - 1}>
				<Audio src={staticFile('audio/Intro_Bubble.wav')} volume={0.28} playbackRate={0.85 + random(`pr${i}`) * 0.55} />
			</Sequence>
		))}

		{/* the hobbies */}
		<Sequence from={Math.round(HOBBIES_AT + 2)}>
			<Audio src={staticFile('audio/Intro_Bubble.wav')} volume={0.5} playbackRate={0.7} />
		</Sequence>
		<Sequence from={Math.round(HOBBIES_AT + HOBBY_LEN + 3)}>
			<Audio src={staticFile('audio/Sfx_Stir.wav')} volume={0.6} />
		</Sequence>
		<Sequence from={Math.round(HOBBIES_AT + 2 * HOBBY_LEN + 4)}>
			<Audio src={staticFile('audio/Sfx_Meow.wav')} volume={0.7} />
		</Sequence>
		<Sequence from={Math.round(HOBBIES_AT + 3 * HOBBY_LEN + 3)}>
			<Audio src={staticFile('audio/Intro_Sparkle.wav')} volume={0.5} />
		</Sequence>

		{/* the name */}
		<Sequence from={END_AT + 2}>
			<Audio src={staticFile('audio/Intro_Crown.wav')} volume={0.7} />
		</Sequence>
	</>
);

export const Trailer15: React.FC = () => {
	useAssets();
	const fr = useCurrentFrame();
	return (
		<AbsoluteFill style={{backgroundColor: '#000000'}}>
			{fr < HOBBIES_AT + 10 && <OpeningIsland fr={fr} />}
			{HOBBIES.map((hobby, k) => {
				const from = Math.round(HOBBIES_AT + k * HOBBY_LEN);
				const to = k + 1 < HOBBIES.length ? Math.round(HOBBIES_AT + (k + 1) * HOBBY_LEN) + 8 : END_AT + 10;
				return fr >= from && fr < to ? <HobbyCut key={k} hobby={hobby} t={fr - from} /> : null;
			})}
			{fr >= END_AT && <EndCard t={fr - END_AT} title={TEXT.title} subtitle={TEXT.subtitle} callToAction={TEXT.callToAction} />}
			<Sound />
		</AbsoluteFill>
	);
};
