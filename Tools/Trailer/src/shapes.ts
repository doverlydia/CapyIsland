import {noise3D} from '@remotion/noise';

export type Pt = [number, number];

const f = (n: number) => n.toFixed(1);

/** A closed Catmull-Rom curve through the points, written as SVG cubic beziers. */
export const smoothClosed = (pts: Pt[]): string => {
	const n = pts.length;
	let d = `M${f(pts[0][0])},${f(pts[0][1])}`;
	for (let i = 0; i < n; i++) {
		const p0 = pts[(i - 1 + n) % n];
		const p1 = pts[i];
		const p2 = pts[(i + 1) % n];
		const p3 = pts[(i + 2) % n];
		const c1x = p1[0] + (p2[0] - p0[0]) / 6;
		const c1y = p1[1] + (p2[1] - p0[1]) / 6;
		const c2x = p2[0] - (p3[0] - p1[0]) / 6;
		const c2y = p2[1] - (p3[1] - p1[1]) / 6;
		d += `C${f(c1x)},${f(c1y)} ${f(c2x)},${f(c2y)} ${f(p2[0])},${f(p2[1])}`;
	}
	return d + 'Z';
};

/** A soft, lumpy ellipse: the island, bushes, drifts. `t` lets the lumps move over time. */
export const blob = (
	cx: number,
	cy: number,
	rx: number,
	ry: number,
	seed: string,
	wobble = 0.06,
	n = 32,
	t = 0,
): string => {
	const pts: Pt[] = [];
	for (let i = 0; i < n; i++) {
		const a = (i / n) * Math.PI * 2;
		const k = 1 + wobble * noise3D(seed, Math.cos(a) * 1.3, Math.sin(a) * 1.3, t);
		pts.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]);
	}
	return smoothClosed(pts);
};

export const clamp01 = (v: number) => Math.max(0, Math.min(1, v));
