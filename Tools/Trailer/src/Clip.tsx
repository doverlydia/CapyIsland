import React from 'react';
import {AbsoluteFill, Img, staticFile, useCurrentFrame} from 'remotion';

/**
 * Real gameplay, filmed in the Unity editor one frame at a time (public/clips/<name>/0000.jpg…,
 * 30 per game second). `from` skips into the clip; `speed` plays it faster, for time-lapses.
 * `zoom` and `focus` push in on part of the picture (focus in 0–1 of the frame).
 */
export const Clip: React.FC<{
	name: string;
	frames: number;
	from?: number;
	speed?: number;
	zoom?: [number, number];
	focus?: [number, number];
	length?: number;
	/** Frames since the shot began; defaults to the current frame of the enclosing Sequence. */
	t?: number;
}> = ({name, frames, from = 0, speed = 1, zoom = [1, 1], focus = [0.5, 0.5], length = 1, t: at}) => {
	const now = useCurrentFrame();
	const t = at ?? now;
	const i = Math.min(frames - 1, Math.floor(from + t * speed));
	const z = zoom[0] + (zoom[1] - zoom[0]) * Math.min(1, t / Math.max(1, length));
	return (
		<AbsoluteFill style={{overflow: 'hidden', backgroundColor: '#000'}}>
			<Img
				src={staticFile(`clips/${name}/${String(i).padStart(4, '0')}.jpg`)}
				style={{width: '100%', height: '100%', transform: `scale(${z})`, transformOrigin: `${focus[0] * 100}% ${focus[1] * 100}%`}}
			/>
		</AbsoluteFill>
	);
};
