import React from 'react';
import {interpolate, spring} from 'remotion';

/** The game's ink (UiKit.Ink) and heading font, so the words look like they came out of the game. */
export const INK = '#3A2A1F';
export const INK_SOFT = '#8A7564';
export const HEADING = 'Fredoka, sans-serif';

const clamp = {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'} as const;

/** A line of text that pops in word by word at `from` and fades away by `to`. */
export const Caption: React.FC<{text: string; fr: number; from: number; to: number; size?: number; y?: number; stagger?: number}> = ({
	text,
	fr,
	from,
	to,
	size = 92,
	y = 860,
	stagger = 3,
}) => {
	if (fr < from || fr > to) return null;
	const out = interpolate(fr, [to - 8, to], [1, 0], clamp);
	return (
		<div
			style={{
				position: 'absolute',
				left: 0,
				right: 0,
				top: y - size / 2,
				display: 'flex',
				justifyContent: 'center',
				gap: size * 0.42,
				opacity: out,
				fontFamily: HEADING,
				fontWeight: 600,
				fontSize: size,
				lineHeight: 1,
				color: '#FFFFFF',
				WebkitTextStroke: `${size * 0.16}px ${INK}`,
				paintOrder: 'stroke fill',
				textShadow: `0 ${size * 0.08}px ${size * 0.12}px rgba(30, 20, 10, 0.35)`,
			}}
		>
			{text.split(' ').map((word, i) => {
				const pop = spring({frame: fr - from - i * stagger, fps: 30, config: {damping: 11, stiffness: 180}});
				return (
					<span key={i} style={{display: 'inline-block', transform: `translateY(${(1 - pop) * 40}px) scale(${0.6 + 0.4 * pop})`, opacity: Math.min(1, pop * 1.5)}}>
						{word}
					</span>
				);
			})}
		</div>
	);
};
