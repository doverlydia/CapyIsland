import React from 'react';
import {AbsoluteFill, Img, interpolate, spring} from 'remotion';
import {HEADING, INK, INK_SOFT} from './Caption';
import {Twinkles} from './Hobbies';
import {art} from './World';

/**
 * The last shot: the game's icon (the King having a soak), the name, and what to do next.
 * `t` counts from the card's first frame; it opens with an iris over the shot before.
 */
export const EndCard: React.FC<{t: number; title: string; subtitle: string; callToAction: string}> = ({t, title, subtitle, callToAction}) => {
	const iris = interpolate(t, [0, 8], [0, 1300], {extrapolateRight: 'clamp'});
	const icon = spring({frame: t - 2, fps: 30, config: {damping: 10, stiffness: 140}});
	const name = spring({frame: t - 9, fps: 30, config: {damping: 12, stiffness: 150}});
	const sub = spring({frame: t - 15, fps: 30, config: {damping: 14, stiffness: 150}});
	const cta = spring({frame: t - 24, fps: 30, config: {damping: 10, stiffness: 170}});
	const bob = Math.sin(t * 0.12) * 6;
	return (
		<AbsoluteFill
			style={{
				clipPath: `circle(${iris}px at 50% 45%)`,
				background: 'radial-gradient(circle at 50% 38%, #FFF6E3 0%, #FFE2B8 45%, #F5B783 100%)',
				alignItems: 'center',
			}}
		>
			<Twinkles t={t} count={18} seed="end" />
			<Img
				src={art('Icon')}
				style={{
					position: 'absolute',
					top: 90 + bob,
					width: 400,
					height: 400,
					borderRadius: 92,
					transform: `scale(${icon}) rotate(${(1 - icon) * -10}deg)`,
					boxShadow: '0 22px 50px rgba(120, 60, 20, 0.35), 0 0 0 10px rgba(255, 255, 255, 0.85)',
				}}
			/>
			<div
				style={{
					position: 'absolute',
					top: 540,
					fontFamily: HEADING,
					fontWeight: 600,
					fontSize: 150,
					lineHeight: 1,
					color: INK,
					transform: `translateY(${(1 - name) * 50}px) scale(${0.7 + 0.3 * name})`,
					opacity: Math.min(1, name * 1.5),
					textShadow: '0 6px 0 rgba(255,255,255,0.7)',
				}}
			>
				{title}
			</div>
			<div
				style={{
					position: 'absolute',
					top: 712,
					fontFamily: HEADING,
					fontWeight: 600,
					fontSize: 62,
					lineHeight: 1,
					color: INK_SOFT,
					letterSpacing: 2,
					opacity: Math.min(1, sub * 1.5),
					transform: `translateY(${(1 - sub) * 30}px)`,
				}}
			>
				{subtitle}
			</div>
			<div
				style={{
					position: 'absolute',
					top: 830,
					padding: '22px 64px',
					borderRadius: 999,
					background: INK,
					color: '#FFF6E3',
					fontFamily: 'Nunito, sans-serif',
					fontWeight: 700,
					fontSize: 52,
					lineHeight: 1,
					transform: `scale(${cta})`,
					boxShadow: '0 10px 24px rgba(60, 30, 10, 0.3)',
				}}
			>
				{callToAction}
			</div>
		</AbsoluteFill>
	);
};
