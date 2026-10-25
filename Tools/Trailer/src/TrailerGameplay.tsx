import React from 'react';
import {AbsoluteFill, Audio, Img, interpolate, Sequence, staticFile, useCurrentFrame} from 'remotion';
import {Caption} from './Caption';
import {Clip} from './Clip';
import {CLIPS} from './clips.gen';
import {EndCard} from './EndCard';
import {clamp, useAssets} from './melt';
import {OPENING_POP, OpeningIsland} from './Trailer15';
import {art} from './World';

/**
 * The gameplay trailer: the Big Chill melting away (drawn from the game's sprites), then real
 * play recorded in the Unity editor (public/clips), cut to "Morning on the Island" (Capy Music
 * Studio, 96 bpm). Every shot below starts on a beat; lengths are in beats.
 */

const BEAT = (30 * 60) / 96;
const BAR = BEAT * 4;
const POP = OPENING_POP;
/** The opening runs one bar past the pop, then cuts to the game. */
const OPEN_END = Math.round(POP + BAR);

/** Words on screen. English for now; the Hebrew version swaps these. */
const TEXT = {
	chill: 'The Big Chill froze Capy Island…',
	pop: 'Pop the bubbles…',
	grow: '…and bring the island back to life!',
	fishing: 'Go fishing',
	cooking: 'Cook tasty dishes',
	cats: 'Adopt cats',
	sewing: 'Sew your own clothes',
	visit: 'Visit friends',
	chat: 'Chat',
	build: 'Build together',
	contest: 'Join contests',
	title: 'Capy Island',
	subtitle: 'The Big Chill',
	friends: 'Play with friends',
	callToAction: 'Play the early version on itch.io',
};

type Shot = {
	clip: string;
	/** Frame of the recording to start from. */
	from: number;
	beats: number;
	speed?: number;
	/** Push in on part of the picture: [start, end] zoom, and where (0–1). */
	zoom?: [number, number];
	focus?: [number, number];
	/** A caption that starts with this shot and stays for `captionBeats` (default: this shot). */
	caption?: string;
	captionBeats?: number;
	/** Where the caption sits (default near the bottom; top where the game's own buttons are). */
	captionY?: number;
	/**
	 * Sound effects, placed by hand: [frame of the recording, file in public/audio, volume]. The
	 * recordings' own sound is not used: it carries the game's sea and wind ambience.
	 */
	sfx?: [number, string, number][];
};

const FULL: Shot[] = [
	{clip: 'pop', from: 40, beats: 4, speed: 1.2, caption: TEXT.pop, sfx: [[59, 'Intro_Bubble', 0.8], [127, 'Intro_Sparkle', 0.4]]},
	{clip: 'timelapse', from: 10, beats: 4, speed: 3.6, zoom: [1.08, 1.12], focus: [0.62, 0.5], caption: TEXT.grow},
	{clip: 'workers', from: 20, beats: 2, zoom: [1.02, 1.08]},
	{clip: 'rain', from: 40, beats: 2, zoom: [1.04, 1.1]},
	{clip: 'fishing', from: 172, beats: 3, speed: 1.45, zoom: [1.0, 1.04], caption: TEXT.fishing, captionY: 150, sfx: [[243, 'Intro_Sparkle', 0.6]]},
	{clip: 'cooking', from: 120, beats: 3, speed: 1.6, caption: TEXT.cooking, captionY: 900},
	{clip: 'cats', from: 62, beats: 2, zoom: [1.25, 1.35], focus: [0.55, 0.5], caption: TEXT.cats, captionBeats: 4, sfx: [[75, 'Sfx_Meow', 0.7]]},
	{clip: 'cats', from: 330, beats: 2, zoom: [1.5, 1.6], focus: [0.5, 0.78]},
	{clip: 'sewing', from: 30, beats: 2, speed: 2.2, caption: TEXT.sewing, captionBeats: 4, sfx: [[80, 'Intro_Sparkle', 0.5]]},
	{clip: 'hat', from: 100, beats: 2, zoom: [1.15, 1.25], focus: [0.55, 0.5]},
	{clip: 'flight', from: 25, beats: 2, caption: TEXT.visit},
	{clip: 'together', from: 15, beats: 5, speed: 1.9, zoom: [1.06, 1.12], focus: [0.5, 0.42], caption: TEXT.chat, captionBeats: 7},
	{clip: 'chat', from: 60, beats: 2},
	{clip: 'cobuild', from: 40, beats: 3, speed: 1.9, zoom: [1.05, 1.1], caption: TEXT.build, sfx: [[100, 'Intro_Bubble', 0.5]]},
	{clip: 'contest', from: 120, beats: 3, speed: 1.5, zoom: [1.0, 1.05], caption: TEXT.contest},
	{clip: 'contest', from: 264, beats: 2, sfx: [[266, 'Intro_Magic', 0.5]]},
	{clip: 'night', from: 40, beats: 3, speed: 3, zoom: [1.0, 1.06]},
	{clip: 'lanterns', from: 100, beats: 3, zoom: [1.0, 1.05]},
];

/** The short cut (about 20 s): one shot for each thing, the best moment of each. */
const SHORT: Shot[] = [
	{clip: 'pop', from: 48, beats: 3, speed: 1.4, caption: TEXT.pop, sfx: [[59, 'Intro_Bubble', 0.8], [127, 'Intro_Sparkle', 0.4]]},
	{clip: 'timelapse', from: 20, beats: 3, speed: 4.5, zoom: [1.08, 1.12], focus: [0.62, 0.5], caption: TEXT.grow},
	{clip: 'fishing', from: 200, beats: 2, speed: 1.3, zoom: [1.0, 1.04], caption: TEXT.fishing, captionY: 150, sfx: [[243, 'Intro_Sparkle', 0.6]]},
	{clip: 'cats', from: 330, beats: 2, zoom: [1.5, 1.6], focus: [0.5, 0.78], caption: TEXT.cats},
	{clip: 'hat', from: 100, beats: 2, zoom: [1.15, 1.25], focus: [0.55, 0.5], caption: TEXT.sewing},
	{clip: 'together', from: 20, beats: 3, speed: 2.4, zoom: [1.06, 1.12], focus: [0.5, 0.42], caption: TEXT.friends},
	{clip: 'contest', from: 264, beats: 2, caption: TEXT.contest, sfx: [[266, 'Intro_Magic', 0.5]]},
	{clip: 'lanterns', from: 110, beats: 2, zoom: [1.0, 1.05]},
];

export type Cut = 'full' | 'short';

/** When each shot starts, in frames of the trailer, and where the end card begins. */
const plan = (cut: Cut) => {
	const shots = cut === 'short' ? SHORT : FULL;
	const starts: number[] = [];
	let beat = 0;
	for (const s of shots) {
		starts.push(Math.round(OPEN_END + beat * BEAT));
		beat += s.beats;
	}
	const end = Math.round(OPEN_END + beat * BEAT);
	const frames = end + Math.round(BEAT * (cut === 'short' ? 5 : 6));
	return {shots, starts, end, frames};
};

export const trailerFrames = (cut: Cut) => plan(cut).frames;

/** Music from bar 5 of "Morning on the Island": the ocarina joins just as the hobbies begin. */
const MUSIC_FROM = Math.round(4 * BAR);

/** The game's pointing hand, wherever a finger touched the screen in the recording. */
const Hand: React.FC<{clip: string; at: number}> = ({clip, at}) => {
	const taps = CLIPS[clip]?.taps ?? [];
	const recent = taps.filter((t) => t.frame <= at && at - t.frame < 8).pop();
	if (!recent) return null;
	const size = 120;
	const press = interpolate(at - recent.frame, [0, 3, 8], [0.9, 1, 1], clamp);
	return (
		<Img
			src={art('Hand')}
			style={{
				position: 'absolute',
				left: recent.x * 1920 - size * 0.3,
				top: recent.y * 1080 - size * 0.08,
				width: size,
				height: size,
				transform: `scale(${press})`,
				filter: 'drop-shadow(0 6px 8px rgba(0,0,0,0.25))',
			}}
		/>
	);
};

const ShotView: React.FC<{shot: Shot; t: number}> = ({shot, t}) => {
	const len = Math.round(shot.beats * BEAT);
	const speed = shot.speed ?? 1;
	const frames = CLIPS[shot.clip]?.frames ?? 1;
	const at = Math.min(frames - 1, Math.floor(shot.from + t * speed));
	return (
		<AbsoluteFill>
			<Clip name={shot.clip} frames={frames} from={shot.from} speed={speed} zoom={shot.zoom ?? [1.0, 1.04]} focus={shot.focus} length={len} t={t} />
			{(shot.zoom?.[0] ?? 1) === 1 && <Hand clip={shot.clip} at={at} />}
		</AbsoluteFill>
	);
};

const Sound: React.FC<{cut: Cut}> = ({cut}) => {
	const {shots: SHOTS, starts: STARTS, end: SHOTS_END, frames: TRAILER_FRAMES} = plan(cut);
	return (
	<>
		{/* the opening: "The Big Chill" under the cold, then the pop */}
		<Audio src={staticFile('audio/intro_chill.wav')} volume={(f) => interpolate(f, [0, 6, POP - 2, POP + 16], [0, 0.8, 0.8, 0], clamp)} />
		<Sequence from={POP - 2}>
			<Audio src={staticFile('audio/Intro_Bubble.wav')} volume={0.9} />
		</Sequence>
		<Sequence from={POP}>
			<Audio src={staticFile('audio/Intro_Magic.wav')} volume={0.55} />
		</Sequence>
		<Sequence from={POP + 12}>
			<Audio src={staticFile('audio/Intro_Sparkle.wav')} volume={0.5} />
		</Sequence>
		{/* "Morning on the Island" from the pop to the end */}
		<Sequence from={POP}>
			<Audio
				src={staticFile('audio/music_day.wav')}
				trimBefore={MUSIC_FROM}
				volume={(f) => interpolate(f, [0, 3, TRAILER_FRAMES - POP - 30, TRAILER_FRAMES - POP], [0, 0.7, 0.7, 0], clamp)}
			/>
		</Sequence>
		{/* the shots' sound effects */}
		{SHOTS.flatMap((s, i) =>
			(s.sfx ?? []).map(([at, file, volume]) => (
				<Sequence key={`${i}-${at}`} from={Math.round(STARTS[i] + (at - s.from) / (s.speed ?? 1))}>
					<Audio src={staticFile(`audio/${file}.wav`)} volume={volume} />
				</Sequence>
			)),
		)}
		<Sequence from={SHOTS_END + 2}>
			<Audio src={staticFile('audio/Intro_Crown.wav')} volume={0.7} />
		</Sequence>
	</>
	);
};

export const TrailerGameplay: React.FC<{cut: Cut}> = ({cut}) => {
	useAssets();
	const fr = useCurrentFrame();
	const {shots: SHOTS, starts: STARTS, end: SHOTS_END} = plan(cut);
	const i = STARTS.findIndex((s, k) => fr >= s && (k + 1 === STARTS.length ? fr < SHOTS_END : fr < STARTS[k + 1]));
	return (
		<AbsoluteFill style={{backgroundColor: '#000'}}>
			{fr < OPEN_END && <OpeningIsland fr={fr} wake={false} />}
			{i >= 0 && <ShotView shot={SHOTS[i]} t={fr - STARTS[i]} />}
			{SHOTS.map((s, k) => {
				if (!s.caption) return null;
				const from = STARTS[k];
				const to = Math.round(from + (s.captionBeats ?? s.beats) * BEAT) - 2;
				return <Caption key={k} text={s.caption} fr={fr} from={from + 2} to={to} size={84} y={s.captionY ?? 960} />;
			})}
			{fr >= SHOTS_END && <EndCard t={fr - SHOTS_END} title={TEXT.title} subtitle={TEXT.subtitle} callToAction={TEXT.callToAction} />}
			<Sound cut={cut} />
		</AbsoluteFill>
	);
};
